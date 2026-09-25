using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.Entities;
using EcoNexus.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace EcoNexus.UnitTests.Infrastructure.Caching;

/// <summary>
/// Contract tests for the caching decorator around
/// <see cref="IRecyclingFacilityRepository"/>. Proves:
///   1. Reads within the TTL window do NOT hit the inner repository.
///   2. Writes evict the cache so the next read is fresh.
///   3. Non-cached reads always pass through.
///
/// Uses a hand-written fake instead of NSubstitute: the concrete
/// repository and DbContext are both sealed, so Castle DynamicProxy
/// cannot create substitutes for them.
/// </summary>
public sealed class CachedRecyclingFacilityRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_SecondCallWithinTtl_DoesNotHitInnerRepository()
    {
        var inner = new FakeRecyclingFacilityRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);

        var first = await sut.GetAllAsync(CancellationToken.None);
        var second = await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(1, inner.GetAllCallCount);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task AddAsync_EvictsCache_SoNextReadHitsInnerRepository()
    {
        var inner = new FakeRecyclingFacilityRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);

        await sut.GetAllAsync(CancellationToken.None);
        Assert.Equal(1, inner.GetAllCallCount);

        await sut.AddAsync(FakeRecyclingFacilityRepository.Facility(), CancellationToken.None);

        await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, inner.GetAllCallCount);
    }

    [Fact]
    public async Task SaveChangesAsync_EvictsCache()
    {
        var inner = new FakeRecyclingFacilityRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);

        await sut.GetAllAsync(CancellationToken.None);
        await sut.SaveChangesAsync(CancellationToken.None);
        await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, inner.GetAllCallCount);
    }

    [Fact]
    public async Task Remove_EvictsCache()
    {
        var inner = new FakeRecyclingFacilityRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);

        await sut.GetAllAsync(CancellationToken.None);
        sut.Remove(FakeRecyclingFacilityRepository.Facility());
        await sut.GetAllAsync(CancellationToken.None);

        Assert.Equal(2, inner.GetAllCallCount);
    }

    [Fact]
    public async Task GetByIdAsync_AlwaysPassesThroughToInner()
    {
        var inner = new FakeRecyclingFacilityRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);
        var id = Guid.NewGuid();

        await sut.GetByIdAsync(id, CancellationToken.None);
        await sut.GetByIdAsync(id, CancellationToken.None);

        Assert.Equal(2, inner.GetByIdCallCount);
    }

    [Fact]
    public async Task NameExistsAsync_AlwaysPassesThroughToInner()
    {
        var inner = new FakeRecyclingFacilityRepository();
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);

        await sut.NameExistsAsync("Test Facility", CancellationToken.None);
        await sut.NameExistsAsync("Test Facility", CancellationToken.None);

        Assert.Equal(2, inner.NameExistsCallCount);
    }

    // ============================================================
    // Fake — implements the interface directly, counts calls.
    // The concrete repository is sealed, so we do not use it here.
    // ============================================================

    private sealed class FakeRecyclingFacilityRepository : IRecyclingFacilityRepository
    {
        private readonly List<RecyclingFacility> _facilities = new();

        public int GetAllCallCount { get; private set; }
        public int GetByIdCallCount { get; private set; }
        public int NameExistsCallCount { get; private set; }
        public int AddCallCount { get; private set; }
        public int SaveChangesCallCount { get; private set; }
        public int RemoveCallCount { get; private set; }

        public Task AddAsync(RecyclingFacility facility, CancellationToken cancellationToken = default)
        {
            AddCallCount++;
            _facilities.Add(facility);
            return Task.CompletedTask;
        }

        public Task<RecyclingFacility?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            GetByIdCallCount++;
            return Task.FromResult<RecyclingFacility?>(null);
        }

        public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
        {
            NameExistsCallCount++;
            return Task.FromResult(false);
        }

        public Task<IReadOnlyList<RecyclingFacility>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            GetAllCallCount++;
            return Task.FromResult<IReadOnlyList<RecyclingFacility>>(_facilities.ToList());
        }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCallCount++;
            return Task.FromResult(1);
        }

        public void Remove(RecyclingFacility facility)
        {
            RemoveCallCount++;
            _facilities.Remove(facility);
        }

        /// <summary>Returns a real RecyclingFacility for use in write-path tests.</summary>
        public static RecyclingFacility Facility()
            => RecyclingFacility.Create(
                "Stub Facility",
                EcoNexus.Domain.ValueObjects.Location.Create(12.9716, 77.5946),
                EcoNexus.Domain.ValueObjects.Weight.FromKilograms(1000));
    }
}
