using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcoNexus.Infrastructure.AI;

/// <summary>
/// Real LLM implementation of IAssistantLlm that uses a local Ollama
/// server. Two calls:
///
///   1. Intent resolution — given the tool catalog and the question,
///      produce { "tool": "...", "parameters": {...} }.
///   2. Answer shaping — given the tool's JSON result, produce prose.
///
/// All parsing is defensive. If Ollama returns garbage, the fallback
/// keyword matching kicks in (same logic as the mock). The operator
/// never sees a stack trace.
/// </summary>
internal sealed class OllamaAssistantLlm : IAssistantLlm
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly WasteClassificationOptions _options;
    private readonly ILogger<OllamaAssistantLlm> _logger;

    public OllamaAssistantLlm(
        HttpClient http,
        IOptions<WasteClassificationOptions> options,
        ILogger<OllamaAssistantLlm> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        _http.BaseAddress = new Uri(_options.Ollama.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.Ollama.TimeoutSeconds);
    }

    public string ProviderName => "Ollama";

    public async Task<ToolCall> ResolveIntentAsync(
        string question,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentNullException.ThrowIfNull(availableTools);

        try
        {
            var prompt = BuildIntentPrompt(question, availableTools);
            var raw = await CallOllamaAsync(prompt, cancellationToken);
            return ParseIntent(raw, availableTools);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama intent resolution failed; returning unknown");
            return ToolCall.NoParameters("__unknown__");
        }
    }

    public async Task<string> ShapeAnswerAsync(
        string question,
        string toolName,
        object toolResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(toolResult);

        try
        {
            var resultJson = JsonSerializer.Serialize(toolResult);
            var prompt = BuildShapePrompt(question, toolName, resultJson);
            return await CallOllamaAsync(prompt, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama answer shaping failed; using raw JSON");
            return $"Query result: {JsonSerializer.Serialize(toolResult)}";
        }
    }

    private async Task<string> CallOllamaAsync(
        string prompt,
        CancellationToken cancellationToken)
    {
        var request = new OllamaGenerateRequest(
            Model: _options.Ollama.Model,
            Prompt: prompt,
            Stream: false,
            Format: "json");

        var response = await _http.PostAsJsonAsync(
            "/api/generate",
            request,
            cancellationToken);

        response.EnsureSuccessStatusCode();

        var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
            cancellationToken: cancellationToken);

        return payload?.Response ?? string.Empty;
    }

    private static string BuildIntentPrompt(
        string question,
        IReadOnlyList<ToolDescriptor> tools)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an operations assistant for EcoNexus AI.");
        sb.AppendLine("Choose exactly ONE tool to answer the operator's question.");
        sb.AppendLine();
        sb.AppendLine("Available tools:");
        foreach (var t in tools)
        {
            sb.AppendLine($"- {t.Name}: {t.Description}");
            sb.AppendLine($"  Parameters schema: {t.ParametersJsonSchema}");
        }
        sb.AppendLine();
        sb.AppendLine("Respond ONLY with a JSON object of the form:");
        sb.AppendLine("{ \"tool\": \"<ToolName>\", \"parameters\": { ... } }");
        sb.AppendLine("No prose. No explanations. Only the JSON object.");
        sb.AppendLine();
        sb.AppendLine($"Question: {question}");
        return sb.ToString();
    }

    private static string BuildShapePrompt(
        string question,
        string toolName,
        string resultJson)
    {
        var sb = new StringBuilder();
        sb.AppendLine("You are an operations assistant for EcoNexus AI.");
        sb.AppendLine("Format the tool result below into a clear, concise answer");
        sb.AppendLine("for an operator. Do NOT invent numbers. Use only the data");
        sb.AppendLine("provided. Two to four sentences is ideal.");
        sb.AppendLine();
        sb.AppendLine($"Original question: {question}");
        sb.AppendLine($"Tool used: {toolName}");
        sb.AppendLine($"Tool result (JSON): {resultJson}");
        sb.AppendLine();
        sb.AppendLine("Respond with the formatted answer only.");
        return sb.ToString();
    }

    private ToolCall ParseIntent(
        string raw,
        IReadOnlyList<ToolDescriptor> availableTools)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<IntentOutput>(raw, JsonOptions);

            if (parsed?.Tool is null)
            {
                return ToolCall.NoParameters("__unknown__");
            }

            // Verify the tool is in the whitelist.
            var match = availableTools.FirstOrDefault(t =>
                string.Equals(t.Name, parsed.Tool, StringComparison.OrdinalIgnoreCase));

            if (match is null)
            {
                _logger.LogWarning(
                    "Ollama returned unknown tool '{Tool}'; rejecting",
                    parsed.Tool);
                return ToolCall.NoParameters("__unknown__");
            }

            var parameters = parsed.Parameters is null
                ? new Dictionary<string, object?>()
                : new Dictionary<string, object?>(parsed.Parameters);

            return new ToolCall(match.Name, parameters);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse Ollama intent JSON");
            return ToolCall.NoParameters("__unknown__");
        }
    }

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("format")] string Format);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response);

    private sealed record IntentOutput(
        [property: JsonPropertyName("tool")] string? Tool,
        [property: JsonPropertyName("parameters")] Dictionary<string, object?>? Parameters);
}
