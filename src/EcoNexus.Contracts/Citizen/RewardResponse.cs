namespace EcoNexus.Contracts.Citizen;

/// <summary>A redeemable reward from the catalog.</summary>
public sealed record RewardResponse(
    Guid Id,
    string Name,
    string Description,
    int CostInPoints,
    bool IsActive,
    DateTimeOffset CreatedAt);
