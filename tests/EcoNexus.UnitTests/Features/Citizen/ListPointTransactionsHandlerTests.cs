using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Citizen.ListPointTransactions;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class ListPointTransactionsHandlerTests
{
    private readonly ICitizenProfileRepository _repository =
        Substitute.For<ICitizenProfileRepository>();

    private readonly ListPointTransactionsHandler _handler;

    public ListPointTransactionsHandlerTests()
    {
        _handler = new ListPointTransactionsHandler(_repository);
    }

    private static CitizenProfile NewProfile()
        => CitizenProfile.Create(Guid.NewGuid(), "Ada Lovelace", DateTimeOffset.UtcNow);

    private static void AddVisits(
        CitizenProfile profile, int count, DateTimeOffset startingAt)
    {
        for (var i = 0; i < count; i++)
        {
            profile.RecordStationVisit(
                stationId: Guid.NewGuid(),
                date: DateOnly.FromDateTime(startingAt.UtcDateTime),
                points: 10,
                occurredAt: startingAt.AddHours(i));
        }
    }

    private static ListPointTransactionsQuery Query(
        Guid userId, int page = 1, int pageSize = 20)
        => new(userId, new PointHistoryQuery { Page = page, PageSize = pageSize });

    [Fact]
    public async Task Handle_ProfileWithTransactions_ReturnsAllOnDefaultPage()
    {
        var profile = NewProfile();
        var baseTime = DateTimeOffset.UtcNow;
        AddVisits(profile, count: 3, startingAt: baseTime);

        _repository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _handler.Handle(
            Query(profile.UserId),
            CancellationToken.None);

        Assert.Equal(3, result.Items.Count);
        Assert.Equal(1, result.Page);
        Assert.Equal(20, result.PageSize);
        Assert.Equal(3, result.TotalCount);

        var transactions = profile.Transactions.OrderByDescending(t => t.OccurredAt).ToList();
        Assert.Equal(transactions[0].Id,          result.Items[0].Id);
        Assert.Equal(10,                          result.Items[0].SignedDelta);
        Assert.Equal("Earned",                    result.Items[0].Source);
        Assert.Equal("StationVisit",              result.Items[0].Reason);
        Assert.Equal(transactions[0].Description, result.Items[0].Description);
        Assert.Equal(transactions[0].OccurredAt,  result.Items[0].OccurredAt);
    }

    [Fact]
    public async Task Handle_PageBelowOne_IsNormalisedToOne()
    {
        var profile = NewProfile();
        _repository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _handler.Handle(
            Query(profile.UserId, page: 0, pageSize: 20),
            CancellationToken.None);

        Assert.Equal(1, result.Page);
    }

    [Fact]
    public async Task Handle_PageSizeBelowOne_IsNormalisedToTwenty()
    {
        var profile = NewProfile();
        _repository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _handler.Handle(
            Query(profile.UserId, page: 1, pageSize: 0),
            CancellationToken.None);

        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task Handle_PageSizeAboveOneHundred_IsNormalisedToTwenty()
    {
        var profile = NewProfile();
        _repository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _handler.Handle(
            Query(profile.UserId, page: 1, pageSize: 999),
            CancellationToken.None);

        Assert.Equal(20, result.PageSize);
    }

    [Fact]
    public async Task Handle_SecondPage_ReturnsCorrectSlice()
    {
        var profile = NewProfile();
        AddVisits(profile, count: 5, startingAt: DateTimeOffset.UtcNow);

        _repository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _handler.Handle(
            Query(profile.UserId, page: 2, pageSize: 2),
            CancellationToken.None);

        Assert.Equal(2, result.Items.Count);
        Assert.Equal(2, result.Page);
        Assert.Equal(2, result.PageSize);
        Assert.Equal(5, result.TotalCount);

        var ordered = profile.Transactions.OrderByDescending(t => t.OccurredAt).ToList();
        Assert.Equal(ordered[2].Id, result.Items[0].Id);
        Assert.Equal(ordered[3].Id, result.Items[1].Id);
    }

    [Fact]
    public async Task Handle_ProfileDoesNotExist_ThrowsNotFoundException()
    {
        var userId = Guid.NewGuid();

        _repository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((CitizenProfile?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(Query(userId), CancellationToken.None));

        Assert.Contains(userId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_EmptyLedger_ReturnsEmptyPageWithZeroTotal()
    {
        var profile = NewProfile();
        _repository
            .GetByUserIdAsync(profile.UserId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var result = await _handler.Handle(
            Query(profile.UserId),
            CancellationToken.None);

        Assert.NotNull(result);
        Assert.Empty(result.Items);
        Assert.Equal(0, result.TotalCount);
        Assert.Equal(0, result.TotalPages);
    }
}