using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.CollectionJobs;
using EcoNexus.Domain.Entities;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.ListJobs;

internal sealed class ListJobsHandler
    : IRequestHandler<ListJobsQuery, IReadOnlyList<CollectionJobResponse>>
{
    private readonly ICollectionJobRepository _repository;

    public ListJobsHandler(ICollectionJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<CollectionJobResponse>> Handle(
        ListJobsQuery request,
        CancellationToken cancellationToken)
    {
        var jobs = await _repository.ListAsync(cancellationToken);
        return jobs.Select(Map).ToList();
    }

    internal static CollectionJobResponse Map(CollectionJob job) => new(
        job.Id,
        job.VehicleId,
        job.Status.ToString(),
        job.ScheduledFor,
        job.StartedAt,
        job.CompletedAt,
        job.Stops
            .OrderBy(s => s.Sequence)
            .Select(s => new RouteStopResponse(
                s.Id,
                s.StationId,
                s.Sequence,
                s.CollectedWeight?.Kilograms,
                s.CompletedAt))
            .ToList(),
        job.TotalCollected.Kilograms);
}
