using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Services;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.ListFacilities;

internal sealed class ListFacilitiesHandler
    : IRequestHandler<ListFacilitiesQuery, IReadOnlyList<FacilityListItemResponse>>
{
    private readonly IRecyclingFacilityRepository _repository;

    public ListFacilitiesHandler(IRecyclingFacilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<IReadOnlyList<FacilityListItemResponse>> Handle(
        ListFacilitiesQuery request,
        CancellationToken cancellationToken)
    {
        var facilities = await _repository.GetAllAsync(cancellationToken);

        return facilities
            .Select(f =>
            {
                var metrics = RecyclingMetricsCalculator.Compute(f);
                return new FacilityListItemResponse(
                    f.Id,
                    f.Name,
                    f.Location.Latitude,
                    f.Location.Longitude,
                    f.DailyCapacity.Kilograms,
                    f.Status.ToString(),
                    f.Intakes.Count,
                    new RecyclingMetricsResponse(
                        metrics.TotalBatches,
                        metrics.RecoveredBatches,
                        metrics.ReceivedKilograms,
                        metrics.RecoveredKilograms,
                        metrics.LandfilledKilograms,
                        metrics.RecyclingRate,
                        metrics.LandfillDiversion,
                        metrics.Co2SavedKilograms),
                    f.LastUpdatedAt);
            })
            .ToList();
    }
}
