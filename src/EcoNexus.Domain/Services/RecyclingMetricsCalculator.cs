using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.Services;

/// <summary>
/// Pure domain service for computing recycling metrics from a facility's
/// intake history. Deterministic - same inputs, same outputs.
/// </summary>
public static class RecyclingMetricsCalculator
{
    /// <summary>
    /// Estimated kilograms of CO2 avoided per kilogram of material recovered.
    /// Single tuning point for the whole platform.
    /// </summary>
    public const double Co2SavedPerKilogramRecovered = 1.5;

    /// <summary>
    /// Computes metrics for a single facility based on its full intake history.
    /// </summary>
    public static RecyclingMetrics Compute(RecyclingFacility facility)
    {
        ArgumentNullException.ThrowIfNull(facility);
        return Compute(facility.Intakes);
    }

    /// <summary>
    /// Computes metrics for a set of intakes. Useful for aggregate metrics
    /// across multiple facilities or filtered time windows.
    /// </summary>
    public static RecyclingMetrics Compute(IEnumerable<FacilityIntake> intakes)
    {
        ArgumentNullException.ThrowIfNull(intakes);

        double receivedKg = 0;
        double recoveredKg = 0;
        double landfilledKg = 0;
        int totalBatches = 0;
        int recoveredBatches = 0;

        foreach (var intake in intakes)
        {
            totalBatches++;
            receivedKg += intake.Weight.Kilograms;

            switch (intake.Stage)
            {
                case IntakeStage.Recovered:
                    recoveredKg += intake.Weight.Kilograms;
                    recoveredBatches++;
                    break;

                case IntakeStage.Landfilled:
                    landfilledKg += intake.Weight.Kilograms;
                    break;
            }
        }

        var recyclingRate = receivedKg > 0 ? recoveredKg / receivedKg : 0;
        var landfillDiversion = receivedKg > 0 ? 1 - (landfilledKg / receivedKg) : 0;
        var co2SavedKg = recoveredKg * Co2SavedPerKilogramRecovered;

        return new RecyclingMetrics(
            TotalBatches: totalBatches,
            RecoveredBatches: recoveredBatches,
            ReceivedKilograms: receivedKg,
            RecoveredKilograms: recoveredKg,
            LandfilledKilograms: landfilledKg,
            RecyclingRate: recyclingRate,
            LandfillDiversion: landfillDiversion,
            Co2SavedKilograms: co2SavedKg);
    }
}

/// <summary>
/// Immutable snapshot of a recycling facility's performance metrics.
/// </summary>
public sealed record RecyclingMetrics(
    int TotalBatches,
    int RecoveredBatches,
    double ReceivedKilograms,
    double RecoveredKilograms,
    double LandfilledKilograms,
    double RecyclingRate,
    double LandfillDiversion,
    double Co2SavedKilograms);
