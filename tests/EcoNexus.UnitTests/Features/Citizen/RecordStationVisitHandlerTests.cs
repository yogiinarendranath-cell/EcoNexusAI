using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Citizen.RecordStationVisit;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Citizen;

public sealed class RecordStationVisitHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("55555555-5555-5555-5555-555555555555");
    private static readonly DateTimeOffset T0 = new(2026, 9, 28, 12, 0, 0, TimeSpan.Zero);

    private readonly ICitizenProfileRepository _profiles = Substitute.For<ICitizenProfileRepository>();
    private readonly IWasteStationRepository _stations = Substitute.For<IWasteStationRepository>();
    private readonly RecordStationVisitHandler _handler;

    public RecordStationVisitHandlerTests()
    {
        _handler = new RecordStationVisitHandler(_profiles, _stations);
    }

    // -------------------- Fixtures --------------------

    private static CitizenProfile NewProfile()
        => CitizenProfile.Create(UserId, "Test Citizen", T0);

    private static WasteStation NewStation(string code = "ST-2001")
        => WasteStation.Create(
            StationCode.Create(code),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);

    private void ProfileExists(CitizenProfile profile)
        => _profiles.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
                    .Returns(profile);

    private void StationExists(Guid stationId)
    {
        var station = NewStation();
        _stations.GetByIdAsync(stationId, Arg.Any<CancellationToken>())
                 .Returns(station);
    }

    // -------------------- Profile guard --------------------

    [Fact]
    public async Task Handle_ProfileNotFound_ThrowsNotFoundException()
    {
        _profiles.GetByUserIdAsync(UserId, Arg.Any<CancellationToken>())
                 .Returns((CitizenProfile?)null);

        var request = new RecordStationVisitRequest(Guid.NewGuid(), new DateOnly(2026, 9, 28));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RecordStationVisitCommand(UserId, request), CancellationToken.None));
    }

    // -------------------- Station guard --------------------

    [Fact]
    public async Task Handle_StationNotFound_ThrowsNotFoundException()
    {
        ProfileExists(NewProfile());

        var missingStationId = Guid.NewGuid();
        _stations.GetByIdAsync(missingStationId, Arg.Any<CancellationToken>())
                 .Returns((WasteStation?)null);

        var request = new RecordStationVisitRequest(missingStationId, new DateOnly(2026, 9, 28));

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(new RecordStationVisitCommand(UserId, request), CancellationToken.None));
    }

    // -------------------- Happy path --------------------

    [Fact]
    public async Task Handle_ValidRequest_AwardsPointsAndReturnsResponse()
    {
        var profile = NewProfile();
        ProfileExists(profile);

        var stationId = Guid.NewGuid();
        StationExists(stationId);

        var date = new DateOnly(2026, 9, 28);
        var request = new RecordStationVisitRequest(stationId, date);

        var response = await _handler.Handle(
            new RecordStationVisitCommand(UserId, request),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(profile.Id, response.CitizenProfileId);
        Assert.Equal(stationId, response.StationId);
        Assert.Equal(10, response.PointsAwarded);
        Assert.Equal(10, response.NewBalance);
        Assert.Equal(1, response.CurrentStreakDays);
        Assert.NotEqual(Guid.Empty, response.TransactionId);
    }

    [Fact]
    public async Task Handle_ValidRequest_PersistsChanges()
    {
        var profile = NewProfile();
        ProfileExists(profile);

        var stationId = Guid.NewGuid();
        StationExists(stationId);

        var request = new RecordStationVisitRequest(stationId, new DateOnly(2026, 9, 28));

        await _handler.Handle(
            new RecordStationVisitCommand(UserId, request),
            CancellationToken.None);

        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ValidRequest_AddsTransactionToProfile()
    {
        var profile = NewProfile();
        ProfileExists(profile);

        var stationId = Guid.NewGuid();
        StationExists(stationId);

        var request = new RecordStationVisitRequest(stationId, new DateOnly(2026, 9, 28));

        await _handler.Handle(
            new RecordStationVisitCommand(UserId, request),
            CancellationToken.None);

        Assert.Single(profile.Transactions);
        Assert.Equal(10, profile.Transactions.First().SignedDelta);
    }

    // -------------------- Duplicate visit --------------------

    [Fact]
    public async Task Handle_DuplicateVisitSameStationSameDay_ThrowsConflictException()
    {
        var profile = NewProfile();
        ProfileExists(profile);

        var stationId = Guid.NewGuid();
        StationExists(stationId);

        var date = new DateOnly(2026, 9, 28);
        var request = new RecordStationVisitRequest(stationId, date);

        // First visit succeeds.
        await _handler.Handle(
            new RecordStationVisitCommand(UserId, request),
            CancellationToken.None);

        // Second visit on the same day â†’ conflict.
        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new RecordStationVisitCommand(UserId, request), CancellationToken.None));

        await _profiles.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    // -------------------- Different day / different station --------------------

    [Fact]
    public async Task Handle_SecondVisitSameStationDifferentDay_Succeeds()
    {
        var profile = NewProfile();
        ProfileExists(profile);

        var stationId = Guid.NewGuid();
        StationExists(stationId);

        await _handler.Handle(
            new RecordStationVisitCommand(UserId, new RecordStationVisitRequest(stationId, new DateOnly(2026, 9, 28))),
            CancellationToken.None);

        await _handler.Handle(
            new RecordStationVisitCommand(UserId, new RecordStationVisitRequest(stationId, new DateOnly(2026, 9, 29))),
            CancellationToken.None);

        Assert.Equal(20, profile.GreenPointsBalance);
        Assert.Equal(2, profile.CurrentStreakDays);
    }
}