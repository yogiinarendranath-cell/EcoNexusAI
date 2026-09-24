namespace EcoNexus.Contracts.CollectionVehicles;

/// <summary>
/// Payload for creating a new collection vehicle.
/// </summary>
public sealed record CreateCollectionVehicleRequest(
    string RegistrationNumber,
    double CapacityKilograms);
