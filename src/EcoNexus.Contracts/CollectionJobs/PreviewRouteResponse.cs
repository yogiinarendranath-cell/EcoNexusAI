namespace EcoNexus.Contracts.CollectionJobs;

/// <summary>
/// Ordered preview of a collection route with per-stop reasoning.
/// </summary>
public sealed record PreviewRouteResponse(
    Guid VehicleId,
    double VehicleCapacityKilograms,
    IReadOnlyList<PreviewRouteStop> Stops,
    double TotalEstimatedWeightKilograms,
    int SkippedCount);

/// <summary>
/// A single stop in the previewed route with the reason it was selected.
/// </summary>
public sealed record PreviewRouteStop(
    Guid StationId,
    string StationCode,
    int Sequence,
    double FillLevelPercent,
    double EstimatedWeightKilograms);
