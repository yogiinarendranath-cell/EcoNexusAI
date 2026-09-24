namespace EcoNexus.Contracts.Recycling;

/// <summary>Request body for recording a new intake batch at a facility.</summary>
public sealed record RecordIntakeRequest(
    string Material,
    double WeightKilograms,
    DateTimeOffset RecordedAt);
