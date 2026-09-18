using EcoNexus.Domain.Entities;

namespace EcoNexus.Worker.Features.IoT;

/// <summary>
/// Produces plausible next-reading values for a station based on its current state.
/// Deliberately simple: fill increases, battery drifts down and occasionally resets,
/// temperature fluctuates around a comfortable band.
/// </summary>
public sealed class ReadingGenerator
{
    private const double MinFillDelta = 0.5;
    private const double MaxFillDelta = 3.0;

    private const double MinTemperature = 18.0;
    private const double MaxTemperature = 34.0;

    private const int MinBatteryDrop = 0;
    private const int MaxBatteryDrop = 1;

    private readonly Random _random = new();

    public SensorReading Next(WasteStation station)
    {
        ArgumentNullException.ThrowIfNull(station);

        var fill = NextFill(station.CurrentFill.Percent);
        var temperature = NextTemperature();
        var battery = NextBattery();

        return new SensorReading(fill, temperature, battery, DateTimeOffset.UtcNow);
    }

    private double NextFill(double currentPercent)
    {
        var delta = MinFillDelta + _random.NextDouble() * (MaxFillDelta - MinFillDelta);
        var next = currentPercent + delta;

        // If station would exceed 100%, treat as "collected" — reset to a low value.
        // This simulates a collection event without needing a separate workflow.
        if (next >= 100.0)
        {
            next = _random.NextDouble() * 10.0;
        }

        return Math.Round(next, 2);
    }

    private double NextTemperature()
    {
        var range = MaxTemperature - MinTemperature;
        var value = MinTemperature + _random.NextDouble() * range;
        return Math.Round(value, 2);
    }

    private int NextBattery()
    {
        // Small chance the battery is refilled / solar-charged back to full.
        if (_random.NextDouble() < 0.02)
        {
            return 100;
        }

        var drop = _random.Next(MinBatteryDrop, MaxBatteryDrop + 1);
        var current = _random.Next(0, 100);
        return Math.Max(0, current - drop);
    }
}

/// <summary>
/// Raw sensor values produced by the generator before they are applied to a station.
/// </summary>
public readonly record struct SensorReading(
    double FillLevelPercent,
    double TemperatureCelsius,
    int BatteryPercent,
    DateTimeOffset RecordedAt);
