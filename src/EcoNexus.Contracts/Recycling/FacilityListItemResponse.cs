namespace EcoNexus.Contracts.Recycling;

/// <summary>Lightweight facility view used by the list endpoint.</summary>
public sealed record FacilityListItemResponse(
    Guid Id,
    string Name,
    double Latitude,
    double Longitude,
    double DailyCapacityKilograms,
    string Status,
    int IntakeCount,
    RecyclingMetricsResponse Metrics,
    DateTimeOffset LastUpdatedAt);
