namespace EcoNexus.Contracts.Stations;

/// <summary>
/// Response for GET /api/v1/stations/{id}/forecast.
/// Predicts when the station will reach 100% fill.
/// All fill values are on the 0-100 percent scale.
/// </summary>
public sealed record ForecastStationFillLevelResponse(
    Guid StationId,
    double CurrentFillPercent,
    double FillRatePercentPerHour,
    DateTimeOffset? PredictedOverflowAt,
    double? HoursUntilOverflow,
    double Confidence,
    string Method,
    int SampleSize,
    bool IsOverflowPredicted);

