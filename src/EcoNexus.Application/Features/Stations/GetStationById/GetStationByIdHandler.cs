using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.GetStationById;

internal sealed class GetStationByIdHandler
    : IRequestHandler<GetStationByIdQuery, StationDetailResponse?>
{
    private readonly IWasteStationRepository _repository;

    public GetStationByIdHandler(IWasteStationRepository repository)
    {
        _repository = repository;
    }

    public async Task<StationDetailResponse?> Handle(
        GetStationByIdQuery request,
        CancellationToken cancellationToken)
    {
        var station = await _repository.GetByIdAsync(request.Id, cancellationToken);

        if (station is null)
        {
            return null;
        }

        return new StationDetailResponse(
            station.Id,
            station.Code.Value,
            station.Location.Latitude,
            station.Location.Longitude,
            station.Capacity.Kilograms,
            station.CurrentFill.Percent,
            station.PrimaryCategory.ToString(),
            station.Status.ToString(),
            station.LastUpdatedAt,
            station.LastCollectedAt);
    }
}
