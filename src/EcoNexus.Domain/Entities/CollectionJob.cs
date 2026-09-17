using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A scheduled collection run assigned to a vehicle. Aggregate root: owns
/// its RouteStops, enforces the job state machine, and raises domain events.
/// </summary>
public sealed class CollectionJob : AggregateRoot
{
    private readonly List<RouteStop> _stops = new();

    public Guid VehicleId { get; private set; }
    public JobStatus Status { get; private set; }
    public DateTimeOffset ScheduledFor { get; private set; }
    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? CompletedAt { get; private set; }

    public IReadOnlyCollection<RouteStop> Stops => _stops.AsReadOnly();

    public Weight TotalCollected =>
        _stops
            .Where(s => s.CollectedWeight is not null)
            .Aggregate(Weight.Zero(), (acc, s) => acc.Add(s.CollectedWeight!));

    // Required by EF Core
    private CollectionJob()
    {
    }

    private CollectionJob(Guid vehicleId, DateTimeOffset scheduledFor)
    {
        VehicleId = vehicleId;
        ScheduledFor = scheduledFor;
        Status = JobStatus.Scheduled;
    }

    public static CollectionJob Schedule(Guid vehicleId, DateTimeOffset scheduledFor)
    {
        if (vehicleId == Guid.Empty)
        {
            throw new ArgumentException("VehicleId must not be empty.", nameof(vehicleId));
        }

        return new CollectionJob(vehicleId, scheduledFor);
    }

    /// <summary>
    /// Adds a route stop to the job. Only allowed while the job is Scheduled.
    /// </summary>
    public RouteStop AddStop(Guid stationId)
    {
        if (Status != JobStatus.Scheduled)
        {
            throw new InvalidOperationException("Cannot add stops to a job that has started or completed.");
        }

        var nextSequence = _stops.Count + 1;
        var stop = new RouteStop(Id, stationId, nextSequence);
        _stops.Add(stop);
        return stop;
    }

    /// <summary>
    /// Transitions the job from Scheduled to InProgress.
    /// </summary>
    public void Start(DateTimeOffset startedAt)
    {
        if (Status != JobStatus.Scheduled)
        {
            throw new InvalidOperationException($"Cannot start a job in status {Status}.");
        }

        if (_stops.Count == 0)
        {
            throw new InvalidOperationException("Cannot start a job with no route stops.");
        }

        Status = JobStatus.InProgress;
        StartedAt = startedAt;

        RaiseDomainEvent(new CollectionJobStartedEvent(Id, VehicleId, startedAt));
    }

    /// <summary>
    /// Completes a route stop. Only allowed while the job is InProgress.
    /// </summary>
    public void CompleteStop(Guid stopId, Weight collectedWeight, DateTimeOffset completedAt)
    {
        if (Status != JobStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete stops on a job in status {Status}.");
        }

        var stop = _stops.FirstOrDefault(s => s.Id == stopId)
            ?? throw new InvalidOperationException($"Route stop {stopId} does not belong to this job.");

        stop.Complete(collectedWeight, completedAt);

        RaiseDomainEvent(new RouteStopCompletedEvent(Id, stop.StationId, collectedWeight, completedAt));
    }

    /// <summary>
    /// Transitions the job from InProgress to Completed.
    /// </summary>
    public void Complete(DateTimeOffset completedAt)
    {
        if (Status != JobStatus.InProgress)
        {
            throw new InvalidOperationException($"Cannot complete a job in status {Status}.");
        }

        Status = JobStatus.Completed;
        CompletedAt = completedAt;

        var completedStops = _stops.Count(s => s.IsCompleted);

        RaiseDomainEvent(new CollectionJobCompletedEvent(
            Id,
            VehicleId,
            completedStops,
            TotalCollected,
            completedAt));
    }

    /// <summary>
    /// Cancels the job. Allowed from Scheduled or InProgress.
    /// </summary>
    public void Cancel()
    {
        if (Status == JobStatus.Completed)
        {
            throw new InvalidOperationException("Cannot cancel a completed job.");
        }

        if (Status == JobStatus.Cancelled)
        {
            throw new InvalidOperationException("Job is already cancelled.");
        }

        Status = JobStatus.Cancelled;
    }
}
