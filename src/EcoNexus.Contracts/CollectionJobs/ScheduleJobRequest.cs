namespace EcoNexus.Contracts.CollectionJobs;

/// <summary>
/// Request to schedule a new collection job. Stops are ordered by array position;
/// the domain assigns sequence numbers implicitly via CollectionJob.AddStop.
/// </summary>
public sealed record ScheduleJobRequest(
    Guid VehicleId,
    DateTimeOffset ScheduledFor,
    IReadOnlyList<ScheduleJobStop> Stops);

public sealed record ScheduleJobStop(Guid StationId);
