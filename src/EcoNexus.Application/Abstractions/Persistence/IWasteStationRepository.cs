using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Abstractions.Persistence;

/// <summary>
/// Persistence abstraction for the WasteStation aggregate. Implemented in the
/// infrastructure layer using EF Core. Handlers depend on this interface,
/// not on DbContext.
/// </summary>
public interface IWasteStationRepository
{
    /// <summary>Adds a new station to the unit of work.</summary>
    Task AddAsync(WasteStation station, CancellationToken cancellationToken = default);

    /// <summary>Loads a station by id, or null if not found.</summary>
    Task<WasteStation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Returns true if a station with the given code already exists.</summary>
    Task<bool> CodeExistsAsync(StationCode code, CancellationToken cancellationToken = default);
}
