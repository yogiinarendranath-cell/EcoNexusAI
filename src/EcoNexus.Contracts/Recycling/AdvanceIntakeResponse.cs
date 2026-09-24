namespace EcoNexus.Contracts.Recycling;

/// <summary>Response returned after an intake batch advances a stage.</summary>
public sealed record AdvanceIntakeResponse(
    Guid FacilityId,
    Guid IntakeId,
    string PreviousStage,
    string CurrentStage,
    DateTimeOffset AdvancedAt);
