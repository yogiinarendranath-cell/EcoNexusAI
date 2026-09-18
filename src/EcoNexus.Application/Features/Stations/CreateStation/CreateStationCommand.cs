using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.CreateStation;

/// <summary>Command: create a new waste station.</summary>
public sealed record CreateStationCommand(CreateStationRequest Request)
    : IRequest<CreateStationResponse>;
