namespace EcoNexus.Contracts.Stations;

/// <summary>
/// A single fill-level reading emitted by a waste station.
/// Returned by GET /api/v1/stations/{id}/readings.
/// </summary>
public sealed record StationReadingResponse(
    Guid Id,
    Guid StationId,
    double FillLevelPercent,
    double TemperatureCelsius,
    int BatteryPercent,
    DateTimeOffset RecordedAt);

