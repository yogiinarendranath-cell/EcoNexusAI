namespace EcoNexus.Domain.Enums;

/// <summary>
/// The lifecycle stage of a single intake batch inside a recycling facility.
/// Progression is linear: Received -> Sorted -> Processed -> Recovered.
/// Landfilled is a terminal side-exit for material that cannot be recovered.
/// </summary>
public enum IntakeStage
{
    Received = 0,
    Sorted = 1,
    Processed = 2,
    Recovered = 3,
    Landfilled = 4
}
