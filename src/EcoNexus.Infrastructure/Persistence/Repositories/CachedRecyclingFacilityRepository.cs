using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.Entities;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;

namespace EcoNexus.Infrastructure.Persistence.Repositories;

/// <summary>
/// Caching decorator for <see cref="IRecyclingFacilityRepository"/>.
/// Only <see cref="GetAllAsync"/> is cached — it is called by the list
/// endpoint and by three assistant tools, on a dataset that changes
/// rarely. All write operations evict the cache so the next read is fresh.
///
/// Depends on the interface, not the concrete repository, so it can be
/// tested with any implementation (including hand-written fakes).
/// The inner implementation is resolved via a keyed service to avoid a
/// circular dependency on <see cref="IRecyclingFacilityRepository"/>.
/// </summary>
internal sealed class CachedRecyclingFacilityRepository : IRecyclingFacilityRepository
{
    /// <summary>Key used by the DI registration for the inner (uncached) repository.</summary>
    internal const string InnerServiceKey = "recycling-facility:inner";

    /// <summary>
    /// Single cache key. The interface has no parameters on GetAllAsync,
    /// so one entry per process is sufficient.
    /// </summary>
    internal const string AllFacilitiesCacheKey = "recycling:facilities:all";

    /// <summary>
    /// Absolute TTL. Facility metadata changes a few times a day, so
    /// 60 seconds is a conservative upper bound on staleness.
    /// </summary>
    internal static readonly TimeSpan CacheTtl = TimeSpan.FromSeconds(60);

    private readonly IRecyclingFacilityRepository _inner;
    private readonly IMemoryCache _cache;

    public CachedRecyclingFacilityRepository(
        [FromKeyedServices(InnerServiceKey)] IRecyclingFacilityRepository inner,
        IMemoryCache cache)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task AddAsync(
        RecyclingFacility facility,
        CancellationToken cancellationToken = default)
    {
        await _inner.AddAsync(facility, cancellationToken);
        Evict();
    }

    public Task<RecyclingFacility?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
        => _inner.GetByIdAsync(id, cancellationToken);

    public Task<bool> NameExistsAsync(
        string name,
        CancellationToken cancellationToken = default)
        => _inner.NameExistsAsync(name, cancellationToken);

    public async Task<IReadOnlyList<RecyclingFacility>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        if (_cache.TryGetValue(AllFacilitiesCacheKey, out IReadOnlyList<RecyclingFacility>? cached)
            && cached is not null)
        {
            return cached;
        }

        var fresh = await _inner.GetAllAsync(cancellationToken);

        _cache.Set(
            AllFacilitiesCacheKey,
            fresh,
            new MemoryCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = CacheTtl,
            });

        return fresh;
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await _inner.SaveChangesAsync(cancellationToken);
        Evict();
        return result;
    }

    public void Remove(RecyclingFacility facility)
    {
        _inner.Remove(facility);
        Evict();
    }

    private void Evict() => _cache.Remove(AllFacilitiesCacheKey);
}
