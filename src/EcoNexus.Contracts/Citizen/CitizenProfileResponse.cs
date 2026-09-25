namespace EcoNexus.Contracts.Citizen;

/// <summary>Current citizen's profile, points, streak, and home location.</summary>
public sealed record CitizenProfileResponse(
    Guid Id,
    Guid UserId,
    string DisplayName,
    string? HomeAddress,
    double? HomeLatitude,
    double? HomeLongitude,
    int GreenPointsBalance,
    int CurrentStreakDays,
    DateOnly? LastVisitDate,
    int TotalTransactions,
    int TotalEarned,
    int TotalRedeemed,
    DateTimeOffset CreatedAt,
    DateTimeOffset LastUpdatedAt);
