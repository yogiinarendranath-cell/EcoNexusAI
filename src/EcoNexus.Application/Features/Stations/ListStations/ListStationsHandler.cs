using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Common;
using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.ListStations;

internal sealed class ListStationsHandler
    : IRequestHandler<ListStationsQuery, PagedResult<StationListItemResponse>>
{
    private readonly IWasteStationRepository _repository;

    public ListStationsHandler(IWasteStationRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<StationListItemResponse>> Handle(
        ListStationsQuery request,
        CancellationToken cancellationToken)
    {
        var p = request.Parameters;

        var (items, totalCount) = await _repository.ListAsync(p, cancellationToken);

        var projected = items
            .Select(s => new StationListItemResponse(
                s.Id,
                s.Code.Value,
                s.Location.Latitude,
                s.Location.Longitude,
                s.CurrentFill.Percent,
                s.PrimaryCategory.ToString(),
                s.Status.ToString(),
                s.CurrentFill.IsCritical,
                s.LastUpdatedAt,
                s.LastCollectedAt))
            .ToList();

        return new PagedResult<StationListItemResponse>(
            projected,
            p.Page,
            p.PageSize,
            totalCount);
    }
}
