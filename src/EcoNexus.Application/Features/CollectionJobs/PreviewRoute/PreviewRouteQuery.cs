using EcoNexus.Contracts.CollectionJobs;
using MediatR;

namespace EcoNexus.Application.Features.CollectionJobs.PreviewRoute;

public sealed record PreviewRouteQuery(
    PreviewRouteRequest Request
) : IRequest<PreviewRouteResponse>;
