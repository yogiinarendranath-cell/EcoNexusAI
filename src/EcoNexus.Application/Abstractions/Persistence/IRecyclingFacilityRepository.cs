using EcoNexus.Domain.Entities;

namespace EcoNexus.Application.Abstractions.Persistence;

/// <summary>
/// Persistence abstraction for the RecyclingFacility aggregate. Implemented
/// in the Infrastructure layer using EF Core.
/// </summary>
/// <remarks>
/// Implementations MUST translate EF Core persistence exceptions into the
/// Application-layer exceptions defined in
/// <c>EcoNexus.Application.Common.Exceptions</c>. In particular,
/// <c>Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException</c> MUST
/// be surfaced as <c>EcoNexus.Application.Common.Exceptions.ConcurrencyConflictException</c>.
/// </remarks>
public interface IRecyclingFacilityRepository
{
    /// <summary>Adds a new facility and commits the change.</summary>
    Task AddAsync(RecyclingFacility facility, CancellationToken cancellationToken = default);

    /// <summary>Loads a facility by id with its intakes, or null if not found. Tracked.</summary>
    Task<RecyclingFacility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns true if a facility with the given name already exists.</summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all facilities as tracked entities, including their intakes.
    /// Used by batch operations and the aggregate metrics endpoint.
    /// </summary>
    Task<IReadOnlyList<RecyclingFacility>> GetAllAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes tracked changes to the database.
    /// MUST translate EF Core's DbUpdateConcurrencyException into
    /// EcoNexus.Application.Common.Exceptions.ConcurrencyConflictException.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the facility for deletion. Caller must call SaveChangesAsync to persist.
    /// </summary>
    void Remove(RecyclingFacility facility);
}
