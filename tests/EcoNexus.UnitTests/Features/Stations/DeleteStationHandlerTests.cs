using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Stations.DeleteStation;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class DeleteStationHandlerTests
{
    private readonly IWasteStationRepository _repository =
        Substitute.For<IWasteStationRepository>();

    private readonly DeleteStationHandler _handler;

    public DeleteStationHandlerTests()
    {
        _handler = new DeleteStationHandler(_repository);
    }

    private static WasteStation NewStation()
    {
        return WasteStation.Create(
            StationCode.Create("ST-0001"),
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);
    }

    [Fact]
    public async Task Handle_StationExists_RemovesStationAndSavesChanges()
    {
        var station = NewStation();
        var stationId = station.Id;

        _repository
            .GetByIdAsync(stationId, Arg.Any<CancellationToken>())
            .Returns(station);

        await _handler.Handle(
            new DeleteStationCommand(stationId),
            CancellationToken.None);

        _repository
            .Received(1)
            .Remove(station);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StationDoesNotExist_ReturnsSilentlyWithoutSaving()
    {
        var stationId = Guid.NewGuid();

        _repository
            .GetByIdAsync(stationId, Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        await _handler.Handle(
            new DeleteStationCommand(stationId),
            CancellationToken.None);

        _repository
            .DidNotReceive()
            .Remove(Arg.Any<WasteStation>());

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_StationDoesNotExist_DoesNotThrow()
    {
        var stationId = Guid.NewGuid();

        _repository
            .GetByIdAsync(stationId, Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        var exception = await Record.ExceptionAsync(() =>
            _handler.Handle(
                new DeleteStationCommand(stationId),
                CancellationToken.None));

        Assert.Null(exception);
    }

    [Fact]
    public async Task Handle_SaveChangesThrowsConcurrencyConflict_ExceptionPropagates()
    {
        var station = NewStation();
        var stationId = station.Id;

        _repository
            .GetByIdAsync(stationId, Arg.Any<CancellationToken>())
            .Returns(station);

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyConflictException(
                "The station was modified by another user."));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            _handler.Handle(
                new DeleteStationCommand(stationId),
                CancellationToken.None));

        _repository
            .Received(1)
            .Remove(station);
    }
}