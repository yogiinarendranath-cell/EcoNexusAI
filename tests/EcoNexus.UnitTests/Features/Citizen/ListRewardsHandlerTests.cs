using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Citizen.ListRewards;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class ListRewardsHandlerTests
{
    private readonly IRewardRepository _repository =
        Substitute.For<IRewardRepository>();

    private readonly ListRewardsHandler _handler;

    public ListRewardsHandlerTests()
    {
        _handler = new ListRewardsHandler(_repository);
    }

    private static Reward NewReward(
        string name = "Free Coffee",
        int cost = 50,
        bool active = true)
    {
        var reward = Reward.Create(
            name,
            "A voucher for one free coffee at partner cafes.",
            cost,
            DateTimeOffset.UtcNow);

        if (!active)
        {
            reward.Deactivate();
        }

        return reward;
    }

    [Fact]
    public async Task Handle_MultipleRewards_ReturnsAllMappedResponses()
    {
        var r1 = NewReward("Free Coffee", 50);
        var r2 = NewReward("Tree Planting", 200);

        _repository
            .GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { r1, r2 });

        var response = await _handler.Handle(
            new ListRewardsQuery(),
            CancellationToken.None);

        Assert.Equal(2, response.Count);

        Assert.Equal(r1.Id,           response[0].Id);
        Assert.Equal("Free Coffee",   response[0].Name);
        Assert.Equal(50,              response[0].CostInPoints);
        Assert.True(response[0].IsActive);
        Assert.Equal(r1.CreatedAt,    response[0].CreatedAt);

        Assert.Equal(r2.Id,           response[1].Id);
        Assert.Equal("Tree Planting", response[1].Name);
        Assert.Equal(200,             response[1].CostInPoints);
        Assert.True(response[1].IsActive);
    }

    [Fact]
    public async Task Handle_DescriptionIsMapped()
    {
        var reward = NewReward();
        var expectedDescription = reward.Description;

        _repository
            .GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { reward });

        var response = await _handler.Handle(
            new ListRewardsQuery(),
            CancellationToken.None);

        Assert.Single(response);
        Assert.Equal(expectedDescription, response[0].Description);
    }

    [Fact]
    public async Task Handle_NoActiveRewards_ReturnsEmptyList()
    {
        _repository
            .GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Reward>());

        var response = await _handler.Handle(
            new ListRewardsQuery(),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce()
    {
        _repository
            .GetActiveAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Reward>());

        await _handler.Handle(new ListRewardsQuery(), CancellationToken.None);

        await _repository
            .Received(1)
            .GetActiveAsync(Arg.Any<CancellationToken>());
    }
}