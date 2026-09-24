namespace EcoNexus.Contracts.Recycling;

/// <summary>Full facility detail including its intake history and metrics.</summary>
public sealed record FacilityDetailResponse(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double DailyCapacityKilograms,
    string Status,
    RecyclingMetricsResponse Metrics,
    IReadOnlyList<IntakeResponse> Intakes,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt);
