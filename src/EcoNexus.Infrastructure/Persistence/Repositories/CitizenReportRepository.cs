using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class CitizenReportRepository : ICitizenReportRepository
{
    private readonly EcoNexusDbContext _context;

    public CitizenReportRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CitizenReport report, CancellationToken cancellationToken = default)
    {
        await _context.CitizenReports.AddAsync(report, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<CitizenReport?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CitizenReports
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<CitizenReport>> ListByUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        return await _context.CitizenReports
            .Where(r => r.FiledByUserId == userId)
            .OrderByDescending(r => r.FiledAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            return await _context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
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

    public void Remove(CitizenReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        _context.CitizenReports.Remove(report);
    }
}
