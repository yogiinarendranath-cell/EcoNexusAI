using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.CollectionJobs.PreviewRoute;
using EcoNexus.Contracts.CollectionJobs;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.CollectionJobs;

public sealed class PreviewRouteHandlerTests
{
    private readonly ICollectionVehicleRepository _vehicles = Substitute.For<ICollectionVehicleRepository>();
    private readonly IWasteStationRepository _stations = Substitute.For<IWasteStationRepository>();
    private readonly PreviewRouteHandler _handler;

    public PreviewRouteHandlerTests()
    {
        _handler = new PreviewRouteHandler(_vehicles, _stations);
    }

    // -------------------- Fixtures --------------------

    private static CollectionVehicle ActiveVehicle(string reg = "MH-12-AB-1234", int capKg = 5000)
        => CollectionVehicle.Create(reg, Weight.FromKilograms(capKg));

    private static WasteStation NewStation(string code, double fillPercent, int capacityKg = 500,
        double latOffset = 0, double lngOffset = 0)
    {
        // Distinct locations are required by the optimizer: same-location
        // stations collapse to a single stop.
        var station = WasteStation.Create(
            StationCode.Create(code),
            Location.Create(12.9716 + latOffset, 77.5946 + lngOffset),
            Weight.FromKilograms(capacityKg),
            WasteCategory.Plastic);
        station.RecordReading(
            FillLevel.FromPercent(fillPercent),
            temperatureCelsius: 20,
            batteryPercent: 100,
            recordedAt: DateTimeOffset.UtcNow);
        return station;
    }

    private void VehicleExists(CollectionVehicle vehicle)
        => _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

    private void StationReturned(Guid id, WasteStation station)
        => _stations.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns(station);

    // -------------------- Vehicle guard --------------------

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsNotFoundException()
    {
        var vehicleId = Guid.NewGuid();
        _vehicles.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns((CollectionVehicle?)null);

        var request = new PreviewRouteRequest(vehicleId, new[] { Guid.NewGuid() });

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new PreviewRouteQuery(request), CancellationToken.None));
    }

    // -------------------- Empty candidates --------------------

    [Fact]
    public async Task Handle_NoCandidateStations_ReturnsEmptyRoute()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);
        _stations.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        var request = new PreviewRouteRequest(
            vehicle.Id,
            new[] { Guid.NewGuid(), Guid.NewGuid() });

        var response = await _handler.Handle(
            new PreviewRouteQuery(request),
            CancellationToken.None);

        Assert.Empty(response.Stops);
        Assert.Equal(0, response.TotalEstimatedWeightKilograms);
        Assert.Equal(2, response.SkippedCount);
        Assert.Equal(vehicle.Id, response.VehicleId);
    }

    [Fact]
    public async Task Handle_EmptyCandidateList_ReturnsEmptyRouteWithZeroSkipped()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var request = new PreviewRouteRequest(
            vehicle.Id,
            Array.Empty<Guid>());

        var response = await _handler.Handle(
            new PreviewRouteQuery(request),
            CancellationToken.None);

        Assert.Empty(response.Stops);
        Assert.Equal(0, response.SkippedCount);
    }

    // -------------------- Partial missing --------------------

    [Fact]
    public async Task Handle_SomeStationsMissing_ProcessesFoundOnes()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var presentId = Guid.NewGuid();
        var missingId = Guid.NewGuid();

        StationReturned(presentId, NewStation("ST-1001", fillPercent: 60, latOffset: 0.001));
        _stations.GetByIdAsync(missingId, Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        // Add a second present station so the optimizer has â‰¥ 2 candidates.
        var secondPresentId = Guid.NewGuid();
        StationReturned(secondPresentId, NewStation("ST-1002", fillPercent: 70, latOffset: 0.002));

        var request = new PreviewRouteRequest(
            vehicle.Id,
            new[] { presentId, missingId, secondPresentId });

        var response = await _handler.Handle(
            new PreviewRouteQuery(request),
            CancellationToken.None);

        Assert.Equal(2, response.Stops.Count);
        Assert.Equal(1, response.SkippedCount);
    }

    // -------------------- Happy path (2 stations) --------------------

    [Fact]
    public async Task Handle_TwoStations_ReturnsTwoStopsWithSequencesOneAndTwo()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        StationReturned(s1, NewStation("ST-1001", fillPercent: 70, latOffset: 0.001));
        StationReturned(s2, NewStation("ST-1002", fillPercent: 60, latOffset: 0.002));

        var request = new PreviewRouteRequest(vehicle.Id, new[] { s1, s2 });

        var response = await _handler.Handle(
            new PreviewRouteQuery(request),
            CancellationToken.None);

        Assert.Equal(2, response.Stops.Count);
        Assert.Equal(1, response.Stops[0].Sequence);
        Assert.Equal(2, response.Stops[1].Sequence);
        Assert.Contains(response.Stops, s => s.StationCode == "ST-1001");
        Assert.Contains(response.Stops, s => s.StationCode == "ST-1002");
    }

    [Fact]
    public async Task Handle_EstimatedWeight_IsFillTimesCapacity()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        // 40% fill of 1000kg capacity = 400kg estimated
        StationReturned(s1, NewStation("ST-1001", fillPercent: 80, capacityKg: 500, latOffset: 0.001));
        // 50% fill of 800kg capacity = 400kg estimated
        StationReturned(s2, NewStation("ST-1002", fillPercent: 80, capacityKg: 500, latOffset: 0.002));

        var request = new PreviewRouteRequest(vehicle.Id, new[] { s1, s2 });

        var response = await _handler.Handle(
            new PreviewRouteQuery(request),
            CancellationToken.None);

        Assert.Equal(2, response.Stops.Count);
        // Both stops contribute 400 kg â†’ total 800 kg
        Assert.Equal(800.0, response.TotalEstimatedWeightKilograms, precision: 1);

        foreach (var stop in response.Stops)
        {
            Assert.Equal(400.0, stop.EstimatedWeightKilograms, precision: 1);
        }
    }

    // -------------------- Distinct candidate IDs --------------------

    [Fact]
    public async Task Handle_DuplicateCandidateIds_LooksUpOncePerDistinctId()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        StationReturned(s1, NewStation("ST-1001", fillPercent: 70, latOffset: 0.001));
        StationReturned(s2, NewStation("ST-1002", fillPercent: 70, latOffset: 0.002));

        // s1 duplicated three times, s2 once.
        var request = new PreviewRouteRequest(
            vehicle.Id,
            new[] { s1, s1, s1, s2 });

        var response = await _handler.Handle(
            new PreviewRouteQuery(request),
            CancellationToken.None);

        await _stations.Received(1).GetByIdAsync(s1, Arg.Any<CancellationToken>());
        Assert.Equal(2, response.Stops.Count);
    }

    // -------------------- Response shape --------------------

    [Fact]
    public async Task Handle_Response_IncludesVehicleCapacity()
    {
        var vehicle = ActiveVehicle(capKg: 7500);
        VehicleExists(vehicle);

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        StationReturned(s1, NewStation("ST-1001", fillPercent: 20, latOffset: 0.001));
        StationReturned(s2, NewStation("ST-1002", fillPercent: 30, latOffset: 0.002));

        var request = new PreviewRouteRequest(vehicle.Id, new[] { s1, s2 });

        var response = await _handler.Handle(
            new PreviewRouteQuery(request),
            CancellationToken.None);

        Assert.Equal(7500, response.VehicleCapacityKilograms);
        Assert.Equal(vehicle.Id, response.VehicleId);
    }
}