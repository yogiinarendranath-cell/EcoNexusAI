using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

public sealed class CollectionJobTests
{
    private static CollectionJob CreateJobWithStops(int stopCount)
    {
        var job = CollectionJob.Schedule(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));
        for (var i = 0; i < stopCount; i++)
        {
            job.AddStop(Guid.NewGuid());
        }
        return job;
    }

    [Fact]
    public void Schedule_WithValidVehicle_ReturnsScheduledJob()
    {
        var job = CollectionJob.Schedule(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));

        Assert.Equal(JobStatus.Scheduled, job.Status);
        Assert.Null(job.StartedAt);
        Assert.Null(job.CompletedAt);
        Assert.Empty(job.Stops);
    }

    [Fact]
    public void Schedule_WithEmptyVehicleId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CollectionJob.Schedule(Guid.Empty, DateTimeOffset.UtcNow.AddHours(1)));
    }

    [Fact]
    public void AddStop_AssignsIncreasingSequence()
    {
        var job = CollectionJob.Schedule(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));

        var stop1 = job.AddStop(Guid.NewGuid());
        var stop2 = job.AddStop(Guid.NewGuid());
        var stop3 = job.AddStop(Guid.NewGuid());

        Assert.Equal(1, stop1.Sequence);
        Assert.Equal(2, stop2.Sequence);
        Assert.Equal(3, stop3.Sequence);
        Assert.Equal(3, job.Stops.Count);
    }

    [Fact]
    public void AddStop_AfterJobStarted_Throws()
    {
        var job = CreateJobWithStops(1);
        job.Start(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => job.AddStop(Guid.NewGuid()));
    }

    [Fact]
    public void Start_WithNoStops_Throws()
    {
        var job = CollectionJob.Schedule(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => job.Start(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Start_TransitionsToInProgressAndRaisesEvent()
    {
        var job = CreateJobWithStops(2);
        job.ClearDomainEvents();

        var startedAt = DateTimeOffset.UtcNow;
        job.Start(startedAt);

        Assert.Equal(JobStatus.InProgress, job.Status);
        Assert.Equal(startedAt, job.StartedAt);
        Assert.Single(job.DomainEvents);
        Assert.IsType<CollectionJobStartedEvent>(job.DomainEvents.First());
    }

    [Fact]
    public void Start_WhenAlreadyStarted_Throws()
    {
        var job = CreateJobWithStops(1);
        job.Start(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => job.Start(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CompleteStop_WhenNotInProgress_Throws()
    {
        var job = CreateJobWithStops(1);
        var stopId = job.Stops.First().Id;

        Assert.Throws<InvalidOperationException>(() =>
            job.CompleteStop(stopId, Weight.FromKilograms(5), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CompleteStop_WithUnknownStopId_Throws()
    {
        var job = CreateJobWithStops(1);
        job.Start(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            job.CompleteStop(Guid.NewGuid(), Weight.FromKilograms(5), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void CompleteStop_MarksStopCompletedAndRaisesEvent()
    {
        var job = CreateJobWithStops(2);
        job.Start(DateTimeOffset.UtcNow);
        job.ClearDomainEvents();

        var stopId = job.Stops.First().Id;
        var collectedAt = DateTimeOffset.UtcNow;
        job.CompleteStop(stopId, Weight.FromKilograms(12.5), collectedAt);

        var completedStop = job.Stops.First(s => s.Id == stopId);
        Assert.True(completedStop.IsCompleted);
        Assert.Equal(12.5, completedStop.CollectedWeight!.Kilograms);
        Assert.Equal(collectedAt, completedStop.CompletedAt);
        Assert.Single(job.DomainEvents);
        Assert.IsType<RouteStopCompletedEvent>(job.DomainEvents.First());
    }

    [Fact]
    public void CompleteStop_TwiceOnSameStop_Throws()
    {
        var job = CreateJobWithStops(1);
        job.Start(DateTimeOffset.UtcNow);
        var stopId = job.Stops.First().Id;
        job.CompleteStop(stopId, Weight.FromKilograms(5), DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() =>
            job.CompleteStop(stopId, Weight.FromKilograms(5), DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Complete_WhenNotInProgress_Throws()
    {
        var job = CreateJobWithStops(1);

        Assert.Throws<InvalidOperationException>(() => job.Complete(DateTimeOffset.UtcNow));
    }

    [Fact]
    public void Complete_TransitionsToCompletedAndRaisesEventWithTotals()
    {
        var job = CreateJobWithStops(2);
        job.Start(DateTimeOffset.UtcNow);

        var stops = job.Stops.ToList();
        job.CompleteStop(stops[0].Id, Weight.FromKilograms(10), DateTimeOffset.UtcNow);
        job.CompleteStop(stops[1].Id, Weight.FromKilograms(15), DateTimeOffset.UtcNow);
        job.ClearDomainEvents();

        var completedAt = DateTimeOffset.UtcNow;
        job.Complete(completedAt);

        Assert.Equal(JobStatus.Completed, job.Status);
        Assert.Equal(completedAt, job.CompletedAt);
        Assert.Single(job.DomainEvents);

        var evt = Assert.IsType<CollectionJobCompletedEvent>(job.DomainEvents.First());
        Assert.Equal(2, evt.StopsCompleted);
        Assert.Equal(25, evt.TotalCollected.Kilograms);
    }

    [Fact]
    public void Cancel_FromScheduled_Succeeds()
    {
        var job = CreateJobWithStops(1);

        job.Cancel();

        Assert.Equal(JobStatus.Cancelled, job.Status);
    }

    [Fact]
    public void Cancel_FromInProgress_Succeeds()
    {
        var job = CreateJobWithStops(1);
        job.Start(DateTimeOffset.UtcNow);

        job.Cancel();

        Assert.Equal(JobStatus.Cancelled, job.Status);
    }

    [Fact]
    public void Cancel_FromCompleted_Throws()
    {
        var job = CreateJobWithStops(1);
        job.Start(DateTimeOffset.UtcNow);
        job.Complete(DateTimeOffset.UtcNow);

        Assert.Throws<InvalidOperationException>(() => job.Cancel());
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_Throws()
    {
        var job = CreateJobWithStops(1);
        job.Cancel();

        Assert.Throws<InvalidOperationException>(() => job.Cancel());
    }

    [Fact]
    public void TotalCollected_SumsCompletedStopsOnly()
    {
        var job = CreateJobWithStops(3);
        job.Start(DateTimeOffset.UtcNow);

        var stops = job.Stops.ToList();
        job.CompleteStop(stops[0].Id, Weight.FromKilograms(10), DateTimeOffset.UtcNow);
        job.CompleteStop(stops[1].Id, Weight.FromKilograms(20), DateTimeOffset.UtcNow);
        // stop[2] intentionally not completed

        Assert.Equal(30, job.TotalCollected.Kilograms);
    }
}
