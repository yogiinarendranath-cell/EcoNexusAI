using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using EcoNexus.Infrastructure.Persistence;
using EcoNexus.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Caching.Memory;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Infrastructure.Caching;

/// <summary>
/// Contract tests for the caching decorator around
/// <see cref="IRecyclingFacilityRepository"/>. Proves:
///   1. Reads within the TTL window do NOT hit the inner repository.
///   2. Writes evict the cache so the next read is fresh.
///   3. Non-cached reads always pass through.
/// </summary>
public sealed class CachedRecyclingFacilityRepositoryTests
{
    [Fact]
    public async Task GetAllAsync_SecondCallWithinTtl_DoesNotHitInnerRepository()
    {
        // Arrange
        var inner = Substitute.For<RecyclingFacilityRepository>(Substitute.For<EcoNexusDbContext>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);
        var facilities = new List<RecyclingFacility>();

        inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns(facilities);

        // Act
        var first = await sut.GetAllAsync(CancellationToken.None);
        var second = await sut.GetAllAsync(CancellationToken.None);

        // Assert: second call was served from cache — inner called only once.
        await inner.Received(1).GetAllAsync(Arg.Any<CancellationToken>());
        Assert.Same(first, second);
    }

    [Fact]
    public async Task AddAsync_EvictsCache_SoNextReadHitsInnerRepository()
    {
        // Arrange
        var inner = Substitute.For<RecyclingFacilityRepository>(Substitute.For<EcoNexusDbContext>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);
        var facilities = new List<RecyclingFacility>();

        inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns(facilities);
        inner.AddAsync(Arg.Any<RecyclingFacility>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        // Prime the cache
        await sut.GetAllAsync(CancellationToken.None);
        await inner.Received(1).GetAllAsync(Arg.Any<CancellationToken>());

        // Act: write evicts
        var anyFacility = CreateStubFacility();
        await sut.AddAsync(anyFacility, CancellationToken.None);

        // Second read must hit inner again because cache was evicted.
        await sut.GetAllAsync(CancellationToken.None);

        // Assert
        await inner.Received(2).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SaveChangesAsync_EvictsCache()
    {
        var inner = Substitute.For<RecyclingFacilityRepository>(Substitute.For<EcoNexusDbContext>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);
        var facilities = new List<RecyclingFacility>();

        inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns(facilities);
        inner.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);

        await sut.GetAllAsync(CancellationToken.None);
        await sut.SaveChangesAsync(CancellationToken.None);
        await sut.GetAllAsync(CancellationToken.None);

        await inner.Received(2).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Remove_EvictsCache()
    {
        var inner = Substitute.For<RecyclingFacilityRepository>(Substitute.For<EcoNexusDbContext>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);
        var facilities = new List<RecyclingFacility>();

        inner.GetAllAsync(Arg.Any<CancellationToken>()).Returns(facilities);

        await sut.GetAllAsync(CancellationToken.None);
        sut.Remove(CreateStubFacility());
        await sut.GetAllAsync(CancellationToken.None);

        await inner.Received(2).GetAllAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetByIdAsync_AlwaysPassesThroughToInner()
    {
        var inner = Substitute.For<RecyclingFacilityRepository>(Substitute.For<EcoNexusDbContext>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);
        var id = Guid.NewGuid();

        inner.GetByIdAsync(id, Arg.Any<CancellationToken>()).Returns((RecyclingFacility?)null);

        await sut.GetByIdAsync(id, CancellationToken.None);
        await sut.GetByIdAsync(id, CancellationToken.None);

        // Not cached — must hit inner both times.
        await inner.Received(2).GetByIdAsync(id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task NameExistsAsync_AlwaysPassesThroughToInner()
    {
        var inner = Substitute.For<RecyclingFacilityRepository>(Substitute.For<EcoNexusDbContext>());
        var cache = new MemoryCache(new MemoryCacheOptions());
        var sut = new CachedRecyclingFacilityRepository(inner, cache);

        inner.NameExistsAsync("Test Facility", Arg.Any<CancellationToken>()).Returns(true);

        await sut.NameExistsAsync("Test Facility", CancellationToken.None);
        await sut.NameExistsAsync("Test Facility", CancellationToken.None);

        await inner.Received(2).NameExistsAsync("Test Facility", Arg.Any<CancellationToken>());
    }

    private static RecyclingFacility CreateStubFacility()
    {
        // We do not need a fully-populated facility for these tests —
        // the mock never inspects the argument. But RecyclingFacility
        // has a private constructor, so we go through its factory.
        // If the factory requires parameters we don't have, we can
        // swap this for whatever the factory needs.
        return RecyclingFacility.Create(
            name: "Stub Facility",
            location: Location.Create(latitude: 12.9716, longitude: 77.5946),
            dailyCapacity: Weight.FromKilograms(1000));
    }
}
