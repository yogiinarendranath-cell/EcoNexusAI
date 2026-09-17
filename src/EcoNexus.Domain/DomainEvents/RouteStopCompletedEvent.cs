using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when an individual route stop is completed.
/// </summary>
public sealed record RouteStopCompletedEvent(
    Guid JobId,
    Guid StationId,
    Weight Collected,
    DateTimeOffset CompletedAt) : IDomainEvent;
