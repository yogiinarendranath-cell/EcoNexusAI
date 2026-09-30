using EcoNexus.Application.Abstractions.Identity;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Citizen.GetProfile;
using EcoNexus.Domain.Entities;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class GetCitizenProfileHandlerTests
{
    private readonly ICitizenProfileRepository _repository =
        Substitute.For<ICitizenProfileRepository>();

    private readonly IUserDirectory _userDirectory =
        Substitute.For<IUserDirectory>();

    private readonly GetCitizenProfileHandler _handler;

    public GetCitizenProfileHandlerTests()
    {
        _handler = new GetCitizenProfileHandler(_repository, _userDirectory);
    }

    [Fact]
    public async Task Handle_ProfileExists_ReturnsMappedResponse()
    {
        var userId = Guid.NewGuid();
        var profile = CitizenProfile.Create(userId, "Ada Lovelace", DateTimeOffset.UtcNow);

        _repository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        var response = await _handler.Handle(
            new GetCitizenProfileQuery(userId, FallbackDisplayName: "Anonymous"),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(profile.Id,               response.Id);
        Assert.Equal(userId,                   response.UserId);
        Assert.Equal("Ada Lovelace",           response.DisplayName);
        Assert.Null(response.HomeAddress);
        Assert.Null(response.HomeLatitude);
        Assert.Null(response.HomeLongitude);
        Assert.Equal(0,                        response.GreenPointsBalance);
        Assert.Equal(0,                        response.CurrentStreakDays);
        Assert.Null(response.LastVisitDate);
        Assert.Equal(0,                        response.TotalTransactions);
        Assert.Equal(0,                        response.TotalEarned);
        Assert.Equal(0,                        response.TotalRedeemed);
        Assert.Equal(profile.CreatedAt,        response.CreatedAt);
        Assert.Equal(profile.LastUpdatedAt,    response.LastUpdatedAt);

        await _userDirectory
            .DidNotReceive()
            .GetDisplayNameAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .AddAsync(Arg.Any<CitizenProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProfileDoesNotExist_DirectoryHasName_CreatesProfileWithDirectoryName()
    {
        var userId = Guid.NewGuid();

        _repository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((CitizenProfile?)null);

        _userDirectory
            .GetDisplayNameAsync(userId, Arg.Any<CancellationToken>())
            .Returns("Grace Hopper");

        var response = await _handler.Handle(
            new GetCitizenProfileQuery(userId, FallbackDisplayName: "Anonymous"),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(userId,           response.UserId);
        Assert.Equal("Grace Hopper",   response.DisplayName);

        await _repository
            .Received(1)
            .AddAsync(
                Arg.Is<CitizenProfile>(p => p.UserId == userId && p.DisplayName == "Grace Hopper"),
                Arg.Any<CancellationToken>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProfileDoesNotExist_DirectoryReturnsNull_UsesFallbackDisplayName()
    {
        var userId = Guid.NewGuid();

        _repository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((CitizenProfile?)null);

        _userDirectory
            .GetDisplayNameAsync(userId, Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var response = await _handler.Handle(
            new GetCitizenProfileQuery(userId, FallbackDisplayName: "Anonymous Citizen"),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(userId,                response.UserId);
        Assert.Equal("Anonymous Citizen",   response.DisplayName);

        await _repository
            .Received(1)
            .AddAsync(
                Arg.Is<CitizenProfile>(p => p.UserId == userId && p.DisplayName == "Anonymous Citizen"),
                Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_QueriesRepositoryWithExactUserId()
    {
        var userId = Guid.NewGuid();
        var profile = CitizenProfile.Create(userId, "Ada Lovelace", DateTimeOffset.UtcNow);

        _repository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns(profile);

        await _handler.Handle(
            new GetCitizenProfileQuery(userId, FallbackDisplayName: "Anonymous"),
            CancellationToken.None);

        await _repository
            .Received(1)
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ProvisioningPath_DoesNotCallSaveChanges()
    {
        var userId = Guid.NewGuid();

        _repository
            .GetByUserIdAsync(userId, Arg.Any<CancellationToken>())
            .Returns((CitizenProfile?)null);

        _userDirectory
            .GetDisplayNameAsync(userId, Arg.Any<CancellationToken>())
            .Returns("Grace Hopper");

        await _handler.Handle(
            new GetCitizenProfileQuery(userId, FallbackDisplayName: "Anonymous"),
            CancellationToken.None);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}