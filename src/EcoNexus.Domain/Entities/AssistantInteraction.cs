using System.Text.Json;
using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A single question-and-answer exchange with the operations assistant.
/// Standalone aggregate root: not a child of any other aggregate.
///
/// Every assistant call is recorded — question, tool selection, tool
/// result, formatted answer, provider, and latency — so that every
/// decision is auditable and analysable.
/// </summary>
public sealed class AssistantInteraction : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string Question { get; private set; }
    public string ToolName { get; private set; }
    public string ToolParametersJson { get; private set; }
    public string ToolResultJson { get; private set; }
    public string Answer { get; private set; }
    public string ProviderName { get; private set; }
    public long LatencyMs { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    // Required by EF Core
    private AssistantInteraction()
    {
        Question = null!;
        ToolName = null!;
        ToolParametersJson = null!;
        ToolResultJson = null!;
        Answer = null!;
        ProviderName = null!;
    }

    private AssistantInteraction(
        Guid userId,
        string question,
        string toolName,
        string toolParametersJson,
        string toolResultJson,
        string answer,
        string providerName,
        long latencyMs,
        DateTimeOffset occurredAt)
    {
        UserId = userId;
        Question = question;
        ToolName = toolName;
        ToolParametersJson = toolParametersJson;
        ToolResultJson = toolResultJson;
        Answer = answer;
        ProviderName = providerName;
        LatencyMs = latencyMs;
        OccurredAt = occurredAt;
    }

    /// <summary>
    /// Records a completed assistant interaction.
    /// </summary>
    public static AssistantInteraction Record(
        Guid userId,
        string question,
        string toolName,
        IReadOnlyDictionary<string, object?> parameters,
        object? toolResult,
        string answer,
        string providerName,
        long latencyMs,
        DateTimeOffset occurredAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException(
                "UserId must not be empty.",
                nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(question))
        {
            throw new ArgumentException(
                "Question must not be empty.",
                nameof(question));
        }

        if (question.Length > 2000)
        {
            throw new ArgumentException(
                "Question must be 2000 characters or fewer.",
                nameof(question));
        }

        if (string.IsNullOrWhiteSpace(toolName))
        {
            throw new ArgumentException(
                "Tool name must not be empty.",
                nameof(toolName));
        }

        if (string.IsNullOrWhiteSpace(answer))
        {
            throw new ArgumentException(
                "Answer must not be empty.",
                nameof(answer));
        }

        if (answer.Length > 4000)
        {
            throw new ArgumentException(
                "Answer must be 4000 characters or fewer.",
                nameof(answer));
        }

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException(
                "Provider name must not be empty.",
                nameof(providerName));
        }

        if (latencyMs < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(latencyMs),
                "Latency must not be negative.");
        }

        var parametersJson = JsonSerializer.Serialize(parameters);
        var resultJson = toolResult is null
            ? "null"
            : JsonSerializer.Serialize(toolResult);

        return new AssistantInteraction(
            userId,
            question.Trim(),
            toolName.Trim(),
            parametersJson,
            resultJson,
            answer.Trim(),
            providerName.Trim(),
            latencyMs,
            occurredAt);
    }
}
