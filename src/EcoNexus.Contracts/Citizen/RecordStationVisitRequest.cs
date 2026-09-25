namespace EcoNexus.Contracts.Citizen;

/// <summary>Request body for logging a station visit (earns green points).</summary>
public sealed record RecordStationVisitRequest(
    Guid StationId,
    DateOnly VisitDate);
