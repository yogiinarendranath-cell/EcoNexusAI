using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a citizen earns green points. Consumers can use this
/// to trigger notifications, streak updates, or analytics.
/// </summary>
public sealed record GreenPointsEarnedEvent(
    Guid CitizenProfileId,
    int Amount,
    GreenPointReason Reason,
    DateTimeOffset EarnedAt) : IDomainEvent;
