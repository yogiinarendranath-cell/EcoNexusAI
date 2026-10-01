using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Recycling.RecordIntake;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Recycling;

public sealed class RecordIntakeHandlerTests
{
    private readonly IRecyclingFacilityRepository _repository =
        Substitute.For<IRecyclingFacilityRepository>();

    private readonly RecordIntakeHandler _handler;

    public RecordIntakeHandlerTests()
    {
        _handler = new RecordIntakeHandler(_repository);
    }

    private static RecyclingFacility NewFacility()
        => RecyclingFacility.Create(
            "Alpha Recycling",
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(10_000));

    private static RecordIntakeCommand Command(
        Guid facilityId,
        string material = "Plastic",
        double weightKg = 100,
        DateTimeOffset? recordedAt = null)
        => new(facilityId,
               new RecordIntakeRequest(
                   material,
                   weightKg,
                   recordedAt ?? DateTimeOffset.UtcNow));

    private void SetupFacility(RecyclingFacility facility)
    {
        _repository
            .GetByIdAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(facility);
    }

    [Fact]
    public async Task Handle_ValidRequest_RecordsIntakeAndReturnsResponse()
    {
        var facility = NewFacility();
        SetupFacility(facility);
        var recordedAt = DateTimeOffset.UtcNow;

        var response = await _handler.Handle(
            Command(facility.Id, material: "Plastic", weightKg: 100, recordedAt: recordedAt),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(facility.Id,      response.FacilityId);
        Assert.NotEqual(Guid.Empty,    response.IntakeId);
        Assert.Equal("Plastic",        response.Material);
        Assert.Equal(100,              response.WeightKilograms);
        Assert.Equal("Received",       response.Stage);
        Assert.Equal(recordedAt,       response.RecordedAt);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_MaterialIsCaseInsensitive_ParsesLowercase()
    {
        var facility = NewFacility();
        SetupFacility(facility);

        var response = await _handler.Handle(
            Command(facility.Id, material: "plastic"),
            CancellationToken.None);

        Assert.Equal("Plastic", response.Material);
    }

    [Fact]
    public async Task Handle_FacilityNotFound_ThrowsNotFoundException()
    {
        var facilityId = Guid.NewGuid();

        _repository
            .GetByIdAsync(facilityId, Arg.Any<CancellationToken>())
            .Returns((RecyclingFacility?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(Command(facilityId), CancellationToken.None));

        Assert.Contains(facilityId.ToString(), ex.Message);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownMaterial_ThrowsArgumentException()
    {
        var facility = NewFacility();
        SetupFacility(facility);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                Command(facility.Id, material: "NotARealMaterial"),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NegativeWeight_ThrowsArgumentOutOfRangeException()
    {
        var facility = NewFacility();
        SetupFacility(facility);

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            _handler.Handle(
                Command(facility.Id, weightKg: -1),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ZeroWeight_ThrowsArgumentException()
    {
        var facility = NewFacility();
        SetupFacility(facility);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                Command(facility.Id, weightKg: 0),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_FacilityIsDecommissioned_ThrowsInvalidOperationException()
    {
        var facility = NewFacility();
        facility.Decommission(DateTimeOffset.UtcNow);
        SetupFacility(facility);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(Command(facility.Id), CancellationToken.None));

        Assert.Contains("decommissioned", ex.Message, StringComparison.OrdinalIgnoreCase);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}