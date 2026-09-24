using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class RecyclingFacilityRepository : IRecyclingFacilityRepository
{
    private readonly EcoNexusDbContext _context;

    public RecyclingFacilityRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(RecyclingFacility facility, CancellationToken cancellationToken = default)
    {
        await _context.RecyclingFacilities.AddAsync(facility, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<RecyclingFacility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.RecyclingFacilities
            .Include(f => f.Intakes)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
        => _context.RecyclingFacilities
            .AnyAsync(f => f.Name == name, cancellationToken);

    public async Task<IReadOnlyList<RecyclingFacility>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _context.RecyclingFacilities
            .Include(f => f.Intakes)
            .OrderBy(f => f.Name)
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
            // Genuine concurrency conflict: EF expected to affect N rows on a
            // Modified or Deleted entity and affected fewer.
            //
            // False concurrency conflict: EF classified an Added entity as
            // Modified and issued an UPDATE that matched zero rows. Rethrow
            // as-is so the model bug is loud, not wrapped.
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

    public void Remove(RecyclingFacility facility)
    {
        ArgumentNullException.ThrowIfNull(facility);
        _context.RecyclingFacilities.Remove(facility);
    }
}
