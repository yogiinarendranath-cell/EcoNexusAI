namespace EcoNexus.Contracts.Recycling;

/// <summary>Immutable snapshot of a facility's recycling metrics.</summary>
public sealed record RecyclingMetricsResponse(
    int TotalBatches,
    int RecoveredBatches,
    double ReceivedKilograms,
    double RecoveredKilograms,
    double LandfilledKilograms,
    double RecyclingRate,
    double LandfillDiversion,
    double Co2SavedKilograms);
