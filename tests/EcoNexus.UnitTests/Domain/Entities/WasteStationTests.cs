using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

public sealed class WasteStationTests
{
    private static WasteStation CreateStation()
    {
        return WasteStation.Create(
            StationCode.Create("ST-1001"),
            Location.Create(12.97, 77.59),
            Weight.FromKilograms(500),
            WasteCategory.General);
    }

    [Fact]
    public void Create_WithValidInputs_ReturnsOnlineStation()
    {
        var station = CreateStation();

        Assert.Equal(StationStatus.Online, station.Status);
        Assert.Equal(0, station.CurrentFill.Percent);
        Assert.Null(station.LastCollectedAt);
        Assert.Empty(station.Readings);
    }

    [Fact]
    public void Create_WithZeroCapacity_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            WasteStation.Create(
                StationCode.Create("ST-1001"),
                Location.Create(12.97, 77.59),
                Weight.FromKilograms(0),
                WasteCategory.General));
    }

    [Fact]
    public void RecordReading_UpdatesFillAndAppendsReading()
    {
        var station = CreateStation();
        var fill = FillLevel.FromPercent(45.5);

        var reading = station.RecordReading(fill, 28.5, 92, DateTimeOffset.UtcNow);

        Assert.Equal(45.5, station.CurrentFill.Percent);
        Assert.Single(station.Readings);
        Assert.Equal(station.Id, reading.StationId);
        Assert.Equal(28.5, reading.TemperatureCelsius);
        Assert.Equal(92, reading.BatteryPercent);
    }

    [Fact]
    public void RecordReading_RaisesFillLevelChangedEvent()
    {
        var station = CreateStation();
        station.ClearDomainEvents();

        station.RecordReading(FillLevel.FromPercent(30), 25, 100, DateTimeOffset.UtcNow);

        Assert.Single(station.DomainEvents);
        Assert.IsType<WasteStationFillLevelChangedEvent>(station.DomainEvents.First());
    }

    [Fact]
    public void RecordReading_WhenCrossingIntoCritical_RaisesCriticalEvent()
    {
        var station = CreateStation();
        station.RecordReading(FillLevel.FromPercent(80), 25, 100, DateTimeOffset.UtcNow);
        station.ClearDomainEvents();

        station.RecordReading(FillLevel.FromPercent(95), 25, 100, DateTimeOffset.UtcNow);

        Assert.Equal(2, station.DomainEvents.Count);
        Assert.Contains(station.DomainEvents, e => e is WasteStationFillLevelChangedEvent);
        Assert.Contains(station.DomainEvents, e => e is WasteStationReachedCriticalFillEvent);
    }

    [Fact]
    public void RecordReading_WhenAlreadyCritical_DoesNotRaiseCriticalEventAgain()
    {
        var station = CreateStation();
        station.RecordReading(FillLevel.FromPercent(95), 25, 100, DateTimeOffset.UtcNow);
        station.ClearDomainEvents();

        station.RecordReading(FillLevel.FromPercent(98), 25, 100, DateTimeOffset.UtcNow);

        Assert.Single(station.DomainEvents);
        Assert.IsType<WasteStationFillLevelChangedEvent>(station.DomainEvents.First());
        Assert.DoesNotContain(station.DomainEvents, e => e is WasteStationReachedCriticalFillEvent);
    }

    [Fact]
    public void Collect_ResetsFillAndRaisesCollectedEvent()
    {
        var station = CreateStation();
        station.RecordReading(FillLevel.FromPercent(85), 25, 100, DateTimeOffset.UtcNow);
        station.ClearDomainEvents();

        var collectedAt = DateTimeOffset.UtcNow;
        station.Collect(collectedAt);

        Assert.Equal(0, station.CurrentFill.Percent);
        Assert.Equal(collectedAt, station.LastCollectedAt);
        Assert.Single(station.DomainEvents);
        Assert.IsType<WasteStationCollectedEvent>(station.DomainEvents.First());
    }

    [Fact]
    public void MarkOffline_UpdatesStatus()
    {
        var station = CreateStation();
        station.MarkOffline(DateTimeOffset.UtcNow);
        Assert.Equal(StationStatus.Offline, station.Status);
    }

    [Fact]
    public void SendForMaintenance_UpdatesStatus()
    {
        var station = CreateStation();
        station.SendForMaintenance(DateTimeOffset.UtcNow);
        Assert.Equal(StationStatus.Maintenance, station.Status);
    }

    [Fact]
    public void Decommission_UpdatesStatus()
    {
        var station = CreateStation();
        station.Decommission(DateTimeOffset.UtcNow);
        Assert.Equal(StationStatus.Decommissioned, station.Status);
    }
}
