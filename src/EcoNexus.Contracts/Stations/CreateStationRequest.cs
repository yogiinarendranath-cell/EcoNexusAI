namespace EcoNexus.Contracts.Stations;

/// <summary>Request body for creating a new waste station.</summary>
public sealed record CreateStationRequest(
    string Code,
    double Latitude,
    double Longitude,
    double CapacityKilograms,
    string PrimaryCategory);
