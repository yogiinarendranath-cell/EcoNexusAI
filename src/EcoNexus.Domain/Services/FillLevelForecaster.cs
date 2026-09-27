using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Services;

/// <summary>
/// Pure domain service that forecasts when a waste station will overflow,
/// based on a window of recent fill-level readings.
///
/// Algorithm: ordinary least squares (linear regression) on (hoursSinceFirst, fillPercent).
/// Fill levels are on the 0-100 percent scale (see <see cref="FillLevel"/>).
/// Predicts overflow when the fitted line reaches 100%. Confidence is derived from
/// R-squared and sample size. If fewer than <see cref="MinimumSamples"/> readings exist,
/// or the fitted slope is non-positive, returns a forecast with Method = "InsufficientData".
/// </summary>
public static class FillLevelForecaster
{
    /// <summary>Minimum readings required before we attempt a forecast.</summary>
    public const int MinimumSamples = 5;

    /// <summary>Number of samples above which we consider confidence saturated.</summary>
    public const int ConfidenceSampleCap = 20;

    /// <summary>Fill percentage at which a station is considered overflowed.</summary>
    public const double OverflowPercent = 100.0;

    /// <summary>
    /// Forecast when the given station will reach 100% fill.
    /// </summary>
    /// <param name="stationId">Station identifier (echoed in result).</param>
    /// <param name="readings">Recent readings, ascending by RecordedAt. Caller must filter to the desired window.</param>
    /// <param name="now">Reference time used for hoursUntilOverflow computation.</param>
    public static ForecastResult Forecast(
        Guid stationId,
        IReadOnlyList<StationReading> readings,
        DateTimeOffset now)
    {
        ArgumentNullException.ThrowIfNull(readings);

        if (readings.Count == 0)
        {
            return Insufficient(stationId, 0, 0, "No readings available.");
        }

        // Latest reading regardless of sample size — always useful.
        var latest = readings[^1];
        var currentPercent = latest.FillLevel.Percent;

        if (readings.Count < MinimumSamples)
        {
            return Insufficient(stationId, currentPercent, readings.Count,
                $"Need at least {MinimumSamples} readings, got {readings.Count}.");
        }

        // Use the earliest reading in the window as time origin.
        var origin = readings[0].RecordedAt;

        // Build x (hours since origin) and y (fill percent) arrays.
        var n = readings.Count;
        var xs = new double[n];
        var ys = new double[n];

        for (var i = 0; i < n; i++)
        {
            xs[i] = (readings[i].RecordedAt - origin).TotalHours;
            ys[i] = readings[i].FillLevel.Percent;
        }

        // Ordinary least squares: slope = Sxy / Sxx
        var meanX = xs.Average();
        var meanY = ys.Average();

        double sxx = 0, sxy = 0, syy = 0;
        for (var i = 0; i < n; i++)
        {
            var dx = xs[i] - meanX;
            var dy = ys[i] - meanY;
            sxx += dx * dx;
            sxy += dx * dy;
            syy += dy * dy;
        }

        // Degenerate case: all readings at the same instant.
        if (sxx <= double.Epsilon)
        {
            return Insufficient(stationId, currentPercent, n, "All readings share the same timestamp.");
        }

        var slope = sxy / sxx;
        var intercept = meanY - slope * meanX;

        // If the fill level is flat or declining, no overflow is predicted.
        if (slope <= 0)
        {
            return new ForecastResult
            {
                StationId = stationId,
                CurrentFillPercent = currentPercent,
                FillRatePercentPerHour = 0,
                PredictedOverflowAt = null,
                HoursUntilOverflow = null,
                Confidence = ComputeConfidence(sxx, syy, sxy, n),
                Method = "LinearRegression",
                SampleSize = n
            };
        }

        // Solve for x where fill = 100%: x = (100 - intercept) / slope
        var hoursToOverflow = (OverflowPercent - intercept) / slope;
        var predictedOverflowAt = origin.AddHours(hoursToOverflow);

        // If the model already says we are past 100%, clamp overflow to now.
        if (hoursToOverflow <= 0)
        {
            predictedOverflowAt = now;
            hoursToOverflow = 0;
        }

        return new ForecastResult
        {
            StationId = stationId,
            CurrentFillPercent = currentPercent,
            FillRatePercentPerHour = slope,
            PredictedOverflowAt = predictedOverflowAt,
            HoursUntilOverflow = Math.Max(0, (predictedOverflowAt - now).TotalHours),
            Confidence = ComputeConfidence(sxx, syy, sxy, n),
            Method = "LinearRegression",
            SampleSize = n
        };
    }

    private static ForecastResult Insufficient(Guid stationId, double currentPercent, int sampleSize, string reason)
    {
        _ = reason;
        return new ForecastResult
        {
            StationId = stationId,
            CurrentFillPercent = currentPercent,
            FillRatePercentPerHour = 0,
            PredictedOverflowAt = null,
            HoursUntilOverflow = null,
            Confidence = 0,
            Method = "InsufficientData",
            SampleSize = sampleSize
        };
    }

    /// <summary>
    /// Confidence between 0 and 1.
    /// Combines R-squared (fit quality) with a sample-size weight.
    /// </summary>
    private static double ComputeConfidence(double sxx, double syy, double sxy, int sampleSize)
    {
        // R-squared of a simple linear fit.
        double rSquared = 0;
        if (sxx > 0 && syy > 0)
        {
            rSquared = (sxy * sxy) / (sxx * syy);
        }

        // Sample-size weight: saturates at ConfidenceSampleCap.
        var sampleWeight = Math.Min(1.0, (double)sampleSize / ConfidenceSampleCap);

        return Math.Round(rSquared * sampleWeight, 4);
    }
}

