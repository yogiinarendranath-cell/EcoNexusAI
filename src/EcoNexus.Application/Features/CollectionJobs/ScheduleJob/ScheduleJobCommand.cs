using EcoNexus.Contracts.CollectionJobs;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.ScheduleJob;

public sealed record ScheduleJobCommand(
    ScheduleJobRequest Request
) : IRequest<CollectionJobResponse>;
