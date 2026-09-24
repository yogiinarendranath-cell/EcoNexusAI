using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.CollectionVehicles;
using MediatR;

namespace EcoNexus.Application.Features.CollectionVehicles.ListVehicles;

internal sealed class ListVehiclesHandler
    : IRequestHandler<ListVehiclesQuery, IReadOnlyList<CollectionVehicleResponse>>
{
    private readonly ICollectionVehicleRepository _repository;

    public ListVehiclesHandler(ICollectionVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CollectionVehicleResponse>> Handle(
        ListVehiclesQuery request,
        CancellationToken cancellationToken)
    {
        var vehicles = await _repository.ListAsync(cancellationToken);

        return vehicles
            .Select(v => new CollectionVehicleResponse(
                v.Id,
                v.RegistrationNumber,
                v.Capacity.Kilograms,
                v.IsActive))
            .ToList();
    }
}
