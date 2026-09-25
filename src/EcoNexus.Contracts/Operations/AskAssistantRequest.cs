namespace EcoNexus.Contracts.Operations;

/// <summary>Request body for POST /api/v1/operations/assistant/ask.</summary>
public sealed record AskAssistantRequest(string Question);
