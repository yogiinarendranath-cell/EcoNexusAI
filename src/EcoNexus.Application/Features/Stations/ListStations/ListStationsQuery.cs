using EcoNexus.Contracts.Common;
using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.ListStations;

public sealed record ListStationsQuery(StationListQuery Parameters)
    : IRequest<PagedResult<StationListItemResponse>>;
