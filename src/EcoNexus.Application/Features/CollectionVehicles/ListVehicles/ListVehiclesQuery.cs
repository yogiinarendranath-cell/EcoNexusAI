using EcoNexus.Contracts.CollectionVehicles;
using MediatR;

namespace EcoNexus.Application.Features.CollectionVehicles.ListVehicles;

public sealed record ListVehiclesQuery() : IRequest<IReadOnlyList<CollectionVehicleResponse>>;
