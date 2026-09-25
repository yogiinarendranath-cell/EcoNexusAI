using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a citizen redeems a reward. Consumers handle
/// fulfillment (email voucher, physical shipment, etc.).
/// </summary>
public sealed record RewardRedeemedEvent(
    Guid CitizenProfileId,
    Guid RewardId,
    string RewardName,
    int PointsSpent,
    DateTimeOffset RedeemedAt) : IDomainEvent;
