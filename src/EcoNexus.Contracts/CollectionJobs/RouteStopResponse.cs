namespace EcoNexus.Contracts.CollectionJobs;

/// <summary>
/// Read model for a single stop within a collection job.
/// </summary>
public sealed record RouteStopResponse(
    Guid Id,
    Guid StationId,
    int Sequence,
    double? CollectedWeightKilograms,
    DateTimeOffset? CompletedAt);
