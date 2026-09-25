using EcoNexus.Domain.Entities;

namespace EcoNexus.Application.Abstractions.Persistence;

/// <summary>
/// Persistence abstraction for the CitizenProfile aggregate.
/// </summary>
public interface ICitizenProfileRepository
{
    /// <summary>Adds a new profile and commits.</summary>
    Task AddAsync(CitizenProfile profile, CancellationToken cancellationToken = default);

    /// <summary>Loads a profile by its own Id, with transactions. Tracked.</summary>
    Task<CitizenProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Loads a profile by the owning Identity user id, with transactions. Tracked.</summary>
    Task<CitizenProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>Returns true if a profile already exists for the given user.</summary>
    Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes tracked changes. MUST translate EF Core's
    /// DbUpdateConcurrencyException into ConcurrencyConflictException.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks the profile for deletion.</summary>
    void Remove(CitizenProfile profile);
}
