using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Services;

/// <summary>
/// Pure domain service that orders a set of candidate stations into a
/// collection route using a greedy nearest-neighbour heuristic.
///
/// Design intent:
/// - Deterministic: same inputs always produce the same output.
/// - Explainable: every decision can be justified in a code review.
/// - Pure: no I/O, no EF, no logging, no configuration.
///
/// This is intentionally NOT an optimal solver. Optimal route planning is
/// NP-hard. The chosen heuristic is fast, deterministic, and can be
/// swapped for a proper solver later without touching callers.
/// </summary>
public static class RouteOptimizer
{
    /// <summary>Candidates below this fill percent are ignored.</summary>
    public const double MinimumFillPercent = 60.0;

    /// <summary>
    /// A full bin is treated as if it were up to this many kilometres closer
    /// than its physical location. Balances urgency against distance.
    /// </summary>
    public const double MaxUrgencyBonusKm = 5.0;

    /// <summary>
    /// Orders the candidates into a route. The route begins at
    /// <paramref name="startLocation"/> (typically the vehicle depot or its
    /// last known position) and proceeds to the highest-value candidates first.
    /// </summary>
    public static IReadOnlyList<Guid> Optimize(
        Location startLocation,
        Weight vehicleCapacity,
        IReadOnlyList<RouteCandidate> candidates)
    {
        ArgumentNullException.ThrowIfNull(startLocation);
        ArgumentNullException.ThrowIfNull(vehicleCapacity);
        ArgumentNullException.ThrowIfNull(candidates);

        // 1. Filter: only visit stations at or above the minimum fill threshold.
        var eligible = candidates
            .Where(c => c.FillLevel.Percent >= MinimumFillPercent)
            .ToList();

        if (eligible.Count == 0)
        {
            return Array.Empty<Guid>();
        }

        var remaining = new List<RouteCandidate>(eligible);
        var route = new List<Guid>(remaining.Count);
        var current = startLocation;
        var accumulatedKg = 0.0;

        // 2. Greedy selection: pick the best candidate at each step.
        while (remaining.Count > 0)
        {
            var best = FindBestNext(current, remaining);

            // 3. Capacity guard: if adding this stop would exceed capacity,
            //    stop the route. A fuller route would be wasted motion.
            var projectedKg = accumulatedKg + best.EstimatedWeight.Kilograms;
            if (projectedKg > vehicleCapacity.Kilograms)
            {
                break;
            }

            route.Add(best.StationId);
            accumulatedKg = projectedKg;
            current = best.Location;
            remaining.Remove(best);
        }

        return route;
    }

    /// <summary>
    /// Picks the next stop by the lowest composite score.
    ///
    /// score = distanceKm - urgencyBonusKm
    ///
    /// A full bin gets a larger urgency bonus, so it can beat a physically
    /// closer but half-empty station. Ties are broken by StationId so the
    /// output is deterministic.
    /// </summary>
    private static RouteCandidate FindBestNext(
        Location from,
        IReadOnlyList<RouteCandidate> remaining)
    {
        RouteCandidate? best = null;
        var bestScore = double.PositiveInfinity;

        foreach (var candidate in remaining)
        {
            var distanceKm = HaversineKm(from, candidate.Location);
            var urgencyBonusKm = UrgencyBonusKm(candidate.FillLevel);
            var score = distanceKm - urgencyBonusKm;

            if (score < bestScore ||
                (score == bestScore && (best is null ||
                    candidate.StationId.CompareTo(best.StationId) < 0)))
            {
                best = candidate;
                bestScore = score;
            }
        }

        return best!;
    }

    /// <summary>
    /// Converts a fill level into an equivalent "distance saved".
    /// At the minimum threshold → 0 km. At full → MaxUrgencyBonusKm.
    /// Linear in between.
    /// </summary>
    private static double UrgencyBonusKm(FillLevel fillLevel)
    {
        var span = 100.0 - MinimumFillPercent;
        var normalized = (fillLevel.Percent - MinimumFillPercent) / span;
        if (normalized < 0) normalized = 0;
        if (normalized > 1) normalized = 1;
        return normalized * MaxUrgencyBonusKm;
    }

    /// <summary>
    /// Great-circle distance between two coordinates, in kilometres.
    /// Uses the haversine formula with an Earth radius of 6371 km.
    /// </summary>
    private static double HaversineKm(Location a, Location b)
    {
        const double earthRadiusKm = 6371.0;

        var lat1 = ToRadians(a.Latitude);
        var lat2 = ToRadians(b.Latitude);
        var dLat = lat2 - lat1;
        var dLon = ToRadians(b.Longitude - a.Longitude);

        var sinLat = Math.Sin(dLat / 2.0);
        var sinLon = Math.Sin(dLon / 2.0);

        var h = sinLat * sinLat + Math.Cos(lat1) * Math.Cos(lat2) * sinLon * sinLon;
        var c = 2.0 * Math.Asin(Math.Min(1.0, Math.Sqrt(h)));

        return earthRadiusKm * c;
    }

    private static double ToRadians(double degrees) => degrees * Math.PI / 180.0;
}
