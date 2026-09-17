using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.DomainEvents;

/// <summary>
/// Raised when a waste station is collected (emptied).
/// </summary>
public sealed record WasteStationCollectedEvent(
    Guid StationId,
    string StationCode,
    DateTimeOffset CollectedAt) : IDomainEvent;
