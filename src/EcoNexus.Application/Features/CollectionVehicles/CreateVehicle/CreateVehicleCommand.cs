using EcoNexus.Contracts.CollectionVehicles;
using MediatR;

namespace EcoNexus.Application.Features.CollectionVehicles.CreateVehicle;

public sealed record CreateVehicleCommand(
    CreateCollectionVehicleRequest Request
) : IRequest<CollectionVehicleResponse>;
