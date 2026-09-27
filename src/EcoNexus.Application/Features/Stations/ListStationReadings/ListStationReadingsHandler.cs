using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Application.Features.Stations.ListStationReadings;

/// <summary>
/// Returns the most recent readings for a station within a time window.
/// Reads are ordered ascending by RecordedAt so the UI can plot them directly.
/// </summary>
internal sealed class ListStationReadingsHandler
    : IRequestHandler<ListStationReadingsQuery, IReadOnlyList<StationReadingResponse>>
{
    private readonly IWasteStationRepository _stations;
    private readonly TimeProvider _timeProvider;

    public ListStationReadingsHandler(
        IWasteStationRepository stations,
        TimeProvider timeProvider)
    {
        _stations = stations ?? throw new ArgumentNullException(nameof(stations));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    public async Task<IReadOnlyList<StationReadingResponse>> Handle(
        ListStationReadingsQuery request,
        CancellationToken cancellationToken)
    {
        var now = _timeProvider.GetUtcNow();
        var since = now.AddHours(-request.WindowHours);

        var station = await _stations.GetWithReadingsAsync(
            request.StationId,
            since,
            cancellationToken);

        if (station is null)
        {
            throw new NotFoundException("Station " + request.StationId + " was not found.");
        }

        var readings = station.Readings
            .OrderBy(r => r.RecordedAt)
            .Take(request.Limit)
            .Select(r => new StationReadingResponse(
                Id: r.Id,
                StationId: r.StationId,
                FillLevelPercent: r.FillLevel.Percent,
                TemperatureCelsius: r.TemperatureCelsius,
                BatteryPercent: r.BatteryPercent,
                RecordedAt: r.RecordedAt))
            .ToList();

        return readings;
    }
}

