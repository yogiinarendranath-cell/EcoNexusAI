namespace EcoNexus.Contracts.Operations;

/// <summary>
/// Response from the operations assistant. Exposes the tool that was
/// used, the parameters it was called with, and the answer text so the
/// operator can verify how the answer was derived.
/// </summary>
public sealed record AskAssistantResponse(
    Guid InteractionId,
    string Question,
    string Answer,
    string ToolName,
    string ToolParametersJson,
    string ProviderName,
    long LatencyMs,
    DateTimeOffset OccurredAt);
