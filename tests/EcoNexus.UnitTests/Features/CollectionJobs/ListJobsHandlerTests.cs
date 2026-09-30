using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.CollectionJobs.ListJobs;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.CollectionJobs;

public sealed class ListJobsHandlerTests
{
    private readonly ICollectionJobRepository _repository =
        Substitute.For<ICollectionJobRepository>();

    private readonly ListJobsHandler _handler;

    public ListJobsHandlerTests()
    {
        _handler = new ListJobsHandler(_repository);
    }

    private static CollectionJob NewScheduledJob(int stopCount = 2)
    {
        var job = CollectionJob.Schedule(
            Guid.NewGuid(),
            scheduledFor: DateTimeOffset.UtcNow.AddHours(2));

        for (var i = 0; i < stopCount; i++)
        {
            job.AddStop(Guid.NewGuid());
        }

        return job;
    }

    [Fact]
    public async Task Handle_MultipleJobs_ReturnsAllMappedResponses()
    {
        var job1 = NewScheduledJob(stopCount: 2);
        var job2 = NewScheduledJob(stopCount: 1);

        _repository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { job1, job2 });

        var response = await _handler.Handle(
            new ListJobsQuery(),
            CancellationToken.None);

        Assert.Equal(2, response.Count);

        Assert.Equal(job1.Id,                response[0].Id);
        Assert.Equal(job1.VehicleId,         response[0].VehicleId);
        Assert.Equal("Scheduled",            response[0].Status);
        Assert.Equal(job1.ScheduledFor,      response[0].ScheduledFor);
        Assert.Null(response[0].StartedAt);
        Assert.Null(response[0].CompletedAt);
        Assert.Equal(2,                      response[0].Stops.Count);
        Assert.Equal(1,                      response[0].Stops[0].Sequence);
        Assert.Equal(2,                      response[0].Stops[1].Sequence);
        Assert.Equal(0,                      response[0].TotalCollectedKilograms);

        Assert.Equal(job2.Id,                response[1].Id);
        Assert.Single(response[1].Stops);
    }

    [Fact]
    public async Task Handle_JobWithNoStops_ReturnsEmptyStopsAndZeroTotal()
    {
        var job = NewScheduledJob(stopCount: 0);

        _repository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { job });

        var response = await _handler.Handle(
            new ListJobsQuery(),
            CancellationToken.None);

        Assert.Single(response);
        Assert.Empty(response[0].Stops);
        Assert.Equal(0, response[0].TotalCollectedKilograms);
    }

    [Fact]
    public async Task Handle_NoJobs_ReturnsEmptyList()
    {
        _repository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollectionJob>());

        var response = await _handler.Handle(
            new ListJobsQuery(),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce()
    {
        _repository
            .ListAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<CollectionJob>());

        await _handler.Handle(new ListJobsQuery(), CancellationToken.None);

        await _repository
            .Received(1)
            .ListAsync(Arg.Any<CancellationToken>());
    }
}