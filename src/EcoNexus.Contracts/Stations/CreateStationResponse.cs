namespace EcoNexus.Contracts.Stations;

/// <summary>Response returned after a station is created.</summary>
public sealed record CreateStationResponse(
    Guid Id,
    string Code);
