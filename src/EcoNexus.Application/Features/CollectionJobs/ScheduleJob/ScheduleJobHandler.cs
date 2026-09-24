using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.CollectionJobs.ListJobs;
using EcoNexus.Contracts.CollectionJobs;
using EcoNexus.Domain.Entities;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.ScheduleJob;

internal sealed class ScheduleJobHandler
    : IRequestHandler<ScheduleJobCommand, CollectionJobResponse>
{
    private readonly ICollectionJobRepository _jobs;
    private readonly IWasteStationRepository _stations;
    private readonly ICollectionVehicleRepository _vehicles;

    public ScheduleJobHandler(
        ICollectionJobRepository jobs,
        IWasteStationRepository stations,
        ICollectionVehicleRepository vehicles)
    {
        _jobs = jobs;
        _stations = stations;
        _vehicles = vehicles;
    }

    public async Task<CollectionJobResponse> Handle(
        ScheduleJobCommand command,
        CancellationToken cancellationToken)
    {
        var request = command.Request;

        // ---- Guard: vehicle must exist and be active ----
        var vehicle = await _vehicles.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException($"Vehicle '{request.VehicleId}' was not found.");

        if (!vehicle.IsActive)
        {
            throw new ConflictException($"Vehicle '{vehicle.RegistrationNumber}' is not active.");
        }

        // ---- Guard: at least one stop ----
        if (request.Stops.Count == 0)
        {
            throw new ConflictException("A collection job must have at least one stop.");
        }

        // ---- Guard: every station must exist ----
        var stationIds = request.Stops.Select(s => s.StationId).Distinct().ToList();
        foreach (var stationId in stationIds)
        {
            var station = await _stations.GetByIdAsync(stationId, cancellationToken);
            if (station is null)
            {
                throw new NotFoundException($"Station '{stationId}' was not found.");
            }
        }

        // ---- Build the aggregate ----
        var job = CollectionJob.Schedule(request.VehicleId, request.ScheduledFor);

        foreach (var stop in request.Stops)
        {
            job.AddStop(stop.StationId);
        }

        await _jobs.AddAsync(job, cancellationToken);

        return ListJobsHandler.Map(job);
    }
}
