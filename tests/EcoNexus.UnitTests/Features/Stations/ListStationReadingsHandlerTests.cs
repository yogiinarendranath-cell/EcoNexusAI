using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Stations.ListStationReadings;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class ListStationReadingsHandlerTests
{
    private readonly IWasteStationRepository _stations =
        Substitute.For<IWasteStationRepository>();

    private readonly TimeProvider _timeProvider = Substitute.For<TimeProvider>();

    private readonly ListStationReadingsHandler _handler;

    public ListStationReadingsHandlerTests()
    {
        _handler = new ListStationReadingsHandler(_stations, _timeProvider);
    }

    private static WasteStation NewStation()
        => WasteStation.Create(
            StationCode.Create("ST-0001"),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);

    private static void AddReading(WasteStation station, double fill, DateTimeOffset at)
    {
        station.RecordReading(
            FillLevel.FromPercent(fill),
            temperatureCelsius: 22.0,
            batteryPercent: 90,
            recordedAt: at);
    }

    private void SetupStation(WasteStation station, DateTimeOffset now)
    {
        _timeProvider.GetUtcNow().Returns(now);
        _stations
            .GetWithReadingsAsync(station.Id, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(station);
    }

    [Fact]
    public async Task Handle_StationWithReadings_ReturnsMappedReadingsOrderedAscending()
    {
        var station = NewStation();
        var now = DateTimeOffset.UtcNow;
        AddReading(station, 70.0, now.AddHours(-1));
        AddReading(station, 40.0, now.AddHours(-3));
        AddReading(station, 55.0, now.AddHours(-2));

        SetupStation(station, now);

        var response = await _handler.Handle(
            new ListStationReadingsQuery(station.Id, WindowHours: 24, Limit: 100),
            CancellationToken.None);

        Assert.Equal(3, response.Count);
        Assert.Equal(40.0, response[0].FillLevelPercent);
        Assert.Equal(55.0, response[1].FillLevelPercent);
        Assert.Equal(70.0, response[2].FillLevelPercent);

        Assert.Equal(station.Id, response[0].StationId);
        Assert.Equal(22.0,       response[0].TemperatureCelsius);
        Assert.Equal(90,         response[0].BatteryPercent);
        Assert.Equal(now.AddHours(-3), response[0].RecordedAt);
    }

    [Fact]
    public async Task Handle_ReadingsExceedLimit_ReturnsOnlyOldestN()
    {
        var station = NewStation();
        var now = DateTimeOffset.UtcNow;
        for (var i = 0; i < 5; i++)
        {
            AddReading(station, 10 * (i + 1), now.AddHours(-5 + i));
        }

        SetupStation(station, now);

        var response = await _handler.Handle(
            new ListStationReadingsQuery(station.Id, WindowHours: 24, Limit: 2),
            CancellationToken.None);

        Assert.Equal(2, response.Count);
        Assert.Equal(10.0, response[0].FillLevelPercent);
        Assert.Equal(20.0, response[1].FillLevelPercent);
    }

    [Fact]
    public async Task Handle_StationWithoutReadings_ReturnsEmptyList()
    {
        var station = NewStation();
        SetupStation(station, DateTimeOffset.UtcNow);

        var response = await _handler.Handle(
            new ListStationReadingsQuery(station.Id),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response);
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
                new ListStationReadingsQuery(stationId),
                CancellationToken.None));

        Assert.Contains(stationId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_PassesSinceComputedFromTimeProviderAndWindow()
    {
        var station = NewStation();
        var fixedNow = new DateTimeOffset(2026, 10, 1, 12, 0, 0, TimeSpan.Zero);
        var expectedSince = fixedNow.AddHours(-6);

        SetupStation(station, fixedNow);

        await _handler.Handle(
            new ListStationReadingsQuery(station.Id, WindowHours: 6, Limit: 100),
            CancellationToken.None);

        await _stations
            .Received(1)
            .GetWithReadingsAsync(
                station.Id,
                expectedSince,
                Arg.Any<CancellationToken>());
    }
}