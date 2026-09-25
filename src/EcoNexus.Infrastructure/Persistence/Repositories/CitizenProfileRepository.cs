using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class CitizenProfileRepository : ICitizenProfileRepository
{
    private readonly EcoNexusDbContext _context;

    public CitizenProfileRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(CitizenProfile profile, CancellationToken cancellationToken = default)
    {
        await _context.CitizenProfiles.AddAsync(profile, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<CitizenProfile?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CitizenProfiles
            .Include(c => c.Transactions)
            .Include(c => c.Classifications)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<CitizenProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.CitizenProfiles
            .Include(c => c.Transactions)
            .Include(c => c.Classifications)
            .FirstOrDefaultAsync(c => c.UserId == userId, cancellationToken);

    public Task<bool> ExistsForUserAsync(Guid userId, CancellationToken cancellationToken = default)
        => _context.CitizenProfiles
            .AnyAsync(c => c.UserId == userId, cancellationToken);

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

    public void Remove(CitizenProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        _context.CitizenProfiles.Remove(profile);
    }
}
