using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Recycling.GetFacilityById;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Recycling;

public sealed class GetFacilityByIdHandlerTests
{
    private readonly IRecyclingFacilityRepository _repository =
        Substitute.For<IRecyclingFacilityRepository>();

    private readonly GetFacilityByIdHandler _handler;

    public GetFacilityByIdHandlerTests()
    {
        _handler = new GetFacilityByIdHandler(_repository);
    }

    private static RecyclingFacility NewFacility(string name = "Test Facility")
        => RecyclingFacility.Create(
            name,
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(10_000));

    [Fact]
    public async Task Handle_FacilityExists_ReturnsFullDetailWithMetricsAndIntakes()
    {
        var facility = NewFacility("Alpha Recycling");
        var now = DateTimeOffset.UtcNow;

        facility.RecordIntake(
            WasteCategory.Plastic,
            Weight.FromKilograms(500),
            now);

        var recovered = facility.RecordIntake(
            WasteCategory.Glass,
            Weight.FromKilograms(300),
            now.AddMinutes(1));
        facility.AdvanceIntake(recovered.Id, IntakeStage.Recovered, now.AddMinutes(2));

        _repository
            .GetByIdAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(facility);

        var response = await _handler.Handle(
            new GetFacilityByIdQuery(facility.Id),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(facility.Id,               response.Id);
        Assert.Equal("Alpha Recycling",         response.Name);
        Assert.Equal(12.9716,                   response.Latitude);
        Assert.Equal(77.5946,                   response.Longitude);
        Assert.Equal(10_000,                    response.DailyCapacityKilograms);
        Assert.Equal("Online",                  response.Status);
        Assert.Equal(facility.CreatedAt,        response.CreatedAt);
        Assert.Equal(facility.LastUpdatedAt,    response.LastUpdatedAt);

        Assert.Equal(2,      response.Metrics.TotalBatches);
        Assert.Equal(1,      response.Metrics.RecoveredBatches);
        Assert.Equal(800,    response.Metrics.ReceivedKilograms);
        Assert.Equal(300,    response.Metrics.RecoveredKilograms);
        Assert.Equal(0,      response.Metrics.LandfilledKilograms);
        Assert.Equal(0.375,  response.Metrics.RecyclingRate,     3);
        Assert.Equal(1.0,    response.Metrics.LandfillDiversion, 3);
        Assert.Equal(450,    response.Metrics.Co2SavedKilograms, 3);

        Assert.Equal(2, response.Intakes.Count);
        Assert.Equal(recovered.Id, response.Intakes[0].Id);
        Assert.Equal("Glass",      response.Intakes[0].Material);
        Assert.Equal(300,          response.Intakes[0].WeightKilograms);
        Assert.Equal("Recovered",  response.Intakes[0].Stage);
    }

    [Fact]
    public async Task Handle_FacilityWithNoIntakes_ReturnsEmptyIntakesAndZeroMetrics()
    {
        var facility = NewFacility("Empty Facility");

        _repository
            .GetByIdAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(facility);

        var response = await _handler.Handle(
            new GetFacilityByIdQuery(facility.Id),
            CancellationToken.None);

        Assert.Empty(response.Intakes);
        Assert.Equal(0, response.Metrics.TotalBatches);
        Assert.Equal(0, response.Metrics.ReceivedKilograms);
        Assert.Equal(0, response.Metrics.RecyclingRate);
    }

    [Fact]
    public async Task Handle_FacilityDoesNotExist_ThrowsNotFoundException()
    {
        var facilityId = Guid.NewGuid();

        _repository
            .GetByIdAsync(facilityId, Arg.Any<CancellationToken>())
            .Returns((RecyclingFacility?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new GetFacilityByIdQuery(facilityId),
                CancellationToken.None));

        Assert.Contains(facilityId.ToString(), ex.Message);
    }

    [Fact]
    public async Task Handle_QueriesRepositoryWithExactRequestedId()
    {
        var facility = NewFacility();

        _repository
            .GetByIdAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(facility);

        await _handler.Handle(new GetFacilityByIdQuery(facility.Id), CancellationToken.None);

        await _repository
            .Received(1)
            .GetByIdAsync(facility.Id, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IntakesAreOrderedNewestFirst()
    {
        var facility = NewFacility();
        var baseTime = DateTimeOffset.UtcNow;

        var older = facility.RecordIntake(
            WasteCategory.Plastic, Weight.FromKilograms(100), baseTime);
        var newer = facility.RecordIntake(
            WasteCategory.Metal, Weight.FromKilograms(200), baseTime.AddHours(1));

        _repository
            .GetByIdAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(facility);

        var response = await _handler.Handle(
            new GetFacilityByIdQuery(facility.Id),
            CancellationToken.None);

        Assert.Equal(2, response.Intakes.Count);
        Assert.Equal(newer.Id, response.Intakes[0].Id);
        Assert.Equal(older.Id, response.Intakes[1].Id);
    }
}