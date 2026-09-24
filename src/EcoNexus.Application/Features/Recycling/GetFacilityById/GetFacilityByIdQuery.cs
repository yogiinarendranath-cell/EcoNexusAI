using EcoNexus.Contracts.Recycling;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.GetFacilityById;

/// <summary>Query: full facility detail including intake history and metrics.</summary>
public sealed record GetFacilityByIdQuery(Guid FacilityId)
    : IRequest<FacilityDetailResponse>;
