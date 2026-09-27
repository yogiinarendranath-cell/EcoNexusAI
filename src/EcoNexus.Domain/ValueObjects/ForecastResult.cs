namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// Result of a fill-level forecast for a waste station. Immutable value object.
/// All fill values are on the 0-100 percent scale (see <see cref="FillLevel"/>).
/// </summary>
public sealed record ForecastResult
{
    /// <summary>Station these forecasts apply to.</summary>
    public Guid StationId { get; init; }

    /// <summary>Latest observed fill level as a percentage (0-100).</summary>
    public double CurrentFillPercent { get; init; }

    /// <summary>Estimated fill rate in percent-per-hour. 0 if flat or declining.</summary>
    public double FillRatePercentPerHour { get; init; }

    /// <summary>When the station is predicted to reach 100% fill. Null if not filling or insufficient data.</summary>
    public DateTimeOffset? PredictedOverflowAt { get; init; }

    /// <summary>Convenience: hours until overflow. Null if PredictedOverflowAt is null.</summary>
    public double? HoursUntilOverflow { get; init; }

    /// <summary>Confidence score between 0 and 1. Based on R-squared and sample size.</summary>
    public double Confidence { get; init; }

    /// <summary>Algorithm used. "LinearRegression" or "InsufficientData".</summary>
    public string Method { get; init; } = "LinearRegression";

    /// <summary>Number of readings used in the forecast.</summary>
    public int SampleSize { get; init; }

    /// <summary>True if overflow is predicted within the forecast horizon.</summary>
    public bool IsOverflowPredicted => PredictedOverflowAt.HasValue;
}

