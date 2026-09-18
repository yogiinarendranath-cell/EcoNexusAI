namespace EcoNexus.Contracts.Stations;

/// <summary>Detailed view of a waste station.</summary>
public sealed record StationDetailResponse(
    Guid Id,
    string Code,
    double Latitude,
    double Longitude,
    double CapacityKilograms,
    double CurrentFillPercent,
    string PrimaryCategory,
    string Status,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? LastCollectedAt);
