using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a collection job transitions from Scheduled to InProgress.
/// </summary>
public sealed record CollectionJobStartedEvent(
    Guid JobId,
    Guid VehicleId,
    DateTimeOffset StartedAt) : IDomainEvent;
