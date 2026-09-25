using EcoNexus.Domain.Entities;

namespace EcoNexus.Application.Abstractions.Persistence;

/// <summary>
/// Persistence abstraction for the CitizenReport aggregate.
/// </summary>
public interface ICitizenReportRepository
{
    /// <summary>Adds a new report and commits the change.</summary>
    Task AddAsync(CitizenReport report, CancellationToken cancellationToken = default);

    /// <summary>Loads a report by id, or null. Tracked.</summary>
    Task<CitizenReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all reports filed by the given user, newest first.
    /// Used by the citizen's own "my reports" view.
    /// </summary>
    Task<IReadOnlyList<CitizenReport>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Flushes tracked changes. MUST translate EF Core's
    /// DbUpdateConcurrencyException into ConcurrencyConflictException.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Marks the report for deletion.</summary>
    void Remove(CitizenReport report);
}
