using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Stations.ForecastStationFillLevel;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class ForecastStationFillLevelHandlerTests
{
    private readonly IWasteStationRepository _stations =
        Substitute.For<IWasteStationRepository>();

    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    private readonly ForecastStationFillLevelHandler _handler;

    public ForecastStationFillLevelHandlerTests()
    {
        _handler = new ForecastStationFillLevelHandler(
            _stations,
            _timeProvider,
            NullLogger<ForecastStationFillLevelHandler>.Instance);
    }

    private static WasteStation NewStation()
        => WasteStation.Create(
            StationCode.Create("ST-0001"),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);

    private static void AddReading(WasteStation station, double fill, DateTimeOffset at)
        => station.RecordReading(
            FillLevel.FromPercent(fill),
            temperatureCelsius: 22.0,
            batteryPercent: 90,
            recordedAt: at);

    private void Setup(WasteStation station, DateTimeOffset now)
    {
        _timeProvider.GetUtcNow().Returns(now);
        _stations
            .GetWithReadingsAsync(station.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(station);
    }

    [Fact]
    public async Task Handle_RisingFill_ReturnsLinearRegressionWithOverflowPrediction()
    {
        var station = NewStation();
        var t0 = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        var fills = new[] { 20.0, 30.0, 40.0, 50.0, 60.0, 70.0 };
        for (var i = 0; i < fills.Length; i++)
        {
            AddReading(station, fills[i], t0.AddHours(i));
        }

        Setup(station, t0.AddHours(6));

        var result = await _handler.Handle(
            new ForecastStationFillLevelQuery(station.Id, WindowHours: 24),
            CancellationToken.None);

        Assert.Equal(station.Id,         result.StationId);
        Assert.Equal(70.0,               result.CurrentFillPercent, 3);
        Assert.Equal(10.0,               result.FillRatePercentPerHour, 3);
        Assert.Equal("LinearRegression", result.Method);
        Assert.Equal(6,                  result.SampleSize);
        Assert.NotNull(result.PredictedOverflowAt);
        Assert.True(result.IsOverflowPredicted);
    }

    [Fact]
    public async Task Handle_FewerThanMinimumSamples_ReturnsInsufficientData()
    {
        var station = NewStation();
        var t0 = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        AddReading(station, 20.0, t0);
        AddReading(station, 30.0, t0.AddHours(1));
        AddReading(station, 40.0, t0.AddHours(2));

        Setup(station, t0.AddHours(3));

        var result = await _handler.Handle(
            new ForecastStationFillLevelQuery(station.Id),
            CancellationToken.None);

        Assert.Equal("InsufficientData", result.Method);
        Assert.Equal(3,                  result.SampleSize);
        Assert.Equal(40.0,               result.CurrentFillPercent, 3);
        Assert.Null(result.PredictedOverflowAt);
        Assert.False(result.IsOverflowPredicted);
    }

    [Fact]
    public async Task Handle_FlatFill_ReturnsZeroRateNoOverflow()
    {
        var station = NewStation();
        var t0 = new DateTimeOffset(2026, 10, 1, 0, 0, 0, TimeSpan.Zero);
        for (var i = 0; i < 6; i++)
        {
            AddReading(station, 50.0, t0.AddHours(i));
        }

        Setup(station, t0.AddHours(6));

        var result = await _handler.Handle(
            new ForecastStationFillLevelQuery(station.Id),
            CancellationToken.None);

        Assert.Equal("LinearRegression", result.Method);
        Assert.Equal(0.0,                result.FillRatePercentPerHour, 3);
        Assert.Null(result.PredictedOverflowAt);
        Assert.False(result.IsOverflowPredicted);
    }

    [Fact]
    public async Task Handle_StationDoesNotExist_ThrowsNotFoundException()
    {
        var stationId = Guid.NewGuid();
        _timeProvider.GetUtcNow().Returns(DateTimeOffset.UtcNow);
        _stations
            .GetWithReadingsAsync(stationId, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new ForecastStationFillLevelQuery(stationId),
                CancellationToken.None));

        Assert.Contains(stationId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_PassesSinceComputedFromTimeProviderAndWindow()
    {
        var station = NewStation();
        var fixedNow = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var expectedSince = fixedNow.AddHours(-6);
        Setup(station, fixedNow);

        await _handler.Handle(
            new ForecastStationFillLevelQuery(station.Id, WindowHours: 6),
            CancellationToken.None);

        await _stations
            .Received(1)
            .GetWithReadingsAsync(
                station.Id,
                expectedSince,
                Arg.Any<CancellationToken>());
    }
}