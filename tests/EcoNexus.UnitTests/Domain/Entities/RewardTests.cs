using EcoNexus.Domain.Entities;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

/// <summary>
/// Unit tests for <see cref="Reward"/> — the citizen-redeemable reward
/// aggregate root.
/// </summary>
public sealed class RewardTests
{
    private static readonly DateTimeOffset T0 = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_ValidInputs_SetsInitialState()
    {
        var reward = Reward.Create("Free Coffee", "A voucher for one coffee", 50, T0);

        Assert.Equal("Free Coffee", reward.Name);
        Assert.Equal("A voucher for one coffee", reward.Description);
        Assert.Equal(50, reward.CostInPoints);
        Assert.True(reward.IsActive);
        Assert.Equal(T0, reward.CreatedAt);
    }

    [Fact]
    public void Create_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reward.Create("", "desc", 50, T0));
    }

    [Fact]
    public void Create_WhitespaceName_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reward.Create("   ", "desc", 50, T0));
    }

    [Fact]
    public void Create_EmptyDescription_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reward.Create("Reward", "", 50, T0));
    }

    [Fact]
    public void Create_ZeroCostInPoints_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reward.Create("Reward", "desc", 0, T0));
    }

    [Fact]
    public void Create_NegativeCostInPoints_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            Reward.Create("Reward", "desc", -10, T0));
    }

    [Fact]
    public void Deactivate_SetsInactive()
    {
        var reward = Reward.Create("Reward", "desc", 50, T0);

        reward.Deactivate();

        Assert.False(reward.IsActive);
    }

    [Fact]
    public void Deactivate_WhenAlreadyInactive_Throws()
    {
        var reward = Reward.Create("Reward", "desc", 50, T0);
        reward.Deactivate();

        Assert.Throws<InvalidOperationException>(() => reward.Deactivate());
    }

    [Fact]
    public void Reactivate_SetsActive()
    {
        var reward = Reward.Create("Reward", "desc", 50, T0);
        reward.Deactivate();

        reward.Reactivate();

        Assert.True(reward.IsActive);
    }

    [Fact]
    public void Reactivate_WhenAlreadyActive_Throws()
    {
        var reward = Reward.Create("Reward", "desc", 50, T0);

        Assert.Throws<InvalidOperationException>(() => reward.Reactivate());
    }
}

