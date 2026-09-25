using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace EcoNexus.Infrastructure.AI;

/// <summary>
/// Real AI waste classification via a local Ollama server running a
/// vision-capable model (LLaVA by default). Sends the image bytes and
/// a strict prompt to Ollama's /api/generate endpoint, parses the JSON
/// response defensively, and degrades gracefully on any failure.
///
/// This service NEVER throws for AI-related errors (network, malformed
/// output, timeout) — it returns a low-confidence "General" result so
/// the caller can render an "unclear image" message. It only throws
/// for programmer errors (null args).
/// </summary>
internal sealed class OllamaWasteClassificationService : IWasteClassificationService
{
    private const string SystemPrompt = """
        You are a waste classification assistant for EcoNexus AI.
        Given an image, classify the PRIMARY waste item.
        Respond ONLY with valid JSON matching this exact schema:
        {
          "category": "Organic" | "Plastic" | "Paper" | "Glass" | "Metal" | "EWaste" | "General" | "Hazardous",
          "confidence": 0.0-1.0,
          "isRecyclable": true | false,
          "isCompostable": true | false,
          "disposalInstruction": "short sentence"
        }
        No prose. No explanations. Only the JSON object.
        """;

    /// <summary>
    /// Shared deserialization options. Cached because JsonSerializerOptions
    /// is expensive to construct and CA1869 forbids per-call allocation.
    /// </summary>
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly HttpClient _http;
    private readonly WasteClassificationOptions _options;
    private readonly ILogger<OllamaWasteClassificationService> _logger;

    public OllamaWasteClassificationService(
        HttpClient http,
        IOptions<WasteClassificationOptions> options,
        ILogger<OllamaWasteClassificationService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        _http.BaseAddress = new Uri(_options.Ollama.BaseUrl);
        _http.Timeout = TimeSpan.FromSeconds(_options.Ollama.TimeoutSeconds);
    }

    public string ProviderName => "Ollama";

    public async Task<WasteClassificationResult> ClassifyAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);

        try
        {
            var base64 = await DownloadAndEncodeImageAsync(imageUrl, cancellationToken);

            var request = new OllamaGenerateRequest(
                Model: _options.Ollama.Model,
                Prompt: SystemPrompt,
                Images: new[] { base64 },
                Stream: false,
                Format: "json");

            var response = await _http.PostAsJsonAsync(
                "/api/generate",
                request,
                cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(
                    "Ollama returned {Status} for image {ImageUrl}",
                    (int)response.StatusCode, imageUrl);
                return FallbackResult("Ollama service returned an error.");
            }

            var payload = await response.Content.ReadFromJsonAsync<OllamaGenerateResponse>(
                cancellationToken: cancellationToken);

            if (payload?.Response is null)
            {
                _logger.LogWarning("Ollama returned an empty response");
                return FallbackResult("The model returned an empty response.");
            }

            return ParseModelResponse(payload.Response);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ollama classification failed for {ImageUrl}", imageUrl);
            return FallbackResult("Waste classification is temporarily unavailable.");
        }
    }

    private static async Task<string> DownloadAndEncodeImageAsync(
        string imageUrl,
        CancellationToken cancellationToken)
    {
        // Support both http(s) URLs and data URIs.
        if (imageUrl.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            var comma = imageUrl.IndexOf(',');
            return comma < 0 ? imageUrl : imageUrl[(comma + 1)..];
        }

        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
        var bytes = await client.GetByteArrayAsync(imageUrl, cancellationToken);
        return Convert.ToBase64String(bytes);
    }

    private WasteClassificationResult ParseModelResponse(string raw)
    {
        try
        {
            var parsed = JsonSerializer.Deserialize<ModelOutput>(raw, JsonOptions);

            if (parsed is null || string.IsNullOrWhiteSpace(parsed.Category))
            {
                return FallbackResult("The model did not return a valid category.");
            }

            if (!Enum.TryParse<WasteCategory>(parsed.Category, ignoreCase: true, out var category))
            {
                _logger.LogWarning("Unknown category '{Category}' from Ollama", parsed.Category);
                return FallbackResult("The model returned an unknown category.");
            }

            var confidence = Math.Clamp(parsed.Confidence, 0.0, 1.0);

            var instruction = string.IsNullOrWhiteSpace(parsed.DisposalInstruction)
                ? "No specific disposal instruction was provided."
                : parsed.DisposalInstruction.Trim();

            return new WasteClassificationResult(
                category,
                confidence,
                parsed.IsRecyclable,
                parsed.IsCompostable,
                instruction);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(ex, "Could not parse Ollama JSON: {Raw}", Truncate(raw, 200));
            return FallbackResult("The model returned an unparseable response.");
        }
    }

    private static WasteClassificationResult FallbackResult(string reason)
        => new(WasteCategory.General, 0.0, false, false, reason);

    private static string Truncate(string s, int max)
        => s.Length <= max ? s : s[..max] + "...";

    private sealed record OllamaGenerateRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("prompt")] string Prompt,
        [property: JsonPropertyName("images")] string[] Images,
        [property: JsonPropertyName("stream")] bool Stream,
        [property: JsonPropertyName("format")] string Format);

    private sealed record OllamaGenerateResponse(
        [property: JsonPropertyName("response")] string? Response);

    private sealed record ModelOutput(
        [property: JsonPropertyName("category")] string? Category,
        [property: JsonPropertyName("confidence")] double Confidence,
        [property: JsonPropertyName("isRecyclable")] bool IsRecyclable,
        [property: JsonPropertyName("isCompostable")] bool IsCompostable,
        [property: JsonPropertyName("disposalInstruction")] string? DisposalInstruction);
}
