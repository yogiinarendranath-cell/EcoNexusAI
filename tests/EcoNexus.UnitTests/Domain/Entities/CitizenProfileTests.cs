using EcoNexus.Domain.Entities;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

/// <summary>
/// Unit tests for <see cref="CitizenProfile"/> — the citizen aggregate root
/// that owns the green-points ledger, streak, and home location.
/// </summary>
public sealed class CitizenProfileTests
{
    private static readonly Guid UserId = Guid.Parse("33333333-3333-3333-3333-333333333333");
    private static readonly DateTimeOffset T0 = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private static CitizenProfile NewProfile(string name = "Test Citizen")
        => CitizenProfile.Create(UserId, name, T0);

    private static Reward NewReward(int costInPoints = 30, bool active = true)
    {
        var reward = Reward.Create(
            name: "Test Reward",
            description: "Test description",
            costInPoints: costInPoints,
            createdAt: T0);
        if (!active) reward.Deactivate();
        return reward;
    }

    // -------------------------------------------------------------
    // Create
    // -------------------------------------------------------------

    [Fact]
    public void Create_ValidInputs_SetsInitialState()
    {
        var profile = CitizenProfile.Create(UserId, "Alice", T0);

        Assert.Equal(UserId, profile.UserId);
        Assert.Equal("Alice", profile.DisplayName);
        Assert.Equal(0, profile.GreenPointsBalance);
        Assert.Equal(0, profile.CurrentStreakDays);
        Assert.Null(profile.LastVisitDate);
        Assert.Empty(profile.Transactions);
        Assert.Empty(profile.Classifications);
    }

    [Fact]
    public void Create_TrimsDisplayName()
    {
        var profile = CitizenProfile.Create(UserId, "   Alice   ", T0);

        Assert.Equal("Alice", profile.DisplayName);
    }

