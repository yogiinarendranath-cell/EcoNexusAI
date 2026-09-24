namespace EcoNexus.Contracts.CollectionJobs;

/// <summary>
/// Requests a preview of the collection route that would visit the given
/// stations in the optimal order for the given vehicle.
///
/// This is a read-only operation: no CollectionJob is created.
/// </summary>
public sealed record PreviewRouteRequest(
    Guid VehicleId,
    IReadOnlyList<Guid> CandidateStationIds);
