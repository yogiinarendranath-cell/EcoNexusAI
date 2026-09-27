using Xunit;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Services;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.UnitTests.Domain.Services;

/// <summary>
/// Unit tests for <see cref="FillLevelForecaster"/> — the pure domain service that
/// predicts when a waste station will overflow based on recent fill-level readings.
/// All fill values are on the 0-100 percent scale.
/// </summary>
public sealed class FillLevelForecasterTests
{
    private static readonly Guid StationId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly DateTimeOffset T0 = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private static StationReading Reading(double percent, double hoursFromT0)
        => new(StationId, FillLevel.FromPercent(percent), 20.0, 100, T0.AddHours(hoursFromT0));

    private static List<StationReading> Linear(double startPercent, double percentPerHour, int count, double hoursBetween)
    {
        var list = new List<StationReading>(count);
        for (var i = 0; i < count; i++)
        {
            list.Add(Reading(startPercent + percentPerHour * i * hoursBetween, i * hoursBetween));
        }
        return list;
    }

    [Fact]
    public void Forecast_EmptyReadings_ReturnsInsufficientData()
    {
        var result = FillLevelForecaster.Forecast(StationId, Array.Empty<StationReading>(), T0);

        Assert.Equal("InsufficientData", result.Method);
        Assert.Null(result.PredictedOverflowAt);
        Assert.Null(result.HoursUntilOverflow);
        Assert.Equal(0, result.Confidence);
        Assert.Equal(0, result.SampleSize);
    }

    [Fact]
    public void Forecast_FewerThanMinimumSamples_ReturnsInsufficientData()
    {
        var readings = Linear(startPercent: 10, percentPerHour: 5, count: 4, hoursBetween: 1);

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal("InsufficientData", result.Method);
        Assert.Equal(4, result.SampleSize);
        Assert.Null(result.PredictedOverflowAt);
    }

    [Fact]
    public void Forecast_ExactlyMinimumSamples_ProducesLinearRegression()
    {
        var readings = Linear(startPercent: 10, percentPerHour: 5, count: FillLevelForecaster.MinimumSamples, hoursBetween: 1);

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal("LinearRegression", result.Method);
        Assert.Equal(FillLevelForecaster.MinimumSamples, result.SampleSize);
        Assert.NotNull(result.PredictedOverflowAt);
    }

    [Fact]
    public void Forecast_PerfectLinearGrowth_PredictsOverflowAt100Percent()
    {
        // Fill starts at 0% at t=0, grows 10%/hour. Overflow (100%) expected at t=10h.
        var readings = Linear(startPercent: 0, percentPerHour: 10, count: 11, hoursBetween: 1);

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal("LinearRegression", result.Method);
        Assert.NotNull(result.PredictedOverflowAt);
        Assert.Equal(T0.AddHours(10), result.PredictedOverflowAt!.Value, TimeSpan.FromMinutes(1));
        Assert.Equal(10.0, result.FillRatePercentPerHour, precision: 3);
        Assert.Equal(100.0, result.CurrentFillPercent, precision: 3);
    }

    [Fact]
    public void Forecast_PerfectLinearFit_HasHighConfidence()
    {
        // 20 samples, perfectly linear -> R^2 = 1, sample weight = 1 -> confidence = 1.0
        var readings = Linear(startPercent: 10, percentPerHour: 2, count: 20, hoursBetween: 1);

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal(1.0, result.Confidence, precision: 3);
    }

    [Fact]
    public void Forecast_FlatLine_ReturnsNoOverflow()
    {
        // Fill stays at 50% forever — no overflow predicted.
        var readings = Linear(startPercent: 50, percentPerHour: 0, count: 10, hoursBetween: 1);

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal("LinearRegression", result.Method);
        Assert.Null(result.PredictedOverflowAt);
        Assert.Null(result.HoursUntilOverflow);
        Assert.Equal(0, result.FillRatePercentPerHour);
    }

    [Fact]
    public void Forecast_DecliningFill_ReturnsNoOverflow()
    {
        // Fill is going down (e.g. after a collection) — no overflow.
        var readings = Linear(startPercent: 80, percentPerHour: -5, count: 10, hoursBetween: 1);

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Null(result.PredictedOverflowAt);
        Assert.Equal(0, result.FillRatePercentPerHour);
    }


