using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A single sensor reading emitted by a waste station. Child entity of the
/// WasteStation aggregate; its lifetime is bound to the owning station.
/// </summary>
public sealed class StationReading : Entity
{
    public Guid StationId { get; private set; }
    public FillLevel FillLevel { get; private set; }
    public double TemperatureCelsius { get; private set; }
    public int BatteryPercent { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }

    // Required by EF Core
    private StationReading()
    {
        FillLevel = ValueObjects.FillLevel.Empty();
    }

    internal StationReading(
        Guid stationId,
        FillLevel fillLevel,
        double temperatureCelsius,
        int batteryPercent,
        DateTimeOffset recordedAt)
    {
        if (stationId == Guid.Empty)
        {
            throw new ArgumentException("StationId must not be empty.", nameof(stationId));
        }

        if (temperatureCelsius < -50 || temperatureCelsius > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(temperatureCelsius),
                "Temperature must be between -50 and 100 Celsius.");
        }

        if (batteryPercent < 0 || batteryPercent > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(batteryPercent),
                "Battery percent must be between 0 and 100.");
        }

        StationId = stationId;
        FillLevel = fillLevel ?? throw new ArgumentNullException(nameof(fillLevel));
        TemperatureCelsius = temperatureCelsius;
        BatteryPercent = batteryPercent;
        RecordedAt = recordedAt;
    }
}
