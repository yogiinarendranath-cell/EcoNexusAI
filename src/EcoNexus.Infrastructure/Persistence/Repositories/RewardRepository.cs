using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class RewardRepository : IRewardRepository
{
    private readonly EcoNexusDbContext _context;

    public RewardRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(Reward reward, CancellationToken cancellationToken = default)
    {
        await _context.Rewards.AddAsync(reward, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<Reward?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Rewards
            .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

    public async Task<IReadOnlyList<Reward>> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rewards
            .Where(r => r.IsActive)
            .OrderBy(r => r.CostInPoints)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Reward>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Rewards
            .OrderBy(r => r.CostInPoints)
            .ToListAsync(cancellationToken);
    }

    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
        => _context.Rewards
            .AnyAsync(r => r.Name == name, cancellationToken);

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

    public void Remove(Reward reward)
    {
        ArgumentNullException.ThrowIfNull(reward);
        _context.Rewards.Remove(reward);
    }
}
