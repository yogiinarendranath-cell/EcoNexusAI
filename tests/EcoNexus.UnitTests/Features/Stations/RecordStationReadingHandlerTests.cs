using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Stations.RecordStationReading;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Stations;

public sealed class RecordStationReadingHandlerTests
{
    private readonly IWasteStationRepository _repository = Substitute.For<IWasteStationRepository>();
    private readonly RecordStationReadingHandler _handler;

    public RecordStationReadingHandlerTests()
    {
        _handler = new RecordStationReadingHandler(_repository);
    }

    private static WasteStation MakeStation()
    {
        return WasteStation.Create(
            StationCode.Create("ST-4001"),
            Location.Create(12.97, 77.59),
            Weight.FromKilograms(500),
            WasteCategory.Plastic);
    }

    [Fact]
    public async Task Handle_StationNotFound_ThrowsInvalidOperation()
    {
        _repository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((WasteStation?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new RecordStationReadingCommand(
                    Guid.NewGuid(),
                    new RecordReadingRequest(50, 25, 80, DateTimeOffset.UtcNow)),
                CancellationToken.None));

        Assert.Contains("was not found", ex.Message);
    }

    [Fact]
    public async Task Handle_ValidReading_UpdatesStationAndPersists()
    {
        var station = MakeStation();
        _repository.GetByIdAsync(station.Id, Arg.Any<CancellationToken>()).Returns(station);

        var response = await _handler.Handle(
            new RecordStationReadingCommand(
                station.Id,
                new RecordReadingRequest(60, 25, 80, DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.Equal(60, response.NewFillLevelPercent);
        Assert.False(response.IsCritical);
        Assert.False(response.CriticalTransitionOccurred);
        Assert.Equal(station.Id, response.StationId);

        await _repository.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CrossingCriticalThreshold_FlagsTransition()
    {
        var station = MakeStation();

        // First reading: below critical
        station.RecordReading(FillLevel.FromPercent(50), 25, 80, DateTimeOffset.UtcNow);
        _repository.GetByIdAsync(station.Id, Arg.Any<CancellationToken>()).Returns(station);

        var response = await _handler.Handle(
            new RecordStationReadingCommand(
                station.Id,
                new RecordReadingRequest(95, 25, 80, DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.True(response.IsCritical);
        Assert.True(response.CriticalTransitionOccurred);
        Assert.Equal(95, response.NewFillLevelPercent);
    }

    [Fact]
    public async Task Handle_AlreadyCritical_NoTransition()
    {
        var station = MakeStation();
        station.RecordReading(FillLevel.FromPercent(92), 25, 80, DateTimeOffset.UtcNow);
        _repository.GetByIdAsync(station.Id, Arg.Any<CancellationToken>()).Returns(station);

        var response = await _handler.Handle(
            new RecordStationReadingCommand(
                station.Id,
                new RecordReadingRequest(96, 25, 80, DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.True(response.IsCritical);
        Assert.False(response.CriticalTransitionOccurred);
    }

    [Fact]
    public async Task Handle_NonCriticalReading_NoCriticalFlag()
    {
        var station = MakeStation();
        _repository.GetByIdAsync(station.Id, Arg.Any<CancellationToken>()).Returns(station);

        var response = await _handler.Handle(
            new RecordStationReadingCommand(
                station.Id,
                new RecordReadingRequest(45, 25, 80, DateTimeOffset.UtcNow)),
            CancellationToken.None);

        Assert.False(response.IsCritical);
        Assert.False(response.CriticalTransitionOccurred);
    }
}
