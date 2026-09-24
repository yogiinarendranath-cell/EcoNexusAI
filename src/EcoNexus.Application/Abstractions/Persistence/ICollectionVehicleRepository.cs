using EcoNexus.Domain.Entities;

namespace EcoNexus.Application.Abstractions.Persistence;

public interface ICollectionVehicleRepository
{
    Task<IReadOnlyList<CollectionVehicle>> ListAsync(CancellationToken cancellationToken = default);

    Task<CollectionVehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default);

    Task AddAsync(CollectionVehicle vehicle, CancellationToken cancellationToken = default);
}
