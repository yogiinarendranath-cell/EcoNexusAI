namespace EcoNexus.Contracts.Stations;

/// <summary>
/// Wire format for real-time station updates pushed over SignalR.
/// Kept small on purpose — clients receive deltas, not full entities.
/// </summary>
public sealed record StationUpdateDto(
    Guid StationId,
    string StationCode,
    double FillLevelPercent,
    bool IsCritical,
    DateTimeOffset RecordedAt);
