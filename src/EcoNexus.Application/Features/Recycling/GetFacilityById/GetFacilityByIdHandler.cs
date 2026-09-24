using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Services;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.GetFacilityById;

internal sealed class GetFacilityByIdHandler
    : IRequestHandler<GetFacilityByIdQuery, FacilityDetailResponse>
{
    private readonly IRecyclingFacilityRepository _repository;

    public GetFacilityByIdHandler(IRecyclingFacilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<FacilityDetailResponse> Handle(
        GetFacilityByIdQuery request,
        CancellationToken cancellationToken)
    {
        var facility = await _repository.GetByIdAsync(request.FacilityId, cancellationToken);

        if (facility is null)
        {
            throw new NotFoundException(
                $"Recycling facility '{request.FacilityId}' was not found.");
        }

        var metrics = RecyclingMetricsCalculator.Compute(facility);

        var intakes = facility.Intakes
            .OrderByDescending(i => i.RecordedAt)
            .Select(i => new IntakeResponse(
                i.Id,
                i.Material.ToString(),
                i.Weight.Kilograms,
                i.Stage.ToString(),
                i.RecordedAt,
                i.StageUpdatedAt))
            .ToList();

        return new FacilityDetailResponse(
            facility.Id,
            facility.Name,
            facility.Location.Latitude,
            facility.Location.Longitude,
            facility.DailyCapacity.Kilograms,
            facility.Status.ToString(),
            new RecyclingMetricsResponse(
                metrics.TotalBatches,
                metrics.RecoveredBatches,
                metrics.ReceivedKilograms,
                metrics.RecoveredKilograms,
                metrics.LandfilledKilograms,
                metrics.RecyclingRate,
                metrics.LandfillDiversion,
                metrics.Co2SavedKilograms),
            intakes,
            facility.CreatedAt,
            facility.LastUpdatedAt);
    }
}
