using EcoNexus.Domain.Services;
using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Services;

public sealed class RouteOptimizerTests
{
    // A neutral starting point. Coordinates chosen so that 1 degree of
    // latitude is ~111 km — easy math for distance-based assertions.
    private static readonly Location Origin = Location.Create(18.5204, 73.8567);

    // A 1-degree-north shift ≈ 111 km. We use degrees rather than km
    // directly so the intent stays readable.
    private static Location North(double degrees) =>
        Location.Create(Origin.Latitude + degrees, Origin.Longitude);

    private static Location East(double degrees) =>
        Location.Create(Origin.Latitude, Origin.Longitude + degrees);

    private static RouteCandidate Candidate(
        Guid id, Location location, double fillPercent, double estimatedKg)
        => new(id, location, FillLevel.FromPercent(fillPercent), Weight.FromKilograms(estimatedKg));

    // -------------------------------------------------------------------
    // 1. Empty candidates → empty route.
    // -------------------------------------------------------------------
    [Fact]
    public void Optimize_NoCandidates_ReturnsEmpty()
    {
        var route = RouteOptimizer.Optimize(
            Origin, Weight.FromKilograms(1000), Array.Empty<RouteCandidate>());

        Assert.Empty(route);
    }

    // -------------------------------------------------------------------
    // 2. All candidates below the minimum fill threshold → empty route.
    // -------------------------------------------------------------------
    [Fact]
    public void Optimize_AllBelowThreshold_ReturnsEmpty()
    {
        var candidates = new[]
        {
            Candidate(Guid.NewGuid(), North(0.001), 30, 100),
            Candidate(Guid.NewGuid(), North(0.002), 59, 100),
            Candidate(Guid.NewGuid(), North(0.003), 45, 100),
        };

        var route = RouteOptimizer.Optimize(Origin, Weight.FromKilograms(1000), candidates);

        Assert.Empty(route);
    }

    // -------------------------------------------------------------------
    // 3. Single eligible candidate → returned in route.
    // -------------------------------------------------------------------
    [Fact]
    public void Optimize_SingleEligibleCandidate_ReturnsIt()
    {
        var id = Guid.NewGuid();
        var candidates = new[] { Candidate(id, North(0.001), 80, 100) };

        var route = RouteOptimizer.Optimize(Origin, Weight.FromKilograms(1000), candidates);

        Assert.Single(route);
        Assert.Equal(id, route[0]);
    }

    // -------------------------------------------------------------------
    // 4. Urgency can beat proximity.
    //    A full bin 4 km away is preferred over a half-full bin 1 km away.
    // -------------------------------------------------------------------
    [Fact]
    public void Optimize_UrgencyCanBeatProximity()
    {
        var closeHalfFull = Guid.NewGuid();
        var farFull = Guid.NewGuid();

        // 1 degree latitude ≈ 111 km. So 0.009 deg ≈ 1 km.
        var candidates = new[]
        {
            Candidate(closeHalfFull, North(0.009),  65, 100),  // 1 km, moderate fill
            Candidate(farFull,       North(0.036), 100, 100),  // 4 km, full
        };

        var route = RouteOptimizer.Optimize(Origin, Weight.FromKilograms(1000), candidates);

        Assert.Equal(2, route.Count);
        Assert.Equal(farFull, route[0]);          // urgency wins — full bin first
        Assert.Equal(closeHalfFull, route[1]);
    }

    // -------------------------------------------------------------------
    // 5. When urgency is exhausted (both full), distance decides.
    // -------------------------------------------------------------------
    [Fact]
    public void Optimize_EqualUrgency_DistanceDecides()
    {
        var near = Guid.NewGuid();
        var far = Guid.NewGuid();

        var candidates = new[]
        {
            Candidate(far,  North(0.027), 100, 100),  // ~3 km
            Candidate(near, North(0.009), 100, 100),  // ~1 km
        };

        var route = RouteOptimizer.Optimize(Origin, Weight.FromKilograms(1000), candidates);

        Assert.Equal(2, route.Count);
        Assert.Equal(near, route[0]);
        Assert.Equal(far, route[1]);
    }

    // -------------------------------------------------------------------
    // 6. Capacity guard: route stops when adding the next stop would exceed
    //    vehicle capacity.
    // -------------------------------------------------------------------
    [Fact]
    public void Optimize_CapacityGuard_StopsAddingStops()
    {
        // Two candidates, both full, both eligible. Vehicle capacity = 100 kg.
        // Each candidate would contribute 60 kg. Two would be 120 kg → exceed.
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        var candidates = new[]
        {
            Candidate(first,  North(0.009), 100, 60),  // ~1 km
            Candidate(second, North(0.036), 100, 60),  // ~4 km
        };

        var route = RouteOptimizer.Optimize(Origin, Weight.FromKilograms(100), candidates);

        // Only the first stop fits within capacity.
        Assert.Single(route);
        Assert.Equal(first, route[0]);
    }

    // -------------------------------------------------------------------
    // 7. Determinism: same inputs → same output, across repeated calls.
    // -------------------------------------------------------------------
    [Fact]
    public void Optimize_IsDeterministic()
    {
        var a = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var b = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var c = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");

        // All three are equally eligible AND equally distant, so the only
        // tiebreaker is StationId. This makes the expected order explicit.
        var candidates = new[]
        {
            Candidate(c, North(0.009), 100, 100),
            Candidate(a, North(0.009), 100, 100),
            Candidate(b, North(0.009), 100, 100),
        };

        var route1 = RouteOptimizer.Optimize(Origin, Weight.FromKilograms(1000), candidates);
        var route2 = RouteOptimizer.Optimize(Origin, Weight.FromKilograms(1000), candidates);

        Assert.Equal(route1, route2);

        // And expected: a, b, c — ascending by Guid.
        Assert.Equal(new[] { a, b, c }, route1);
    }
}
