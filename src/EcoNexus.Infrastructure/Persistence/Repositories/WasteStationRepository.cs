using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.Entities;
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
}

