using EcoNexus.Contracts.CollectionJobs;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.GetJobById;

public sealed record GetJobByIdQuery(Guid Id) : IRequest<CollectionJobResponse>;
