using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A physical waste station (smart bin). Aggregate root: owns its historical
/// readings, enforces fill-level invariants, and raises domain events.
/// </summary>
public sealed class WasteStation : AggregateRoot
{
    private readonly List<StationReading> _readings = new();

    public StationCode Code { get; private set; }
    public Location Location { get; private set; }
    public Weight Capacity { get; private set; }
    public FillLevel CurrentFill { get; private set; }
    public WasteCategory PrimaryCategory { get; private set; }
    public StationStatus Status { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }
    public DateTimeOffset? LastCollectedAt { get; private set; }

    /// <summary>Historical readings for this station (read-only).</summary>
    public IReadOnlyCollection<StationReading> Readings => _readings.AsReadOnly();

    // Required by EF Core
    private WasteStation()
    {
        Code = null!;
        Location = null!;
        Capacity = null!;
        CurrentFill = FillLevel.Empty();
    }

    private WasteStation(
        StationCode code,
        Location location,
        Weight capacity,
        WasteCategory primaryCategory)
    {
        Code = code ?? throw new ArgumentNullException(nameof(code));
        Location = location ?? throw new ArgumentNullException(nameof(location));
        Capacity = capacity ?? throw new ArgumentNullException(nameof(capacity));
        PrimaryCategory = primaryCategory;
        CurrentFill = FillLevel.Empty();
        Status = StationStatus.Online;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }

    /// <summary>
    /// Factory: creates a new online waste station with empty fill.
    /// </summary>
    public static WasteStation Create(
        StationCode code,
        Location location,
        Weight capacity,
        WasteCategory primaryCategory)
    {
        if (capacity.Kilograms <= 0)
        {
            throw new ArgumentException(
                "Station capacity must be greater than zero.",
                nameof(capacity));
        }

        return new WasteStation(code, location, capacity, primaryCategory);
    }

    /// <summary>
    /// Updates the current fill level from a sensor reading. Records the
    /// reading, raises a fill-level-changed event, and raises a critical-fill
    /// event if the threshold is crossed for the first time.
    /// </summary>
    public StationReading RecordReading(
        FillLevel fillLevel,
        double temperatureCelsius,
        int batteryPercent,
        DateTimeOffset recordedAt)
    {
        ArgumentNullException.ThrowIfNull(fillLevel);

        var previousFill = CurrentFill;
        var wasCritical = previousFill.IsCritical;

        CurrentFill = fillLevel;
        LastUpdatedAt = recordedAt;

        var reading = new StationReading(Id, fillLevel, temperatureCelsius, batteryPercent, recordedAt);
        _readings.Add(reading);

        RaiseDomainEvent(new WasteStationFillLevelChangedEvent(
            Id,
            Code.Value,
            fillLevel,
            recordedAt));

        // Raise critical event only on the transition into critical.
        if (!wasCritical && fillLevel.IsCritical)
        {
            RaiseDomainEvent(new WasteStationReachedCriticalFillEvent(
                Id,
                Code.Value,
                fillLevel,
                recordedAt));
        }

        return reading;
    }

    /// <summary>
    /// Records a collection: resets fill to empty and stamps the time.
    /// </summary>
    public void Collect(DateTimeOffset collectedAt)
    {
        CurrentFill = FillLevel.Empty();
        LastCollectedAt = collectedAt;
        LastUpdatedAt = collectedAt;

        RaiseDomainEvent(new WasteStationCollectedEvent(
            Id,
            Code.Value,
            collectedAt));
    }

    public void MarkOffline(DateTimeOffset at)
    {
        Status = StationStatus.Offline;
        LastUpdatedAt = at;
    }

    public void MarkOnline(DateTimeOffset at)
    {
        Status = StationStatus.Online;
        LastUpdatedAt = at;
    }

    public void SendForMaintenance(DateTimeOffset at)
    {
        Status = StationStatus.Maintenance;
        LastUpdatedAt = at;
    }

    public void Decommission(DateTimeOffset at)
    {
        Status = StationStatus.Decommissioned;
        LastUpdatedAt = at;
    }

    /// <summary>
    /// Updates the mutable metadata of the station (location, capacity, category).
    /// Code, CurrentFill, and Status are intentionally NOT changeable here.
    /// </summary>
    public void UpdateMetadata(
        Location location,
        Weight capacity,
        WasteCategory category)
    {
        ArgumentNullException.ThrowIfNull(location);
        ArgumentNullException.ThrowIfNull(capacity);

        if (capacity.Kilograms <= 0)
        {
            throw new ArgumentException(
                "Station capacity must be greater than zero.",
                nameof(capacity));
        }

        Location = location;
        Capacity = capacity;
        PrimaryCategory = category;
        LastUpdatedAt = DateTimeOffset.UtcNow;
    }
}
