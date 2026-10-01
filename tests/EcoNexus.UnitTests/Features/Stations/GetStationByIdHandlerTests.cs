using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Stations.GetStationById;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class GetStationByIdHandlerTests
{
    private readonly IWasteStationRepository _repository =
        Substitute.For<IWasteStationRepository>();

    private readonly GetStationByIdHandler _handler;

    public GetStationByIdHandlerTests()
    {
        _handler = new GetStationByIdHandler(_repository);
    }

    private static WasteStation NewStation()
        => WasteStation.Create(
            StationCode.Create("ST-0001"),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);

    [Fact]
    public async Task Handle_StationExists_ReturnsMappedDetailResponse()
    {
        var station = NewStation();
        var recordedAt = DateTimeOffset.UtcNow;
        station.RecordReading(
            FillLevel.FromPercent(62.5),
            temperatureCelsius: 24.0,
            batteryPercent: 88,
            recordedAt: recordedAt);

        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var response = await _handler.Handle(
            new GetStationByIdQuery(station.Id),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(station.Id,                  response.Id);
        Assert.Equal("ST-0001",                   response.Code);
        Assert.Equal(12.9716,                     response.Latitude);
        Assert.Equal(77.5946,                     response.Longitude);
        Assert.Equal(500,                         response.CapacityKilograms);
        Assert.Equal(62.5,                        response.CurrentFillPercent);
        Assert.Equal("Plastic",                   response.PrimaryCategory);
        Assert.Equal("Online",                    response.Status);
        Assert.Equal(recordedAt,                  response.LastUpdatedAt);
        Assert.Null(response.LastCollectedAt);

        await _repository
            .Received(1)
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StationDoesNotExist_ReturnsNull()
    {
        var stationId = Guid.NewGuid();

        _repository
            .GetByIdAsync(stationId, Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        var exception = await Record.ExceptionAsync(async () =>
        {
            var response = await _handler.Handle(
                new GetStationByIdQuery(stationId),
                CancellationToken.None);
            Assert.Null(response);
        });

        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_StationIsOffline_ResponseReflectsOfflineStatus()
    {
        var station = NewStation();
        station.MarkOffline(DateTimeOffset.UtcNow);

        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var response = await _handler.Handle(
            new GetStationByIdQuery(station.Id),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal("Offline", response.Status);
    }

    [Fact]
    public async Task Handle_IsReadOnly_DoesNotWriteToRepository()
    {
        var station = NewStation();

        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        await _handler.Handle(
            new GetStationByIdQuery(station.Id),
            CancellationToken.None);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<WasteStation>(), Arg.Any<CancellationToken>());
        _repository
            .DidNotReceive()
            .Remove(Arg.Any<WasteStation>());
    }
}