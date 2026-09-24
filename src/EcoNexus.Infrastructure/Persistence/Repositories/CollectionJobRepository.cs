using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class CollectionJobRepository : ICollectionJobRepository
{
    private readonly EcoNexusDbContext _context;

    public CollectionJobRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CollectionJob>> ListAsync(CancellationToken cancellationToken = default)
        => await _context.CollectionJobs
            .AsNoTracking()
            .Include(j => j.Stops)
            .OrderByDescending(j => j.ScheduledFor)
            .ToListAsync(cancellationToken);

    public Task<CollectionJob?> GetByIdWithStopsAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CollectionJobs
            .Include(j => j.Stops)
            .FirstOrDefaultAsync(j => j.Id == id, cancellationToken);

    public async Task AddAsync(CollectionJob job, CancellationToken cancellationToken = default)
    {
        await _context.CollectionJobs.AddAsync(job, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
