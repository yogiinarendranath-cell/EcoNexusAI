using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.CollectionJobs.GetJobById;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.CollectionJobs;

public sealed class GetJobByIdHandlerTests
{
    private readonly ICollectionJobRepository _repository =
        Substitute.For<ICollectionJobRepository>();

    private readonly GetJobByIdHandler _handler;

    public GetJobByIdHandlerTests()
    {
        _handler = new GetJobByIdHandler(_repository);
    }

    private static CollectionJob NewJob()
    {
        var job = CollectionJob.Schedule(
            Guid.NewGuid(),
            scheduledFor: DateTimeOffset.UtcNow.AddHours(2));

        job.AddStop(Guid.NewGuid());
        job.AddStop(Guid.NewGuid());

        return job;
    }

    [Fact]
    public async Task Handle_JobExists_ReturnsMappedResponse()
    {
        var job = NewJob();

        _repository
            .GetByIdWithStopsAsync(job.Id, Arg.Any<CancellationToken>())
            .Returns(job);

        var response = await _handler.Handle(
            new GetJobByIdQuery(job.Id),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(job.Id,               response.Id);
        Assert.Equal(job.VehicleId,        response.VehicleId);
        Assert.Equal("Scheduled",          response.Status);
        Assert.Equal(job.ScheduledFor,     response.ScheduledFor);
        Assert.Null(response.StartedAt);
        Assert.Null(response.CompletedAt);
        Assert.Equal(2,                    response.Stops.Count);
        Assert.Equal(0,                    response.TotalCollectedKilograms);
    }

    [Fact]
    public async Task Handle_JobDoesNotExist_ThrowsNotFoundException()
    {
        var jobId = Guid.NewGuid();

        _repository
            .GetByIdWithStopsAsync(jobId, Arg.Any<CancellationToken>())
            .Returns((CollectionJob?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new GetJobByIdQuery(jobId), CancellationToken.None));

        Assert.Contains(jobId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_QueriesRepositoryWithExactRequestedId()
    {
        var job = NewJob();

        _repository
            .GetByIdWithStopsAsync(job.Id, Arg.Any<CancellationToken>())
            .Returns(job);

        await _handler.Handle(new GetJobByIdQuery(job.Id), CancellationToken.None);

        await _repository
            .Received(1)
            .GetByIdWithStopsAsync(job.Id, Arg.Any<CancellationToken>());
    }
}