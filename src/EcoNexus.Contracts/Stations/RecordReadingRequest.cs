namespace EcoNexus.Contracts.Stations;

/// <summary>Request body for POST /api/v1/stations/{id}/readings.</summary>
public sealed record RecordReadingRequest(
    double FillLevelPercent,
    double TemperatureCelsius,
    int BatteryPercent,
    DateTimeOffset RecordedAt);
