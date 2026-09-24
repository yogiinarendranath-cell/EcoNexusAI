using EcoNexus.Domain.Entities;

namespace EcoNexus.Application.Abstractions.Persistence;

public interface ICollectionJobRepository
{
    Task<IReadOnlyList<CollectionJob>> ListAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a job with its Stops eagerly included. Needed because callers
    /// read job.Stops after the query returns.
    /// </summary>
    Task<CollectionJob?> GetByIdWithStopsAsync(Guid id, CancellationToken cancellationToken = default);

    Task AddAsync(CollectionJob job, CancellationToken cancellationToken = default);
}
