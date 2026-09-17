using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a collection job is completed.
/// </summary>
public sealed record CollectionJobCompletedEvent(
    Guid JobId,
    Guid VehicleId,
    int StopsCompleted,
    Weight TotalCollected,
    DateTimeOffset CompletedAt) : IDomainEvent;
