namespace EcoNexus.Contracts.Stations;

/// <summary>
/// Payload for updating the mutable metadata of a waste station.
/// Station Code, CurrentFill, and Status are NOT updatable through this endpoint.
/// </summary>
public sealed record UpdateStationRequest(
    double Latitude,
    double Longitude,
    double CapacityKilograms,
    string PrimaryCategory);
