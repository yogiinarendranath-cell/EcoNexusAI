using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class WasteStationRepository : IWasteStationRepository
{
    private readonly EcoNexusDbContext _context;

    public WasteStationRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(WasteStation station, CancellationToken cancellationToken = default)
    {
        await _context.WasteStations.AddAsync(station, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<WasteStation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.WasteStations
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

    public Task<bool> CodeExistsAsync(StationCode code, CancellationToken cancellationToken = default)
        => _context.WasteStations
            .AnyAsync(s => s.Code == code, cancellationToken);

    public async Task<IReadOnlyList<WasteStation>> GetActiveAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.WasteStations
            .Where(s => s.Status == StationStatus.Online)
            .ToListAsync(cancellationToken);
    }

    public async Task<(IReadOnlyList<WasteStation> Items, int TotalCount)> ListAsync(
        StationListQuery query,
        CancellationToken cancellationToken = default)
    {
        var q = _context.WasteStations.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status)
            && Enum.TryParse<StationStatus>(query.Status, ignoreCase: true, out var status))
        {
            q = q.Where(s => s.Status == status);
        }

        if (!string.IsNullOrWhiteSpace(query.Category)
            && Enum.TryParse<WasteCategory>(query.Category, ignoreCase: true, out var category))
        {
            q = q.Where(s => s.PrimaryCategory == category);
        }

        if (query.CriticalOnly)
        {
            q = q.Where(s => s.CurrentFill.Percent >= FillLevel.CriticalThresholdPercent);
        }

        var total = await q.CountAsync(cancellationToken);

        var sortBy = (query.SortBy ?? "code").Trim().ToLowerInvariant();

        q = sortBy switch
        {
            "filllevel" => query.SortDesc
                ? q.OrderByDescending(s => s.CurrentFill.Percent).ThenBy(s => s.Code)
                : q.OrderBy(s => s.CurrentFill.Percent).ThenBy(s => s.Code),
            "lastupdated" => query.SortDesc
                ? q.OrderByDescending(s => s.LastUpdatedAt).ThenBy(s => s.Code)
                : q.OrderBy(s => s.LastUpdatedAt).ThenBy(s => s.Code),
            _ => query.SortDesc
                ? q.OrderByDescending(s => s.Code)
                : q.OrderBy(s => s.Code)
        };

        var page = query.Page < 1 ? 1 : query.Page;
        var pageSize = query.PageSize is < 1 or > 100 ? 20 : query.PageSize;

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, total);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            // Genuine concurrency conflict: EF expected to affect N rows on a
            // Modified or Deleted entity and affected fewer. Surface that as a
            // domain-meaningful exception the Application layer can react to.
            //
            // False concurrency conflict: EF classified an Added entity as
            // Modified (e.g. because its client-assigned Id was non-empty),
            // issued UPDATE ... WHERE Id = <new>, matched 0 rows, and threw.
            // That is a model configuration bug, not a concurrency condition.
            // Rethrow as-is so the failure is loud instead of silently wrapped.
            var hadExpectedModificationOrDeletion = ex.Entries.Any(e =>
                e.State == EntityState.Modified || e.State == EntityState.Deleted);

            if (!hadExpectedModificationOrDeletion)
            {
                throw;
            }

            throw new ConcurrencyConflictException(
                "A concurrency conflict occurred while saving changes.", ex);
        }
    }

    public void Remove(WasteStation station)
    {
        ArgumentNullException.ThrowIfNull(station);
        _context.WasteStations.Remove(station);
    }
}
