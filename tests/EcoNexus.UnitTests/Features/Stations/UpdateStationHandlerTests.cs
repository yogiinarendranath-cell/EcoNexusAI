using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Stations.UpdateStation;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class UpdateStationHandlerTests
{
    private readonly IWasteStationRepository _repository =
        Substitute.For<IWasteStationRepository>();

    private readonly UpdateStationHandler _handler;

    public UpdateStationHandlerTests()
    {
        _handler = new UpdateStationHandler(_repository);
    }

    private static WasteStation NewStation()
    {
        return WasteStation.Create(
            StationCode.Create("ST-0001"),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);
    }

    private static UpdateStationRequest ValidRequest() =>
        new(
            Latitude: 12.9352,
            Longitude: 77.6245,
            CapacityKilograms: 750,
            PrimaryCategory: "Glass");

    [Fact]
    public async Task Handle_ValidRequest_UpdatesStationAndSavesChanges()
    {
        var station = NewStation();
        var stationId = station.Id;

        _repository
            .GetByIdAsync(stationId, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = ValidRequest();

        await _handler.Handle(
            new UpdateStationCommand(stationId, request),
            CancellationToken.None);

        Assert.Equal(12.9352, station.Location.Latitude);
        Assert.Equal(77.6245, station.Location.Longitude);
        Assert.Equal(750, station.Capacity.Kilograms);
        Assert.Equal(WasteCategory.Glass, station.PrimaryCategory);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CategoryIsCaseInsensitive_ParsesLowercase()
    {
        var station = NewStation();

        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = new UpdateStationRequest(
            Latitude: 12.9352,
            Longitude: 77.6245,
            CapacityKilograms: 750,
            PrimaryCategory: "glass");

        await _handler.Handle(
            new UpdateStationCommand(station.Id, request),
            CancellationToken.None);

        Assert.Equal(WasteCategory.Glass, station.PrimaryCategory);
    }

    [Fact]
    public async Task Handle_StationDoesNotExist_ThrowsNotFoundException()
    {
        var stationId = Guid.NewGuid();

        _repository
            .GetByIdAsync(stationId, Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        var request = ValidRequest();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new UpdateStationCommand(stationId, request),
                CancellationToken.None));

        Assert.Contains(stationId.ToString(), ex.Message);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LatitudeOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var station = NewStation();
        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = new UpdateStationRequest(
            Latitude: 91.0,
            Longitude: 77.6245,
            CapacityKilograms: 750,
            PrimaryCategory: "Glass");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(
                new UpdateStationCommand(station.Id, request),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LongitudeOutOfRange_ThrowsArgumentOutOfRangeException()
    {
        var station = NewStation();
        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = new UpdateStationRequest(
            Latitude: 12.9352,
            Longitude: 181.0,
            CapacityKilograms: 750,
            PrimaryCategory: "Glass");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(
                new UpdateStationCommand(station.Id, request),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_LatitudeNaN_ThrowsArgumentException()
    {
        var station = NewStation();
        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = new UpdateStationRequest(
            Latitude: double.NaN,
            Longitude: 77.6245,
            CapacityKilograms: 750,
            PrimaryCategory: "Glass");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new UpdateStationCommand(station.Id, request),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeCapacity_ThrowsArgumentOutOfRangeException()
    {
        var station = NewStation();
        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = new UpdateStationRequest(
            Latitude: 12.9352,
            Longitude: 77.6245,
            CapacityKilograms: -1,
            PrimaryCategory: "Glass");

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(
                new UpdateStationCommand(station.Id, request),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ZeroCapacity_ThrowsArgumentException()
    {
        var station = NewStation();
        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = new UpdateStationRequest(
            Latitude: 12.9352,
            Longitude: 77.6245,
            CapacityKilograms: 0,
            PrimaryCategory: "Glass");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new UpdateStationCommand(station.Id, request),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownCategory_ThrowsArgumentException()
    {
        var station = NewStation();
        _repository
            .GetByIdAsync(station.Id, Arg.Any<CancellationToken>())
            .Returns(station);

        var request = new UpdateStationRequest(
            Latitude: 12.9352,
            Longitude: 77.6245,
            CapacityKilograms: 750,
            PrimaryCategory: "NotARealCategory");

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new UpdateStationCommand(station.Id, request),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}