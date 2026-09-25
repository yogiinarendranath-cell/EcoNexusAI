namespace EcoNexus.Contracts.Operations;

/// <summary>
/// Lightweight view of a past assistant interaction, used by the
/// "recent questions" list on the dashboard.
/// </summary>
public sealed record AssistantInteractionResponse(
    Guid Id,
    string Question,
    string Answer,
    string ToolName,
    string ProviderName,
    long LatencyMs,
    DateTimeOffset OccurredAt);
