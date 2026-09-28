using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.CollectionJobs.ScheduleJob;
using EcoNexus.Contracts.CollectionJobs;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.CollectionJobs;

public sealed class ScheduleJobHandlerTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly ICollectionJobRepository _jobs = Substitute.For<ICollectionJobRepository>();
    private readonly IWasteStationRepository _stations = Substitute.For<IWasteStationRepository>();
    private readonly ICollectionVehicleRepository _vehicles = Substitute.For<ICollectionVehicleRepository>();
    private readonly ScheduleJobHandler _handler;

    public ScheduleJobHandlerTests()
    {
        _handler = new ScheduleJobHandler(_jobs, _stations, _vehicles);
    }

    // -------------------- Fixtures --------------------

    private static CollectionVehicle ActiveVehicle(string reg = "MH-12-AB-1234")
        => CollectionVehicle.Create(reg, Weight.FromKilograms(5000));

    private static WasteStation NewStation(string code = "ST-9001")
        => WasteStation.Create(
            StationCode.Create(code),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            EcoNexus.Domain.Enums.WasteCategory.Plastic);

    private static ScheduleJobRequest Request(
        Guid vehicleId,
        params Guid[] stationIds)
        => new(
            VehicleId: vehicleId,
            ScheduledFor: T0.AddHours(2),
            Stops: stationIds.Select(id => new ScheduleJobStop(id)).ToList());

    private void VehicleExists(CollectionVehicle vehicle)
        => _vehicles.GetByIdAsync(vehicle.Id, Arg.Any<CancellationToken>()).Returns(vehicle);

    private void StationExists(Guid stationId)
    {
        var station = NewStation();
        typeof(WasteStation).GetProperty("Id")!.SetValue(station, stationId);
        _stations.GetByIdAsync(stationId, Arg.Any<CancellationToken>()).Returns(station);
    }

    // -------------------- Happy path --------------------

    [Fact]
    public async Task Handle_ValidRequest_CreatesJobWithStops()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        var s3 = Guid.NewGuid();
        StationExists(s1);
        StationExists(s2);
        StationExists(s3);

        var request = Request(vehicle.Id, s1, s2, s3);

        var response = await _handler.Handle(
            new ScheduleJobCommand(request),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.Id);
        Assert.Equal(vehicle.Id, response.VehicleId);
        Assert.Equal("Scheduled", response.Status);
        Assert.Equal(3, response.Stops.Count);
        Assert.Equal(0, response.TotalCollectedKilograms);

        await _jobs
            .Received(1)
            .AddAsync(
                Arg.Is<CollectionJob>(j => j.Stops.Count == 3),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_AssignsStopSequencesInOrder()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var s1 = Guid.NewGuid();
        var s2 = Guid.NewGuid();
        StationExists(s1);
        StationExists(s2);

        var request = Request(vehicle.Id, s1, s2);

        var response = await _handler.Handle(
            new ScheduleJobCommand(request),
            CancellationToken.None);

        Assert.Equal(2, response.Stops.Count);
        Assert.Equal(1, response.Stops[0].Sequence);
        Assert.Equal(2, response.Stops[1].Sequence);
        Assert.Equal(s1, response.Stops[0].StationId);
        Assert.Equal(s2, response.Stops[1].StationId);
    }

    // -------------------- Vehicle guards --------------------

    [Fact]
    public async Task Handle_VehicleNotFound_ThrowsNotFoundException()
    {
        var vehicleId = Guid.NewGuid();
        _vehicles.GetByIdAsync(vehicleId, Arg.Any<CancellationToken>())
            .Returns((CollectionVehicle?)null);

        var request = Request(vehicleId, Guid.NewGuid());

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ScheduleJobCommand(request), CancellationToken.None));
    }

    [Fact]
    public async Task Handle_InactiveVehicle_ThrowsConflictException()
    {
        var vehicle = ActiveVehicle();
        vehicle.Deactivate();
        VehicleExists(vehicle);

        var request = Request(vehicle.Id, Guid.NewGuid());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new ScheduleJobCommand(request), CancellationToken.None));
    }

    // -------------------- Stops guards --------------------

    [Fact]
    public async Task Handle_EmptyStops_ThrowsConflictException()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var request = new ScheduleJobRequest(
            VehicleId: vehicle.Id,
            ScheduledFor: T0.AddHours(2),
            Stops: Array.Empty<ScheduleJobStop>());

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new ScheduleJobCommand(request), CancellationToken.None));

        await _jobs.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    [Fact]
    public async Task Handle_StationNotFound_ThrowsNotFoundException()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var missingStationId = Guid.NewGuid();
        _stations.GetByIdAsync(missingStationId, Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        var request = Request(vehicle.Id, missingStationId);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new ScheduleJobCommand(request), CancellationToken.None));

        await _jobs.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // -------------------- Duplicate station IDs --------------------

    [Fact]
    public async Task Handle_DuplicateStationIds_LooksUpOncePerDistinctId()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var stationId = Guid.NewGuid();
        StationExists(stationId);

        // Same station listed twice in the request.
        var request = Request(vehicle.Id, stationId, stationId);

        var response = await _handler.Handle(
            new ScheduleJobCommand(request),
            CancellationToken.None);

        // Handler dedupes for validation but still adds both stops.
        await _stations.Received(1).GetByIdAsync(stationId, Arg.Any<CancellationToken>());
        Assert.Equal(2, response.Stops.Count);
    }

    // -------------------- Scheduled-for passthrough --------------------

    [Fact]
    public async Task Handle_ValidRequest_PreservesScheduledFor()
    {
        var vehicle = ActiveVehicle();
        VehicleExists(vehicle);

        var s1 = Guid.NewGuid();
        StationExists(s1);

        var scheduled = T0.AddDays(1);
        var request = new ScheduleJobRequest(
            VehicleId: vehicle.Id,
            ScheduledFor: scheduled,
            Stops: new[] { new ScheduleJobStop(s1) });

        var response = await _handler.Handle(
            new ScheduleJobCommand(request),
            CancellationToken.None);

        Assert.Equal(scheduled, response.ScheduledFor);
    }
}