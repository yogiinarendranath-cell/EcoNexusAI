using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class AssistantInteractionRepository : IAssistantInteractionRepository
{
    private readonly EcoNexusDbContext _context;

    public AssistantInteractionRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task AddAsync(
        AssistantInteraction interaction,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(interaction);

        await _context.Set<AssistantInteraction>().AddAsync(interaction, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public Task<AssistantInteraction?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _context.Set<AssistantInteraction>()
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id, cancellationToken);

    public async Task<IReadOnlyList<AssistantInteraction>> GetRecentForUserAsync(
        Guid userId,
        int take,
        CancellationToken cancellationToken = default)
    {
        // Guard against bad input from callers; clamp to a sane window.
        var limit = take switch
        {
            < 1 => 10,
            > 100 => 100,
            _ => take,
        };

        return await _context.Set<AssistantInteraction>()
            .AsNoTracking()
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.OccurredAt)
            .Take(limit)
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
            // Modified or Deleted entity and affected fewer. Surface it as a
            // domain-meaningful exception.
            //
            // False conflict: EF classified an Added entity as Modified.
            // That's a model bug — rethrow as-is so it's loud.
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
}
