using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A single batch of material received at a recycling facility.
/// Child entity of the RecyclingFacility aggregate; its lifetime is bound
/// to the owning facility. Advances linearly through IntakeStage.
/// </summary>
public sealed class FacilityIntake : Entity
{
    public Guid FacilityId { get; private set; }
    public WasteCategory Material { get; private set; }
    public Weight Weight { get; private set; }
    public IntakeStage Stage { get; private set; }
    public DateTimeOffset RecordedAt { get; private set; }
    public DateTimeOffset? StageUpdatedAt { get; private set; }

    // Required by EF Core
    private FacilityIntake()
    {
        Weight = Weight.FromKilograms(0);
    }

    internal FacilityIntake(
        Guid facilityId,
        WasteCategory material,
        Weight weight,
        DateTimeOffset recordedAt)
    {
        if (facilityId == Guid.Empty)
        {
            throw new ArgumentException("FacilityId must not be empty.", nameof(facilityId));
        }

        ArgumentNullException.ThrowIfNull(weight);

        if (weight.Kilograms <= 0)
        {
            throw new ArgumentException(
                "Intake weight must be greater than zero.",
                nameof(weight));
        }

        FacilityId = facilityId;
        Material = material;
        Weight = weight;
        Stage = IntakeStage.Received;
        RecordedAt = recordedAt;
    }

    /// <summary>
    /// Advances the intake to the given stage. Only forward transitions are
    /// allowed; the previous stage is captured by the caller for eventing.
    /// </summary>
    internal IntakeStage AdvanceTo(IntakeStage nextStage, DateTimeOffset at)
    {
        if (nextStage == Stage)
        {
            throw new InvalidOperationException(
                $"Intake is already in stage '{Stage}'.");
        }

        if (nextStage < Stage && nextStage != IntakeStage.Landfilled)
        {
            throw new InvalidOperationException(
                $"Cannot move intake backwards from '{Stage}' to '{nextStage}'.");
        }

        if (Stage == IntakeStage.Landfilled || Stage == IntakeStage.Recovered)
        {
            throw new InvalidOperationException(
                $"Intake is in terminal stage '{Stage}' and cannot advance further.");
        }

        var previous = Stage;
        Stage = nextStage;
        StageUpdatedAt = at;
        return previous;
    }
}
