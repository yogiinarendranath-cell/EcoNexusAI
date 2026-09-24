namespace EcoNexus.Contracts.Recycling;

/// <summary>A single intake batch, as exposed over the API.</summary>
public sealed record IntakeResponse(
    Guid Id,
    string Material,
    double WeightKilograms,
    string Stage,
    DateTimeOffset RecordedAt,
    DateTimeOffset? StageUpdatedAt);
