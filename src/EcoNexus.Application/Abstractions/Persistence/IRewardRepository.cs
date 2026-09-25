using EcoNexus.Domain.Entities;

namespace EcoNexus.Application.Abstractions.Persistence;

/// <summary>
/// Persistence abstraction for the Reward catalog.
/// </summary>
public interface IRewardRepository
{
    /// <summary>Adds a new reward and commits.</summary>
    Task AddAsync(Reward reward, CancellationToken cancellationToken = default);

    /// <summary>Loads a reward by id, or null. Tracked.</summary>
    Task<Reward?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns all currently active rewards, ordered by cost ascending.</summary>
    Task<IReadOnlyList<Reward>> GetActiveAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns all rewards (including inactive), ordered by cost ascending.</summary>
    Task<IReadOnlyList<Reward>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Returns true if a reward with the given name already exists.</summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes tracked changes. MUST translate EF Core's
    /// DbUpdateConcurrencyException into ConcurrencyConflictException.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks the reward for deletion.</summary>
    void Remove(Reward reward);
}
