using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.Services;
using EcoNexus.Domain.ValueObjects;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Services;

/// <summary>
/// Unit tests for <see cref="RecyclingMetricsCalculator"/> — the pure
/// domain service that aggregates facility intake data into KPIs.
/// </summary>
public sealed class RecyclingMetricsCalculatorTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private static RecyclingFacility NewFacility(int dailyCapacityKg = 10_000)
        => RecyclingFacility.Create(
            name: "Test Facility",
            location: Location.Create(12.9716, 77.5946),
            dailyCapacity: Weight.FromKilograms(dailyCapacityKg));

    private static FacilityIntake IntakeAtStage(FacilityIntake intake, IntakeStage stage)
    {
        // AdvanceTo is internal but InternalsVisibleTo(EcoNexus.UnitTests)
        // makes it callable from this assembly. IntakeStage values are ordered,
        // and Landfilled is a side-branch from any non-terminal stage.
        if (stage == IntakeStage.Recovered)
        {
            intake.AdvanceTo(IntakeStage.Sorted, T0.AddMinutes(1));
            intake.AdvanceTo(IntakeStage.Processed, T0.AddMinutes(2));
            intake.AdvanceTo(IntakeStage.Recovered, T0.AddMinutes(3));
        }
        else if (stage == IntakeStage.Landfilled)
        {
            intake.AdvanceTo(IntakeStage.Landfilled, T0.AddMinutes(1));
        }
        return intake;
    }

    private static FacilityIntake MakeIntake(RecyclingFacility facility, double kg, IntakeStage stage)
    {
        var intake = facility.RecordIntake(WasteCategory.Plastic, Weight.FromKilograms(kg), T0);
        return IntakeAtStage(intake, stage);
    }

    // -------------------------------------------------------------
    // Empty / trivial cases
    // -------------------------------------------------------------

    [Fact]
    public void Compute_EmptyIntakes_ReturnsZeroMetrics()
    {
        var metrics = RecyclingMetricsCalculator.Compute(Array.Empty<FacilityIntake>());

        Assert.Equal(0, metrics.TotalBatches);
        Assert.Equal(0, metrics.RecoveredBatches);
        Assert.Equal(0, metrics.ReceivedKilograms);
        Assert.Equal(0, metrics.RecoveredKilograms);
        Assert.Equal(0, metrics.LandfilledKilograms);
        Assert.Equal(0, metrics.RecyclingRate);
        Assert.Equal(0, metrics.LandfillDiversion);
        Assert.Equal(0, metrics.Co2SavedKilograms);
    }

    [Fact]
    public void Compute_NullIntakes_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RecyclingMetricsCalculator.Compute((IEnumerable<FacilityIntake>)null!));
    }

    [Fact]
    public void Compute_NullFacility_Throws()
    {
        Assert.Throws<ArgumentNullException>(() =>
            RecyclingMetricsCalculator.Compute((RecyclingFacility)null!));
    }

    // -------------------------------------------------------------
    // Single-intake cases
    // -------------------------------------------------------------

    [Fact]
    public void Compute_SingleReceivedIntake_CountsAsReceivedOnly()
    {
        var facility = NewFacility();
        var intake = facility.RecordIntake(WasteCategory.Plastic, Weight.FromKilograms(100), T0);

        var metrics = RecyclingMetricsCalculator.Compute(new[] { intake });

        Assert.Equal(1, metrics.TotalBatches);
        Assert.Equal(0, metrics.RecoveredBatches);
        Assert.Equal(100, metrics.ReceivedKilograms);
        Assert.Equal(0, metrics.RecoveredKilograms);
        Assert.Equal(0, metrics.LandfilledKilograms);
        Assert.Equal(0, metrics.RecyclingRate);
    }

    [Fact]
    public void Compute_SingleRecoveredIntake_ContributesToRecoveredKilograms()
    {
        var facility = NewFacility();
        var intake = MakeIntake(facility, 100, IntakeStage.Recovered);

        var metrics = RecyclingMetricsCalculator.Compute(new[] { intake });

        Assert.Equal(1, metrics.TotalBatches);
        Assert.Equal(1, metrics.RecoveredBatches);
        Assert.Equal(100, metrics.ReceivedKilograms);
        Assert.Equal(100, metrics.RecoveredKilograms);
        Assert.Equal(0, metrics.LandfilledKilograms);
        Assert.Equal(1.0, metrics.RecyclingRate, precision: 6);
        Assert.Equal(1.0, metrics.LandfillDiversion, precision: 6);
    }

    [Fact]
    public void Compute_SingleLandfilledIntake_ContributesToLandfilledKilograms()
    {
        var facility = NewFacility();
        var intake = MakeIntake(facility, 100, IntakeStage.Landfilled);

        var metrics = RecyclingMetricsCalculator.Compute(new[] { intake });

        Assert.Equal(1, metrics.TotalBatches);
        Assert.Equal(0, metrics.RecoveredBatches);
        Assert.Equal(100, metrics.ReceivedKilograms);
        Assert.Equal(0, metrics.RecoveredKilograms);
        Assert.Equal(100, metrics.LandfilledKilograms);
        Assert.Equal(0, metrics.RecyclingRate);
        Assert.Equal(0, metrics.LandfillDiversion, precision: 6);
    }

    // -------------------------------------------------------------
    // Aggregation
    // -------------------------------------------------------------

    [Fact]
    public void Compute_MixedStages_SumsCorrectly()
    {
        var facility = NewFacility();
        var received    = facility.RecordIntake(WasteCategory.Plastic, Weight.FromKilograms(50), T0);
        var recovered1  = MakeIntake(facility, 100, IntakeStage.Recovered);
        var recovered2  = MakeIntake(facility, 200, IntakeStage.Recovered);
        var landfilled  = MakeIntake(facility, 150, IntakeStage.Landfilled);

        var metrics = RecyclingMetricsCalculator.Compute(new[] { received, recovered1, recovered2, landfilled });

        Assert.Equal(4, metrics.TotalBatches);
        Assert.Equal(2, metrics.RecoveredBatches);
        Assert.Equal(500, metrics.ReceivedKilograms);
        Assert.Equal(300, metrics.RecoveredKilograms);
        Assert.Equal(150, metrics.LandfilledKilograms);
    }

    // -------------------------------------------------------------
    // Derived metrics
    // -------------------------------------------------------------

    [Fact]
    public void Compute_RecyclingRate_IsRecoveredOverReceived()
    {
        var facility = NewFacility();
        var recovered = MakeIntake(facility, 60, IntakeStage.Recovered);
        var received  = facility.RecordIntake(WasteCategory.Plastic, Weight.FromKilograms(40), T0);

        var metrics = RecyclingMetricsCalculator.Compute(new[] { recovered, received });

        Assert.Equal(0.6, metrics.RecyclingRate, precision: 6);
    }

    [Fact]
    public void Compute_LandfillDiversion_IsOneMinusLandfilledOverReceived()
    {
        var facility = NewFacility();
        var recovered  = MakeIntake(facility, 70, IntakeStage.Recovered);
        var landfilled = MakeIntake(facility, 30, IntakeStage.Landfilled);

        var metrics = RecyclingMetricsCalculator.Compute(new[] { recovered, landfilled });

        Assert.Equal(0.7, metrics.LandfillDiversion, precision: 6);
    }

    [Fact]
    public void Compute_Co2Saved_IsRecoveredKilogramsTimesConstant()
    {
        var facility = NewFacility();
        var recovered = MakeIntake(facility, 200, IntakeStage.Recovered);

        var metrics = RecyclingMetricsCalculator.Compute(new[] { recovered });

        var expected = 200 * RecyclingMetricsCalculator.Co2SavedPerKilogramRecovered;
        Assert.Equal(expected, metrics.Co2SavedKilograms, precision: 6);
    }

    // -------------------------------------------------------------
    // Facility overload
    // -------------------------------------------------------------

    [Fact]
    public void Compute_Facility_DelegatesToIntakesOverload()
    {
        var facility = NewFacility();
        MakeIntake(facility, 100, IntakeStage.Recovered);
        MakeIntake(facility, 50, IntakeStage.Landfilled);

        var byFacility = RecyclingMetricsCalculator.Compute(facility);
        var byIntakes  = RecyclingMetricsCalculator.Compute(facility.Intakes);

        Assert.Equal(byIntakes.TotalBatches, byFacility.TotalBatches);
        Assert.Equal(byIntakes.RecoveredBatches, byFacility.RecoveredBatches);
        Assert.Equal(byIntakes.ReceivedKilograms, byFacility.ReceivedKilograms);
        Assert.Equal(byIntakes.RecoveredKilograms, byFacility.RecoveredKilograms);
        Assert.Equal(byIntakes.LandfilledKilograms, byFacility.LandfilledKilograms);
        Assert.Equal(byIntakes.RecyclingRate, byFacility.RecyclingRate);
        Assert.Equal(byIntakes.Co2SavedKilograms, byFacility.Co2SavedKilograms);
    }

    [Fact]
    public void Compute_FacilityWithNoIntakes_ReturnsZeroMetrics()
    {
        var facility = NewFacility();

        var metrics = RecyclingMetricsCalculator.Compute(facility);

        Assert.Equal(0, metrics.TotalBatches);
        Assert.Equal(0, metrics.ReceivedKilograms);
    }
}

