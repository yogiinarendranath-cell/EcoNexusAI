namespace EcoNexus.Contracts.Citizen;

/// <summary>A single entry in the citizen's green points ledger.</summary>
public sealed record PointTransactionResponse(
    Guid Id,
    int SignedDelta,
    string Source,
    string Reason,
    string Description,
    Guid? RelatedEntityId,
    DateTimeOffset OccurredAt);