    [Fact]
    public void Forecast_NoisyData_HasLowerConfidenceThanPerfectFit()
    {
        // Same trend as the perfect case, but with noise added.
        var clean = Linear(startPercent: 10, percentPerHour: 3, count: 15, hoursBetween: 1);
        var noisy = new List<StationReading>
        {
            Reading(10, 0),
            Reading(14, 1),
            Reading(12, 2),
            Reading(19, 3),
            Reading(21, 4),
            Reading(24, 5),
            Reading(28, 6),
            Reading(31, 7),
            Reading(34, 8),
            Reading(38, 9),
            Reading(42, 10),
            Reading(44, 11)
        };

        var cleanResult = FillLevelForecaster.Forecast(StationId, clean, T0);
        var noisyResult = FillLevelForecaster.Forecast(StationId, noisy, T0);

        Assert.True(noisyResult.Confidence < cleanResult.Confidence,
            $"Noisy confidence {noisyResult.Confidence} should be less than clean {cleanResult.Confidence}.");
        Assert.True(noisyResult.Confidence > 0, "Confidence should still be positive for noisy-but-trending data.");
    }

    [Fact]
    public void Forecast_AllReadingsSameTimestamp_ReturnsInsufficientData()
    {
        // Degenerate: cannot regress when time is constant.
        var readings = new List<StationReading>
        {
            Reading(10, 0),
            Reading(20, 0),
            Reading(30, 0),
            Reading(40, 0),
            Reading(50, 0)
        };

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal("InsufficientData", result.Method);
        Assert.Null(result.PredictedOverflowAt);
    }

    [Fact]
    public void Forecast_OverflowInPast_HoursUntilOverflowIsZeroButPredictedTimeIsHistorical()
    {
        // Fill already exceeds 100% in the last readings — overflow is "now".
        var readings = new List<StationReading>
        {
            Reading(90, 0),
            Reading(95, 1),
            Reading(99, 2),
            Reading(100, 3),
            Reading(100, 4),
            Reading(100, 5)
        };

        var now = T0.AddHours(5);
        var result = FillLevelForecaster.Forecast(StationId, readings, now);

        Assert.NotNull(result.PredictedOverflowAt);
        Assert.Equal(0, result.HoursUntilOverflow!.Value, precision: 3);
        Assert.True(result.PredictedOverflowAt!.Value < now, "Predicted overflow should be in the past.");
    }

    [Fact]
    public void Forecast_NullReadings_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            FillLevelForecaster.Forecast(StationId, null!, T0));
    }

    [Fact]
    public void Forecast_SingleReading_ReturnsInsufficientDataWithCurrentFill()
    {
        var readings = new List<StationReading> { Reading(42.5, 0) };

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal("InsufficientData", result.Method);
        Assert.Equal(42.5, result.CurrentFillPercent, precision: 3);
        Assert.Equal(1, result.SampleSize);
    }

    [Fact]
    public void Forecast_EchoesStationId()
    {
        var readings = Linear(startPercent: 10, percentPerHour: 5, count: 10, hoursBetween: 1);
        var differentId = Guid.NewGuid();

        var result = FillLevelForecaster.Forecast(differentId, readings, T0);

        Assert.Equal(differentId, result.StationId);
    }

    [Fact]
    public void Forecast_LargeSampleSize_ConfidenceSaturatesAtCap()
    {
        // 40 samples, perfectly linear. Confidence is capped by the sample-size weight at 1.0
        // because sampleSize (40) > ConfidenceSampleCap (20). Combined with R^2 = 1, final = 1.0.
        var readings = Linear(startPercent: 5, percentPerHour: 2, count: 40, hoursBetween: 1);

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal(1.0, result.Confidence, precision: 3);
        Assert.Equal(40, result.SampleSize);
    }

    [Fact]
    public void Forecast_CurrentFillPercent_ReflectsLatestReading()
    {
        // Non-monotonic path: latest reading is 63%, not the max (which is 80%).
        var readings = new List<StationReading>
        {
            Reading(10, 0),
            Reading(50, 1),
            Reading(80, 2),
            Reading(20, 3),
            Reading(63, 4),
            Reading(63, 5)
        };

        var result = FillLevelForecaster.Forecast(StationId, readings, T0);

        Assert.Equal(63, result.CurrentFillPercent, precision: 3);
    }
}

