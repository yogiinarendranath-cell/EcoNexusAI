using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Domain.Services;
using EcoNexus.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace EcoNexus.Application.Features.Stations.ForecastStationFillLevel;

/// <summary>
/// Handles the forecast query: loads the station with recent readings,
/// runs the linear-regression forecaster, and returns the result.
/// </summary>
internal sealed class ForecastStationFillLevelHandler
    : IRequestHandler<ForecastStationFillLevelQuery, ForecastResult>
{
    private readonly IWasteStationRepository _stations;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<ForecastStationFillLevelHandler> _logger;

    public ForecastStationFillLevelHandler(
        IWasteStationRepository stations,
        TimeProvider timeProvider,
        ILogger<ForecastStationFillLevelHandler> logger)
    {
        _stations = stations ?? throw new ArgumentNullException(nameof(stations));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ForecastResult> Handle(
        ForecastStationFillLevelQuery request,
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

        var ordered = station.Readings
            .OrderBy(r => r.RecordedAt)
            .ToList();

        var forecast = FillLevelForecaster.Forecast(station.Id, ordered, now);

        _logger.LogInformation(
            "Forecast for station {StationId}: fill={Fill}, rate={Rate}, overflowAt={OverflowAt}, confidence={Confidence}, samples={Samples}, method={Method}",
            station.Id,
            forecast.CurrentFillPercent,
            forecast.FillRatePercentPerHour,
            forecast.PredictedOverflowAt,
            forecast.Confidence,
            forecast.SampleSize,
            forecast.Method);

        return forecast;
    }
}

