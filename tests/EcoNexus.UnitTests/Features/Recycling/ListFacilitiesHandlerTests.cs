using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Features.Recycling.ListFacilities;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Recycling;

public sealed class ListFacilitiesHandlerTests
{
    private readonly IRecyclingFacilityRepository _repository =
        Substitute.For<IRecyclingFacilityRepository>();

    private readonly ListFacilitiesHandler _handler;

    public ListFacilitiesHandlerTests()
    {
        _handler = new ListFacilitiesHandler(_repository);
    }

    private static RecyclingFacility NewFacility(string name)
        => RecyclingFacility.Create(
            name,
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(10_000));

    [Fact]
    public async Task Handle_MultipleFacilities_ReturnsMappedListWithMetrics()
    {
        var facility = NewFacility("Alpha Recycling");

        facility.RecordIntake(
            WasteCategory.Plastic,
            Weight.FromKilograms(500),
            DateTimeOffset.UtcNow);

        var recovered = facility.RecordIntake(
            WasteCategory.Glass,
            Weight.FromKilograms(300),
            DateTimeOffset.UtcNow);
        facility.AdvanceIntake(
            recovered.Id,
            IntakeStage.Recovered,
            DateTimeOffset.UtcNow);

        _repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { facility });

        var response = await _handler.Handle(
            new ListFacilitiesQuery(),
            CancellationToken.None);

        Assert.Single(response);

        var item = response[0];
        Assert.Equal(facility.Id,               item.Id);
        Assert.Equal("Alpha Recycling",         item.Name);
        Assert.Equal(12.9716,                   item.Latitude);
        Assert.Equal(77.5946,                   item.Longitude);
        Assert.Equal(10_000,                    item.DailyCapacityKilograms);
        Assert.Equal("Online",                  item.Status);
        Assert.Equal(2,                         item.IntakeCount);

        Assert.Equal(2,      item.Metrics.TotalBatches);
        Assert.Equal(1,      item.Metrics.RecoveredBatches);
        Assert.Equal(800,    item.Metrics.ReceivedKilograms);
        Assert.Equal(300,    item.Metrics.RecoveredKilograms);
        Assert.Equal(0,      item.Metrics.LandfilledKilograms);
        Assert.Equal(0.375,  item.Metrics.RecyclingRate,        3);
        Assert.Equal(1.0,    item.Metrics.LandfillDiversion,    3);
        Assert.Equal(450,    item.Metrics.Co2SavedKilograms,    3);
    }

    [Fact]
    public async Task Handle_FacilityWithNoIntakes_ReturnsZeroMetrics()
    {
        var facility = NewFacility("Empty Facility");

        _repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(new[] { facility });

        var response = await _handler.Handle(
            new ListFacilitiesQuery(),
            CancellationToken.None);

        Assert.Single(response);
        Assert.Equal(0, response[0].IntakeCount);
        Assert.Equal(0, response[0].Metrics.TotalBatches);
        Assert.Equal(0, response[0].Metrics.RecoveredBatches);
        Assert.Equal(0, response[0].Metrics.ReceivedKilograms);
        Assert.Equal(0, response[0].Metrics.RecoveredKilograms);
        Assert.Equal(0, response[0].Metrics.LandfilledKilograms);
        Assert.Equal(0, response[0].Metrics.RecyclingRate);
        Assert.Equal(0, response[0].Metrics.LandfillDiversion);
        Assert.Equal(0, response[0].Metrics.Co2SavedKilograms);
    }

    [Fact]
    public async Task Handle_NoFacilities_ReturnsEmptyList()
    {
        _repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RecyclingFacility>());

        var response = await _handler.Handle(
            new ListFacilitiesQuery(),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response);
    }

    [Fact]
    public async Task Handle_CallsRepositoryExactlyOnce()
    {
        _repository
            .GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<RecyclingFacility>());

        await _handler.Handle(new ListFacilitiesQuery(), CancellationToken.None);

        await _repository
            .Received(1)
            .GetAllAsync(Arg.Any<CancellationToken>());
    }
}