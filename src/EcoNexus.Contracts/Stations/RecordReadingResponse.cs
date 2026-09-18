namespace EcoNexus.Contracts.Stations;

/// <summary>Response returned after a sensor reading is recorded.</summary>
public sealed record RecordReadingResponse(
    Guid StationId,
    Guid ReadingId,
    double NewFillLevelPercent,
    bool IsCritical,
    bool CriticalTransitionOccurred,
    DateTimeOffset LastUpdatedAt);
