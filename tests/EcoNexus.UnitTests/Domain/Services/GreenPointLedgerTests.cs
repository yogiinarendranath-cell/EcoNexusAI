using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Services;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Services;

/// <summary>
/// Unit tests for <see cref="GreenPointLedger"/> — the pure domain service
/// that aggregates a citizen's green-points ledger.
/// </summary>
public sealed class GreenPointLedgerTests
{
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly DateTimeOffset T0 = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private static CitizenProfile NewProfile()
        => CitizenProfile.Create(UserId, "Test Citizen", T0);

    private static CitizenProfile ProfileWithEarn(int points, int dayOffset = 0)
    {
        var p = NewProfile();
        p.RecordStationVisit(
            stationId: Guid.NewGuid(),
            date: DateOnly.FromDateTime(T0.DateTime.AddDays(dayOffset)),
            points: points,
            occurredAt: T0.AddDays(dayOffset));
        return p;
    }

    private static CitizenProfile ProfileWithMultipleEarns(params int[] amounts)
    {
        var p = NewProfile();
        for (var i = 0; i < amounts.Length; i++)
        {
            p.RecordStationVisit(
                stationId: Guid.NewGuid(),
                date: DateOnly.FromDateTime(T0.DateTime.AddDays(i)),
                points: amounts[i],
                occurredAt: T0.AddDays(i));
        }
        return p;
    }

    // -------------------------------------------------------------
    // Balance
    // -------------------------------------------------------------

    [Fact]
    public void Balance_OnFreshProfile_IsZero()
    {
        var profile = NewProfile();

        var balance = GreenPointLedger.Balance(profile);

        Assert.Equal(0, balance);
    }

    [Fact]
    public void Balance_AfterSingleEarn_EqualsEarnedAmount()
    {
        var profile = ProfileWithEarn(50);

        var balance = GreenPointLedger.Balance(profile);

        Assert.Equal(50, balance);
    }

    [Fact]
    public void Balance_AfterMultipleEarns_EqualsSum()
    {
        var profile = ProfileWithMultipleEarns(10, 20, 30, 40);

        var balance = GreenPointLedger.Balance(profile);

        Assert.Equal(100, balance);
    }

    [Fact]
    public void Balance_NullProfile_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GreenPointLedger.Balance(null!));
    }

    // -------------------------------------------------------------
    // TotalEarned / TotalRedeemed
    // -------------------------------------------------------------

    [Fact]
    public void TotalEarned_OnFreshProfile_IsZero()
    {
        var profile = NewProfile();

        var earned = GreenPointLedger.TotalEarned(profile);

        Assert.Equal(0, earned);
    }

    [Fact]
    public void TotalEarned_SumsPositiveDeltas()
    {
        var profile = ProfileWithMultipleEarns(10, 20, 30);

        var earned = GreenPointLedger.TotalEarned(profile);

        Assert.Equal(60, earned);
    }

    [Fact]
    public void TotalRedeemed_OnFreshProfile_IsZero()
    {
        var profile = NewProfile();

        var redeemed = GreenPointLedger.TotalRedeemed(profile);

        Assert.Equal(0, redeemed);
    }

    [Fact]
    public void TotalRedeemed_ReturnsPositiveNumber_ForNegativeDeltas()
    {
        var profile = NewProfile();
        profile.AwardReportPoints(Guid.NewGuid(), 100, T0);

        var reward = Reward.Create(
            name: "Test Reward",
            description: "Test",
            costInPoints: 30,
            createdAt: T0);

        profile.RedeemReward(reward, T0.AddMinutes(1));

        var redeemed = GreenPointLedger.TotalRedeemed(profile);

        Assert.Equal(30, redeemed);
    }

    // -------------------------------------------------------------
    // TransactionCount
    // -------------------------------------------------------------

    [Fact]
    public void TransactionCount_OnFreshProfile_IsZero()
    {
        var profile = NewProfile();

        var count = GreenPointLedger.TransactionCount(profile);

        Assert.Equal(0, count);
    }

    [Fact]
    public void TransactionCount_EqualsNumberOfRecordedEntries()
    {
        var profile = ProfileWithMultipleEarns(5, 5, 5);

        var count = GreenPointLedger.TransactionCount(profile);

        Assert.Equal(3, count);
    }

    // -------------------------------------------------------------
    // Invariant: Balance = TotalEarned - TotalRedeemed
    // -------------------------------------------------------------

    [Fact]
    public void Balance_EqualsTotalEarnedMinusTotalRedeemed()
    {
        var profile = NewProfile();
        profile.AwardReportPoints(Guid.NewGuid(), 100, T0);
        profile.AwardReportPoints(Guid.NewGuid(), 50, T0.AddMinutes(1));

        var reward = Reward.Create(
            name: "Reward",
            description: "Test",
            costInPoints: 40,
            createdAt: T0);
        profile.RedeemReward(reward, T0.AddMinutes(2));

        var balance  = GreenPointLedger.Balance(profile);
        var earned   = GreenPointLedger.TotalEarned(profile);
        var redeemed = GreenPointLedger.TotalRedeemed(profile);

        Assert.Equal(110, balance);
        Assert.Equal(150, earned);
        Assert.Equal(40, redeemed);
        Assert.Equal(earned - redeemed, balance);
    }

    [Fact]
    public void TotalEarned_NullProfile_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GreenPointLedger.TotalEarned(null!));
    }

    [Fact]
    public void TotalRedeemed_NullProfile_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GreenPointLedger.TotalRedeemed(null!));
    }

    [Fact]
    public void TransactionCount_NullProfile_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => GreenPointLedger.TransactionCount(null!));
    }
}

