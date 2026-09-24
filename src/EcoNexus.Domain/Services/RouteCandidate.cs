using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Services;

/// <summary>
/// A station that is a candidate for inclusion in a collection route.
/// Carries everything the optimizer needs to rank and weigh it.
/// </summary>
public sealed record RouteCandidate(
    Guid StationId,
    Location Location,
    FillLevel FillLevel,
    Weight EstimatedWeight);
