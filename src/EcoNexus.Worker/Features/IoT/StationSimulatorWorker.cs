using Microsoft.Extensions.Options;
using EcoNexus.Domain.Entities;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Stations.RecordStationReading;
using EcoNexus.Contracts.Stations;
using MediatR;

namespace EcoNexus.Worker.Features.IoT;

/// <summary>
/// Periodically generates synthetic sensor readings for active stations and
/// records them via the same MediatR command the HTTP API uses. This keeps
/// domain validation, logging, and business rules consistent between the
/// simulator and real clients.
/// </summary>
public sealed class StationSimulatorWorker : BackgroundService
{
    private readonly IWasteStationRepository _repository;
    private readonly IMediator _mediator;
    private readonly ReadingGenerator _generator;
    private readonly IOptions<StationSimulatorOptions> _options;
    private readonly ILogger<StationSimulatorWorker> _logger;

    public StationSimulatorWorker(
        IWasteStationRepository repository,
        IMediator mediator,
        ReadingGenerator generator,
        IOptions<StationSimulatorOptions> options,
        ILogger<StationSimulatorWorker> logger)
    {
        _repository = repository;
        _mediator = mediator;
        _generator = generator;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var opts = _options.Value;

        if (!opts.Enabled)
        {
            _logger.LogWarning("IoT simulator is disabled by configuration. Worker exiting.");
            return;
        }

        _logger.LogInformation(
            "IoT simulator started. Tick: {Tick}s, ReadingsPerTick: {Readings}.",
            opts.TickIntervalSeconds,
            opts.ReadingsPerTick);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await TickAsync(opts, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // Graceful shutdown — exit loop.
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "IoT simulator tick failed. Continuing with next tick.");
            }

            try
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(opts.TickIntervalSeconds),
                    stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }

        _logger.LogInformation("IoT simulator stopped.");
    }

    private async Task TickAsync(
        StationSimulatorOptions opts,
        CancellationToken cancellationToken)
    {
        var stations = await _repository.GetActiveAsync(cancellationToken);

        if (stations.Count == 0)
        {
            _logger.LogDebug("No active stations to simulate.");
            return;
        }

        var count = Math.Min(opts.ReadingsPerTick, stations.Count);
        var selected = PickRandom(stations, count);

        var success = 0;
        var failed = 0;

        foreach (var station in selected)
        {
            try
            {
                var reading = _generator.Next(station);

                var command = new RecordStationReadingCommand(
                    station.Id,
                    new RecordReadingRequest(
                        reading.FillLevelPercent,
                        reading.TemperatureCelsius,
                        reading.BatteryPercent,
                        reading.RecordedAt));

                await _mediator.Send(command, cancellationToken);
                success++;
            }
            catch (Exception ex)
            {
                failed++;
                _logger.LogWarning(
                    ex,
                    "Failed to record reading for station {StationId}.",
                    station.Id);
            }
        }

        _logger.LogInformation(
            "Tick complete. Active: {Active}, Sampled: {Sampled}, Success: {Success}, Failed: {Failed}.",
            stations.Count,
            count,
            success,
            failed);
    }

    private static List<WasteStation> PickRandom(
        IReadOnlyList<WasteStation> source,
        int count)
    {
        // Fisher-Yates style partial shuffle.
        var pool = source.ToList();
        var result = new List<WasteStation>(count);

        for (var i = 0; i < count; i++)
        {
            var index = Random.Shared.Next(pool.Count);
            result.Add(pool[index]);
            pool.RemoveAt(index);
        }

        return result;
    }
}
