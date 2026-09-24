namespace EcoNexus.Contracts.Recycling;

/// <summary>Response returned after an intake batch is recorded.</summary>
public sealed record RecordIntakeResponse(
    Guid FacilityId,
    Guid IntakeId,
    string Material,
    double WeightKilograms,
    string Stage,
    DateTimeOffset RecordedAt);
