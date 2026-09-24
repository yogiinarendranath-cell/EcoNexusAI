using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.CollectionJobs.ListJobs;
using EcoNexus.Contracts.CollectionJobs;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.GetJobById;

internal sealed class GetJobByIdHandler
    : IRequestHandler<GetJobByIdQuery, CollectionJobResponse>
{
    private readonly ICollectionJobRepository _repository;

    public GetJobByIdHandler(ICollectionJobRepository repository)
    {
        _repository = repository;
    }

    public async Task<CollectionJobResponse> Handle(
        GetJobByIdQuery request,
        CancellationToken cancellationToken)
    {
        var job = await _repository.GetByIdWithStopsAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Collection job '{request.Id}' was not found.");

        return ListJobsHandler.Map(job);
    }
}
