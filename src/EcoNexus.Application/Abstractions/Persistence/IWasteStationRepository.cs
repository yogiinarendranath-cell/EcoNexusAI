using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Abstractions.Persistence;

/// <summary>
/// Persistence abstraction for the WasteStation aggregate. Implemented in the
/// infrastructure layer using EF Core. Handlers depend on this interface,
/// not on DbContext.
/// </summary>
/// <remarks>
/// Implementations MUST translate EF Core persistence exceptions into the
/// Application-layer exceptions defined in
/// <c>EcoNexus.Application.Common.Exceptions</c>. In particular,
/// <c>Microsoft.EntityFrameworkCore.DbUpdateConcurrencyException</c> MUST
/// be surfaced as <c>EcoNexus.Application.Common.Exceptions.ConcurrencyConflictException</c>.
/// This keeps EF Core out of the Application layer's dependency graph.
/// </remarks>
public interface IWasteStationRepository
{
    /// <summary>Adds a new station and commits the change.</summary>
    Task AddAsync(WasteStation station, CancellationToken cancellationToken = default);

    /// <summary>Loads a station by id, or null if not found. Tracked by EF Core.</summary>
    Task<WasteStation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns true if a station with the given code already exists.</summary>
    Task<bool> CodeExistsAsync(StationCode code, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a page of stations matching the query, plus the total count.
    /// </summary>
    Task<(IReadOnlyList<WasteStation> Items, int TotalCount)> ListAsync(
        StationListQuery query,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all active stations as tracked entities. Used by batch operations
    /// such as the IoT simulator that need to read-modify-write many stations.
    /// </summary>
    Task<IReadOnlyList<WasteStation>> GetActiveAsync(
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a station together with its readings recorded at or after <paramref name="since"/>.
    /// Used by the fill-level forecaster, which needs the recent reading window.
    /// Returns null if no station with that id exists.
    /// </summary>
    Task<WasteStation?> GetWithReadingsAsync(
        Guid id,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes tracked changes to the database.
    /// MUST translate EF Core's DbUpdateConcurrencyException into
    /// EcoNexus.Application.Common.Exceptions.ConcurrencyConflictException.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the station for deletion. Caller must call SaveChangesAsync to persist.
    /// </summary>
    void Remove(WasteStation station);
}