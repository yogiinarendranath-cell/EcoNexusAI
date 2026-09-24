using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A recycling facility that receives collected material and processes it
/// through intake -> sorting -> processing -> recovery. Aggregate root:
/// owns its intake history and raises domain events on intake operations.
/// </summary>
public sealed class RecyclingFacility : AggregateRoot
{
    private readonly List<FacilityIntake> _intakes = new();

    public string Name { get; private set; }
    public Location Location { get; private set; }
    public Weight DailyCapacity { get; private set; }
    public FacilityStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }

    /// <summary>Historical intake batches for this facility (read-only).</summary>
    public IReadOnlyCollection<FacilityIntake> Intakes => _intakes.AsReadOnly();

    // Required by EF Core
    private RecyclingFacility()
    {
        Name = null!;
        Location = null!;
        DailyCapacity = Weight.FromKilograms(0);
    }

    private RecyclingFacility(
        string name,
        Location location,
        Weight dailyCapacity)
    {
        Name = ValidateName(name);
        Location = location ?? throw new ArgumentNullException(nameof(location));
        DailyCapacity = dailyCapacity ?? throw new ArgumentNullException(nameof(dailyCapacity));

        if (dailyCapacity.Kilograms <= 0)
        {
            throw new ArgumentException(
                "Daily capacity must be greater than zero.",
                nameof(dailyCapacity));
        }

        Status = FacilityStatus.Online;
        CreatedAt = DateTimeOffset.UtcNow;
        LastUpdatedAt = CreatedAt;
    }

    /// <summary>
    /// Factory: creates a new online recycling facility.
    /// </summary>
    public static RecyclingFacility Create(
        string name,
        Location location,
        Weight dailyCapacity)
        => new(name, location, dailyCapacity);

    /// <summary>
    /// Records a new intake batch. Raises RecyclingIntakeRecordedEvent.
    /// </summary>
    public FacilityIntake RecordIntake(
        WasteCategory material,
        Weight weight,
        DateTimeOffset recordedAt)
    {
        if (Status == FacilityStatus.Decommissioned)
        {
            throw new InvalidOperationException(
                "Cannot record intake at a decommissioned facility.");
        }

        var intake = new FacilityIntake(Id, material, weight, recordedAt);
        _intakes.Add(intake);
        LastUpdatedAt = recordedAt;

        RaiseDomainEvent(new RecyclingIntakeRecordedEvent(
            Id,
            Name,
            intake.Id,
            material,
            weight.Kilograms,
            recordedAt));

        return intake;
    }

    /// <summary>
    /// Advances an intake to a new lifecycle stage. Raises
    /// RecyclingIntakeAdvancedEvent on success.
    /// </summary>
    public void AdvanceIntake(Guid intakeId, IntakeStage nextStage, DateTimeOffset at)
    {
        var intake = _intakes.FirstOrDefault(i => i.Id == intakeId)
            ?? throw new InvalidOperationException(
                $"Intake '{intakeId}' does not belong to this facility.");

        var previous = intake.AdvanceTo(nextStage, at);
        LastUpdatedAt = at;

        RaiseDomainEvent(new RecyclingIntakeAdvancedEvent(
            Id,
            intakeId,
            previous,
            nextStage,
            at));
    }

    public void MarkOffline(DateTimeOffset at)
    {
        Status = FacilityStatus.Offline;
        LastUpdatedAt = at;
    }

    public void MarkOnline(DateTimeOffset at)
    {
        Status = FacilityStatus.Online;
        LastUpdatedAt = at;
    }

    public void SendForMaintenance(DateTimeOffset at)
    {
        Status = FacilityStatus.Maintenance;
        LastUpdatedAt = at;
    }

    public void Decommission(DateTimeOffset at)
    {
        Status = FacilityStatus.Decommissioned;
        LastUpdatedAt = at;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Facility name must not be empty.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 120)
        {
            throw new ArgumentException(
                "Facility name must be 120 characters or fewer.",
                nameof(name));
        }

        return trimmed;
    }
}
