using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a waste station's fill level crosses the critical threshold.
/// </summary>
public sealed record WasteStationReachedCriticalFillEvent(
    Guid StationId,
    string StationCode,
    FillLevel FillLevel,
    DateTimeOffset DetectedAt) : IDomainEvent;
