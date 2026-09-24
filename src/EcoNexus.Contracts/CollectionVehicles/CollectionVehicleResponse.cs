namespace EcoNexus.Contracts.CollectionVehicles;

/// <summary>
/// Read model for a collection vehicle.
/// </summary>
public sealed record CollectionVehicleResponse(
    Guid Id,
    string RegistrationNumber,
    double CapacityKilograms,
    bool IsActive);
