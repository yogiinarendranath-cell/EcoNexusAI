namespace EcoNexus.Domain.Enums;

/// <summary>
/// Specific reason a green points transaction was created.
/// Combined with GreenPointSource to describe any transaction.
/// </summary>
public enum GreenPointReason
{
    StationVisit = 0,
    ReportFiled = 1,
    StreakBonus = 2,
    FirstTimeStationVisit = 3,
    RewardRedemption = 4,
    AdminAdjustment = 5
}
