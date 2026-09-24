namespace EcoNexus.Contracts.CollectionJobs;

/// <summary>
/// Read model for a collection job with its ordered route stops.
/// </summary>
public sealed record CollectionJobResponse(
    Guid Id,
    Guid VehicleId,
    string Status,
    DateTimeOffset ScheduledFor,
    DateTimeOffset? StartedAt,
    DateTimeOffset? CompletedAt,
    IReadOnlyList<RouteStopResponse> Stops,
    double TotalCollectedKilograms);
