namespace EcoNexus.Contracts.Recycling;

/// <summary>Request body for creating a new recycling facility.</summary>
public sealed record CreateFacilityRequest(
    string Name,
    double Latitude,
    double Longitude,
    double DailyCapacityKilograms);
