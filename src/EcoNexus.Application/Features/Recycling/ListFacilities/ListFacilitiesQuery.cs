using EcoNexus.Contracts.Recycling;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.ListFacilities;

/// <summary>Query: list every recycling facility with summary metrics.</summary>
public sealed record ListFacilitiesQuery
    : IRequest<IReadOnlyList<FacilityListItemResponse>>;
