namespace EcoNexus.Contracts.Citizen;

/// <summary>Response after a successful station visit.</summary>
public sealed record RecordStationVisitResponse(
    Guid CitizenProfileId,
    Guid TransactionId,
    Guid StationId,
    int PointsAwarded,
    int NewBalance,
    int CurrentStreakDays,
    DateTimeOffset OccurredAt);
