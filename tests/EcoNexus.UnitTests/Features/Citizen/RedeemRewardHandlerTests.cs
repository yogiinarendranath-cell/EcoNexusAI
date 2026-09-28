using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Citizen.RedeemReward;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class RedeemRewardHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("66666666-6666-6666-6666-666666666666");
    private static readonly DateTimeOffset T0 = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly ICitizenProfileRepository _profiles = Substitute.For<ICitizenProfileRepository>();
    private readonly IRewardRepository _rewards = Substitute.For<IRewardRepository>();
    private readonly RedeemRewardHandler _handler;

    public RedeemRewardHandlerTests()
    {
        _handler = new RedeemRewardHandler(_profiles, _rewards);
    }

    // -------------------- Fixtures --------------------

    private static CitizenProfile NewProfile(int startingBalance = 0)
    {
        var profile = CitizenProfile.Create(UserId, "Test Citizen", T0);
        if (startingBalance > 0)
        {
            profile.AwardReportPoints(Guid.NewGuid(), startingBalance, T0);
        }
        return profile;
    }

    private static Reward NewReward(int cost, bool active = true)
    {
        var reward = Reward.Create("Free Coffee", "A voucher for one coffee", cost, T0);
        if (!active) reward.Deactivate();
        return reward;
    }

    private void ProfileExists(CitizenProfile profile)
        => _profiles.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
                    .Returns(profile);

    private void RewardExists(Reward reward)
        => _rewards.GetByIdAsync(reward.Id, Arg.Any<CancellationToken>())
                   .Returns(reward);

    // -------------------- Profile guard --------------------

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _profiles.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
                 .Returns((CitizenProfile?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RedeemRewardCommand(UserId, Guid.NewGuid()), CancellationToken.None));
    }

    // -------------------- Reward guard --------------------

    [Fact]
    public async Task Handle_RewardNotFound_ThrowsNotFoundException()
    {
        ProfileExists(NewProfile(startingBalance: 100));

        var missingRewardId = Guid.NewGuid();
        _rewards.GetByIdAsync(missingRewardId, Arg.Any<CancellationToken>())
                .Returns((Reward?)null);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RedeemRewardCommand(UserId, missingRewardId), CancellationToken.None));
    }

    // -------------------- Happy path --------------------

    [Fact]
    public async Task Handle_ValidRequest_DeductsPointsAndReturnsResponse()
    {
        var profile = NewProfile(startingBalance: 100);
        ProfileExists(profile);

        var reward = NewReward(cost: 40);
        RewardExists(reward);

        var response = await _handler.Handle(
            new RedeemRewardCommand(UserId, reward.Id),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(profile.Id, response.CitizenProfileId);
        Assert.Equal(reward.Id, response.RewardId);
        Assert.Equal("Free Coffee", response.RewardName);
        Assert.Equal(40, response.PointsSpent);
        Assert.Equal(60, response.NewBalance);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsChanges()
    {
        var profile = NewProfile(startingBalance: 100);
        ProfileExists(profile);

        var reward = NewReward(cost: 40);
        RewardExists(reward);

        await _handler.Handle(
            new RedeemRewardCommand(UserId, reward.Id),
            CancellationToken.None);

        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ExactBalance_Succeeds()
    {
        var profile = NewProfile(startingBalance: 50);
        ProfileExists(profile);

        var reward = NewReward(cost: 50);
        RewardExists(reward);

        var response = await _handler.Handle(
            new RedeemRewardCommand(UserId, reward.Id),
            CancellationToken.None);

        Assert.Equal(0, response.NewBalance);
    }

    // -------------------- Domain invariant failures --------------------

    [Fact]
    public async Task Handle_InsufficientBalance_ThrowsConflictException()
    {
        var profile = NewProfile(startingBalance: 10);
        ProfileExists(profile);

        var reward = NewReward(cost: 40);
        RewardExists(reward);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new RedeemRewardCommand(UserId, reward.Id), CancellationToken.None));

        await _profiles.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }

    [Fact]
    public async Task Handle_InactiveReward_ThrowsConflictException()
    {
        var profile = NewProfile(startingBalance: 100);
        ProfileExists(profile);

        var reward = NewReward(cost: 40, active: false);
        RewardExists(reward);

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new RedeemRewardCommand(UserId, reward.Id), CancellationToken.None));

        await _profiles.DidNotReceiveWithAnyArgs().SaveChangesAsync(default);
    }
}