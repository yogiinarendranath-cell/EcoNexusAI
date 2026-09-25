namespace EcoNexus.Contracts.Citizen;

/// <summary>Response after a successful reward redemption.</summary>
public sealed record RedeemRewardResponse(
    Guid CitizenProfileId,
    Guid RewardId,
    string RewardName,
    int PointsSpent,
    int NewBalance,
    DateTimeOffset RedeemedAt);
