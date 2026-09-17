using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a waste station's fill level changes.
/// </summary>
public sealed record WasteStationFillLevelChangedEvent(
    Guid StationId,
    string StationCode,
    FillLevel NewFillLevel,
    DateTimeOffset RecordedAt) : IDomainEvent;
