namespace EcoNexus.Contracts.Recycling;

/// <summary>Request body for advancing an intake batch to a new lifecycle stage.</summary>
public sealed record AdvanceIntakeRequest(
    string NextStage,
    DateTimeOffset AdvancedAt);
