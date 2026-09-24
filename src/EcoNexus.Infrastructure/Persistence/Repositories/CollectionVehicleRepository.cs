using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

internal sealed class CollectionVehicleRepository : ICollectionVehicleRepository
{
    private readonly EcoNexusDbContext _context;

    public CollectionVehicleRepository(EcoNexusDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<CollectionVehicle>> ListAsync(CancellationToken cancellationToken = default)
        => await _context.CollectionVehicles
            .AsNoTracking()
            .OrderBy(v => v.RegistrationNumber)
            .ToListAsync(cancellationToken);

    public Task<CollectionVehicle?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.CollectionVehicles.FirstOrDefaultAsync(v => v.Id == id, cancellationToken);

    public Task<bool> RegistrationNumberExistsAsync(string registrationNumber, CancellationToken cancellationToken = default)
    {
        // Registration numbers are stored uppercase. Use ToUpperInvariant to avoid
        // locale-dependent behavior (e.g., Turkish 'i' -> 'İ'), and StringComparison
        // for the comparison so the intent is explicit to both readers and analyzers.
        var normalized = registrationNumber.ToUpperInvariant();

        // Both sides are already uppercased, so an ordinal comparison is
        // correct. We deliberately use == (not string.Equals with
        // StringComparison) because EF Core cannot translate the latter
        // overload to SQL. The analyzer wants string.Equals here — that
        // suggestion does not apply to IQueryable expressions.
#pragma warning disable CA1862 // Use StringComparison for case-insensitive comparison
        return _context.CollectionVehicles
            .AnyAsync(v => v.RegistrationNumber == normalized, cancellationToken);
#pragma warning restore CA1862
    }

    public async Task AddAsync(CollectionVehicle vehicle, CancellationToken cancellationToken = default)
    {
        await _context.CollectionVehicles.AddAsync(vehicle, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
