using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A single stop within a collection job: a visit to a specific waste station.
/// Child entity of the CollectionJob aggregate.
/// </summary>
public sealed class RouteStop : Entity
{
    public Guid JobId { get; private set; }
    public Guid StationId { get; private set; }
    public int Sequence { get; private set; }
    public Weight? CollectedWeight { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public bool IsCompleted => CompletedAt.HasValue;

    // Required by EF Core
    private RouteStop()
    {
    }

    internal RouteStop(Guid jobId, Guid stationId, int sequence)
    {
        if (jobId == Guid.Empty)
        {
            throw new ArgumentException("JobId must not be empty.", nameof(jobId));
        }

        if (stationId == Guid.Empty)
        {
            throw new ArgumentException("StationId must not be empty.", nameof(stationId));
        }

        if (sequence < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(sequence), "Sequence must be 1 or greater.");
        }

        JobId = jobId;
        StationId = stationId;
        Sequence = sequence;
    }

    internal void Complete(Weight collectedWeight, DateTimeOffset completedAt)
    {
        if (IsCompleted)
        {
            throw new InvalidOperationException("Route stop is already completed.");
        }

        CollectedWeight = collectedWeight ?? throw new ArgumentNullException(nameof(collectedWeight));
        CompletedAt = completedAt;
    }
}
