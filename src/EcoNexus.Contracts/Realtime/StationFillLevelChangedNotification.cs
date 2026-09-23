namespace EcoNexus.Contracts.Realtime;

/// <summary>
/// Payload pushed to operations clients when a station's fill level changes.
/// Shape is stable and consumed by the React dashboard (Step 13).
/// </summary>
public sealed record StationFillLevelChangedNotification(
    Guid StationId,
    string StationCode,
    double FillLevelPercent,
    bool IsCritical,
    DateTimeOffset RecordedAt);
