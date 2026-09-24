using EcoNexus.Contracts.CollectionJobs;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.ListJobs;

public sealed record ListJobsQuery() : IRequest<IReadOnlyList<CollectionJobResponse>>;
