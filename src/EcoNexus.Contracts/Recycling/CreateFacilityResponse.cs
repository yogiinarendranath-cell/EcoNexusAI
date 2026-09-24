namespace EcoNexus.Contracts.Recycling;

/// <summary>Response returned after a recycling facility is created.</summary>
public sealed record CreateFacilityResponse(
    Guid Id,
    string Name);