    [Fact]
    public void Create_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CitizenProfile.Create(Guid.Empty, "Alice", T0));
    }

    [Fact]
    public void Create_EmptyDisplayName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CitizenProfile.Create(UserId, "", T0));
    }

    [Fact]
    public void Create_WhitespaceDisplayName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            CitizenProfile.Create(UserId, "   ", T0));
    }

    [Fact]
    public void Create_DisplayNameOver120Chars_Throws()
    {
        var tooLong = new string('A', 121);

        Assert.Throws<ArgumentException>(() =>
            CitizenProfile.Create(UserId, tooLong, T0));
    }

    // -------------------------------------------------------------
    // SetHomeLocation
    // -------------------------------------------------------------

    [Fact]
    public void SetHomeLocation_ValidValues_Persists()
    {
        var profile = NewProfile();

        profile.SetHomeLocation("123 Main St", 12.9716, 77.5946, T0);

        Assert.Equal("123 Main St", profile.HomeAddress);
        Assert.Equal(12.9716, profile.HomeLatitude);
        Assert.Equal(77.5946, profile.HomeLongitude);
    }

    [Fact]
    public void SetHomeLocation_LatitudeOutOfRange_Throws()
    {
        var profile = NewProfile();

        Assert.Throws<ArgumentException>(() =>
            profile.SetHomeLocation(null, 91.0, 0.0, T0));
        Assert.Throws<ArgumentException>(() =>
            profile.SetHomeLocation(null, -91.0, 0.0, T0));
    }

    [Fact]
    public void SetHomeLocation_LongitudeOutOfRange_Throws()
    {
        var profile = NewProfile();

        Assert.Throws<ArgumentException>(() =>
            profile.SetHomeLocation(null, 0.0, 181.0, T0));
        Assert.Throws<ArgumentException>(() =>
            profile.SetHomeLocation(null, 0.0, -181.0, T0));
    }

    // -------------------------------------------------------------
    // RecordStationVisit
    // -------------------------------------------------------------

    [Fact]
    public void RecordStationVisit_AwardsPoints()
    {
        var profile = NewProfile();
        var stationId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(T0.Date);

        profile.RecordStationVisit(stationId, date, 50, T0);

        Assert.Equal(50, profile.GreenPointsBalance);
        Assert.Single(profile.Transactions);
    }

    [Fact]
    public void RecordStationVisit_SameStationSameDay_Throws()
    {
        var profile = NewProfile();
        var stationId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(T0.Date);

        profile.RecordStationVisit(stationId, date, 50, T0);

        Assert.Throws<InvalidOperationException>(() =>
            profile.RecordStationVisit(stationId, date, 50, T0.AddMinutes(1)));
    }

    [Fact]
    public void RecordStationVisit_DifferentStationSameDay_Allowed()
    {
        var profile = NewProfile();
        var date = DateOnly.FromDateTime(T0.Date);

        profile.RecordStationVisit(Guid.NewGuid(), date, 10, T0);
        profile.RecordStationVisit(Guid.NewGuid(), date, 20, T0.AddMinutes(1));

        Assert.Equal(30, profile.GreenPointsBalance);
        Assert.Equal(2, profile.Transactions.Count);
    }

    [Fact]
    public void RecordStationVisit_SameStationDifferentDay_Allowed()
    {
        var profile = NewProfile();
        var stationId = Guid.NewGuid();
        var day1 = DateOnly.FromDateTime(T0.Date);
        var day2 = day1.AddDays(1);

        profile.RecordStationVisit(stationId, day1, 10, T0);
        profile.RecordStationVisit(stationId, day2, 20, T0.AddDays(1));

        Assert.Equal(30, profile.GreenPointsBalance);
    }

    // -------------------------------------------------------------
    // Streak transitions
    // -------------------------------------------------------------

    [Fact]
    public void Streak_FirstVisit_SetsToOne()
    {
        var profile = NewProfile();
        var day1 = DateOnly.FromDateTime(T0.Date);

        profile.RecordStationVisit(Guid.NewGuid(), day1, 10, T0);

        Assert.Equal(1, profile.CurrentStreakDays);
        Assert.Equal(day1, profile.LastVisitDate);
    }

    [Fact]
    public void Streak_ConsecutiveDayVisit_Increments()
    {
        var profile = NewProfile();
        var day1 = DateOnly.FromDateTime(T0.Date);
        var day2 = day1.AddDays(1);

        profile.RecordStationVisit(Guid.NewGuid(), day1, 10, T0);
        profile.RecordStationVisit(Guid.NewGuid(), day2, 10, T0.AddDays(1));

        Assert.Equal(2, profile.CurrentStreakDays);
        Assert.Equal(day2, profile.LastVisitDate);
    }

    [Fact]
    public void Streak_GapMoreThanOneDay_Resets()
    {
        var profile = NewProfile();
        var day1 = DateOnly.FromDateTime(T0.Date);
        var day5 = day1.AddDays(4);

        profile.RecordStationVisit(Guid.NewGuid(), day1, 10, T0);
        profile.RecordStationVisit(Guid.NewGuid(), day5, 10, T0.AddDays(4));

        Assert.Equal(1, profile.CurrentStreakDays);
    }

    // -------------------------------------------------------------
    // AwardReportPoints
    // -------------------------------------------------------------

    [Fact]
    public void AwardReportPoints_PositiveAmount_AwardsPoints()
    {
        var profile = NewProfile();

        profile.AwardReportPoints(Guid.NewGuid(), 25, T0);

        Assert.Equal(25, profile.GreenPointsBalance);
        Assert.Single(profile.Transactions);
    }

    [Fact]
    public void AwardReportPoints_ZeroAmount_Throws()
    {
        var profile = NewProfile();

        Assert.Throws<ArgumentException>(() =>
            profile.AwardReportPoints(Guid.NewGuid(), 0, T0));
    }

    [Fact]
    public void AwardReportPoints_NegativeAmount_Throws()
    {
        var profile = NewProfile();

        Assert.Throws<ArgumentException>(() =>
            profile.AwardReportPoints(Guid.NewGuid(), -10, T0));
    }

    // -------------------------------------------------------------
    // RedeemReward
    // -------------------------------------------------------------

    [Fact]
    public void RedeemReward_SufficientBalance_DeductsPoints()
    {
        var profile = NewProfile();
        profile.AwardReportPoints(Guid.NewGuid(), 100, T0);
        var reward = NewReward(costInPoints: 40);

        profile.RedeemReward(reward, T0.AddMinutes(1));

        Assert.Equal(60, profile.GreenPointsBalance);
        Assert.Equal(2, profile.Transactions.Count);
    }

    [Fact]
    public void RedeemReward_InsufficientBalance_Throws()
    {
        var profile = NewProfile();
        profile.AwardReportPoints(Guid.NewGuid(), 10, T0);
        var reward = NewReward(costInPoints: 40);

        Assert.Throws<InvalidOperationException>(() =>
            profile.RedeemReward(reward, T0.AddMinutes(1)));
    }

    [Fact]
    public void RedeemReward_InactiveReward_Throws()
    {
        var profile = NewProfile();
        profile.AwardReportPoints(Guid.NewGuid(), 100, T0);
        var inactiveReward = NewReward(costInPoints: 40, active: false);

        Assert.Throws<InvalidOperationException>(() =>
            profile.RedeemReward(inactiveReward, T0.AddMinutes(1)));
    }

    [Fact]
    public void RedeemReward_NullReward_Throws()
    {
        var profile = NewProfile();

        Assert.Throws<ArgumentNullException>(() =>
            profile.RedeemReward(null!, T0));
    }

    [Fact]
    public void RedeemReward_ExactBalance_Succeeds()
    {
        var profile = NewProfile();
        profile.AwardReportPoints(Guid.NewGuid(), 50, T0);
        var reward = NewReward(costInPoints: 50);

        profile.RedeemReward(reward, T0.AddMinutes(1));

        Assert.Equal(0, profile.GreenPointsBalance);
    }

    // -------------------------------------------------------------
    // CanVisitStationOn
    // -------------------------------------------------------------

    [Fact]
    public void CanVisitStationOn_NoVisitsYet_ReturnsTrue()
    {
        var profile = NewProfile();
        var date = DateOnly.FromDateTime(T0.Date);

        Assert.True(profile.CanVisitStationOn(Guid.NewGuid(), date));
    }

    [Fact]
    public void CanVisitStationOn_AfterVisitSameDay_ReturnsFalse()
    {
        var profile = NewProfile();
        var stationId = Guid.NewGuid();
        var date = DateOnly.FromDateTime(T0.Date);

        profile.RecordStationVisit(stationId, date, 10, T0);

        Assert.False(profile.CanVisitStationOn(stationId, date));
    }

    [Fact]
    public void CanVisitStationOn_AfterVisitDifferentDay_ReturnsTrue()
    {
        var profile = NewProfile();
        var stationId = Guid.NewGuid();
        var day1 = DateOnly.FromDateTime(T0.Date);

        profile.RecordStationVisit(stationId, day1, 10, T0);

        Assert.True(profile.CanVisitStationOn(stationId, day1.AddDays(1)));
    }
}

