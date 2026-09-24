using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.CollectionJobs;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Services;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.PreviewRoute;

internal sealed class PreviewRouteHandler
    : IRequestHandler<PreviewRouteQuery, PreviewRouteResponse>
{
    private readonly ICollectionVehicleRepository _vehicles;
    private readonly IWasteStationRepository _stations;

    public PreviewRouteHandler(
        ICollectionVehicleRepository vehicles,
        IWasteStationRepository stations)
    {
        _vehicles = vehicles;
        _stations = stations;
    }

    public async Task<PreviewRouteResponse> Handle(
        PreviewRouteQuery query,
        CancellationToken cancellationToken)
    {
        var request = query.Request;

        // 1. Load the vehicle. Must exist.
        var vehicle = await _vehicles.GetByIdAsync(request.VehicleId, cancellationToken)
            ?? throw new NotFoundException($"Vehicle '{request.VehicleId}' was not found.");

        // 2. Load every candidate station. Missing ones are counted, not fatal.
        var loadedStations = new List<WasteStation>(request.CandidateStationIds.Count);
        var missingCount = 0;

        foreach (var stationId in request.CandidateStationIds.Distinct())
        {
            var station = await _stations.GetByIdAsync(stationId, cancellationToken);
            if (station is null)
            {
                missingCount++;
                continue;
            }

            loadedStations.Add(station);
        }

        // 3. If no stations were found, return an empty route.
        if (loadedStations.Count == 0)
        {
            return new PreviewRouteResponse(
                vehicle.Id,
                vehicle.Capacity.Kilograms,
                Array.Empty<PreviewRouteStop>(),
                0,
                missingCount);
        }

        // 4. Determine where the vehicle starts. Until we model depots or
        //    track last-known vehicle positions, we anchor at the first
        //    candidate so distances are relative and the output is deterministic.
        var startLocation = loadedStations[0].Location;

        // 5. Build optimizer candidates. Estimated weight is fill% × capacity.
        var byId = loadedStations.ToDictionary(s => s.Id);
        var candidates = loadedStations
            .Select(s => new RouteCandidate(
                s.Id,
                s.Location,
                s.CurrentFill,
                Weight.FromKilograms(s.Capacity.Kilograms * s.CurrentFill.Percent / 100.0)))
            .ToList();

        // 6. Run the optimizer.
        var orderedIds = RouteOptimizer.Optimize(startLocation, vehicle.Capacity, candidates);

        // 7. Map back to a response with per-stop reasoning.
        var stops = new List<PreviewRouteStop>(orderedIds.Count);
        var totalWeight = 0.0;

        for (var i = 0; i < orderedIds.Count; i++)
        {
            var station = byId[orderedIds[i]];
            var estimatedWeight = station.Capacity.Kilograms * station.CurrentFill.Percent / 100.0;
            totalWeight += estimatedWeight;

            stops.Add(new PreviewRouteStop(
                station.Id,
                station.Code.Value,
                i + 1,
                station.CurrentFill.Percent,
                Math.Round(estimatedWeight, 2)));
        }

        return new PreviewRouteResponse(
            vehicle.Id,
            vehicle.Capacity.Kilograms,
            stops,
            Math.Round(totalWeight, 2),
            missingCount);
    }
}
