namespace EcoNexus.Contracts.Stations;

/// <summary>Lightweight station view used by the list endpoint.</summary>
public sealed record StationListItemResponse(
    Guid Id,
    string Code,
    double Latitude,
    double Longitude,
    double CapacityKilograms,
    double CurrentFillPercent,
    string PrimaryCategory,
    string Status,
    bool IsCritical,
    DateTimeOffset LastUpdatedAt,
    DateTimeOffset? LastCollectedAt);
