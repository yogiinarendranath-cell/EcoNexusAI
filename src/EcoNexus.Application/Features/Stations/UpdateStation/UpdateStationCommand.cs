using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.UpdateStation;

/// <summary>Command: update the mutable metadata of an existing waste station.</summary>
public sealed record UpdateStationCommand(Guid StationId, UpdateStationRequest Request)
    : IRequest;
