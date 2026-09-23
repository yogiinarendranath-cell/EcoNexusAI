using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Stations.RecordStationReading;

internal sealed class RecordStationReadingHandler
    : IRequestHandler<RecordStationReadingCommand, RecordReadingResponse>
{
    private readonly IWasteStationRepository _repository;

    public RecordStationReadingHandler(IWasteStationRepository repository)
    {
        _repository = repository;
    }

    public async Task<RecordReadingResponse> Handle(
        RecordStationReadingCommand request,
        CancellationToken cancellationToken)
    {
        var station = await _repository.GetByIdAsync(request.StationId, cancellationToken);

        if (station is null)
        {
            throw new NotFoundException($"Station '{request.StationId}' was not found.");
        }

        var wasCritical = station.CurrentFill.IsCritical;

        var fillLevel = FillLevel.FromPercent(request.Request.FillLevelPercent);

        var reading = station.RecordReading(
            fillLevel,
            request.Request.TemperatureCelsius,
            request.Request.BatteryPercent,
            request.Request.RecordedAt);

        await _repository.SaveChangesAsync(cancellationToken);

        var isCritical = station.CurrentFill.IsCritical;
        var criticalTransition = !wasCritical && isCritical;

        return new RecordReadingResponse(
            station.Id,
            reading.Id,
            station.CurrentFill.Percent,
            isCritical,
            criticalTransition,
            station.LastUpdatedAt);
    }
}
