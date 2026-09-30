using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Recycling.AdvanceIntake;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Xunit;

namespace EcoNexus.UnitTests.Features.Recycling;

public sealed class AdvanceIntakeHandlerTests
{
    private readonly IRecyclingFacilityRepository _repository =
        Substitute.For<IRecyclingFacilityRepository>();

    private readonly AdvanceIntakeHandler _handler;

    public AdvanceIntakeHandlerTests()
    {
        _handler = new AdvanceIntakeHandler(_repository);
    }

    private static RecyclingFacility NewFacility() =>
        RecyclingFacility.Create(
            "Test Facility",
            Location.Create(12.9716, 77.5946),
            Weight.FromKilograms(10_000));

    private static FacilityIntake AddIntake(RecyclingFacility facility)
        => facility.RecordIntake(
            WasteCategory.Plastic,
            Weight.FromKilograms(500),
            DateTimeOffset.UtcNow);

    private static void AdvanceToStage(
        RecyclingFacility facility, FacilityIntake intake, IntakeStage stage)
    {
        facility.AdvanceIntake(intake.Id, stage, DateTimeOffset.UtcNow);
    }

    private static AdvanceIntakeRequest Request(
        string nextStage, DateTimeOffset? at = null) =>
        new(nextStage, at ?? DateTimeOffset.UtcNow);

    private void SetupFacility(RecyclingFacility facility)
    {
        _repository
            .GetByIdAsync(facility.Id, Arg.Any<CancellationToken>())
            .Returns(facility);
    }

    [Theory]
    [InlineData(IntakeStage.Received,   "Sorted")]
    [InlineData(IntakeStage.Sorted,     "Processed")]
    [InlineData(IntakeStage.Processed,  "Recovered")]
    public async Task Handle_ForwardTransition_AdvancesIntakeAndSaves(
        IntakeStage currentStage, string nextStage)
    {
        var facility = NewFacility();
        var intake = AddIntake(facility);
        if (currentStage != IntakeStage.Received)
        {
            AdvanceToStage(facility, intake, currentStage);
        }

        SetupFacility(facility);
        var at = DateTimeOffset.UtcNow;

        var response = await _handler.Handle(
            new AdvanceIntakeCommand(facility.Id, intake.Id, Request(nextStage, at)),
            CancellationToken.None);

        Assert.Equal(facility.Id, response.FacilityId);
        Assert.Equal(intake.Id, response.IntakeId);
        Assert.Equal(currentStage.ToString(), response.PreviousStage);
        Assert.Equal(nextStage, response.CurrentStage);
        Assert.Equal(at, response.AdvancedAt);

        Assert.Equal(Enum.Parse<IntakeStage>(nextStage), intake.Stage);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReceivedToLandfilled_SkipsStagesViaSideExit()
    {
        var facility = NewFacility();
        var intake = AddIntake(facility);
        SetupFacility(facility);

        var response = await _handler.Handle(
            new AdvanceIntakeCommand(
                facility.Id,
                intake.Id,
                Request("Landfilled")),
            CancellationToken.None);

        Assert.Equal("Received", response.PreviousStage);
        Assert.Equal("Landfilled", response.CurrentStage);
        Assert.Equal(IntakeStage.Landfilled, intake.Stage);

        await _repository
            .Received(1)
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData("sorted")]
    [InlineData("SORTED")]
    [InlineData("SoRtEd")]
    public async Task Handle_StageStringIsCaseInsensitive_ParsesCorrectly(string nextStage)
    {
        var facility = NewFacility();
        var intake = AddIntake(facility);
        SetupFacility(facility);

        var response = await _handler.Handle(
            new AdvanceIntakeCommand(
                facility.Id,
                intake.Id,
                Request(nextStage)),
            CancellationToken.None);

        Assert.Equal("Sorted", response.CurrentStage);
        Assert.Equal(IntakeStage.Sorted, intake.Stage);
    }

    [Fact]
    public async Task Handle_FacilityNotFound_ThrowsNotFoundException()
    {
        var facilityId = Guid.NewGuid();
        var intakeId = Guid.NewGuid();

        _repository
            .GetByIdAsync(facilityId, Arg.Any<CancellationToken>())
            .Returns((RecyclingFacility?)null);

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(facilityId, intakeId, Request("Sorted")),
                CancellationToken.None));

        Assert.Contains(facilityId.ToString(), ex.Message);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IntakeNotFoundAtFacility_ThrowsNotFoundException()
    {
        var facility = NewFacility();
        SetupFacility(facility);

        var missingIntakeId = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facility.Id,
                    missingIntakeId,
                    Request("Sorted")),
                CancellationToken.None));

        Assert.Contains(missingIntakeId.ToString(), ex.Message);
        Assert.Contains(facility.Id.ToString(), ex.Message);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_IntakeBelongsToAnotherFacility_ThrowsNotFoundException()
    {
        var facilityA = NewFacility();
        var intake = AddIntake(facilityA);

        var facilityB = NewFacility();
        SetupFacility(facilityB);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facilityB.Id,
                    intake.Id,
                    Request("Sorted")),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnknownStageString_ThrowsArgumentException()
    {
        var facility = NewFacility();
        var intake = AddIntake(facility);
        SetupFacility(facility);

        await Assert.ThrowsAsync<ArgumentException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facility.Id,
                    intake.Id,
                    Request("NotARealStage")),
                CancellationToken.None));

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SameStage_ThrowsInvalidOperationException()
    {
        var facility = NewFacility();
        var intake = AddIntake(facility);
        SetupFacility(facility);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facility.Id,
                    intake.Id,
                    Request("Received")),
                CancellationToken.None));

        Assert.Contains("already", ex.Message, StringComparison.OrdinalIgnoreCase);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_BackwardTransition_ThrowsInvalidOperationException()
    {
        var facility = NewFacility();
        var intake = AddIntake(facility);
        AdvanceToStage(facility, intake, IntakeStage.Processed);
        SetupFacility(facility);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facility.Id,
                    intake.Id,
                    Request("Sorted")),
                CancellationToken.None));

        Assert.Contains("backwards", ex.Message, StringComparison.OrdinalIgnoreCase);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdvanceFromRecovered_ThrowsTerminalStageException()
    {
        // Recovered(3) is the highest non-side-exit stage. Attempting to move
        // forward from it (to Landfilled(4)) hits the terminal-stage guard,
        // because the target is not backwards.
        var facility = NewFacility();
        var intake = AddIntake(facility);
        AdvanceToStage(facility, intake, IntakeStage.Recovered);
        SetupFacility(facility);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facility.Id,
                    intake.Id,
                    Request("Landfilled")),
                CancellationToken.None));

        Assert.Contains("terminal", ex.Message, StringComparison.OrdinalIgnoreCase);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AdvanceFromLandfilled_ThrowsBackwardsException()
    {
        // Landfilled(4) is the highest enum value. Any other target is lower,
        // so the backward-transition guard fires before the terminal guard.
        // Documenting this order — the "terminal" branch is unreachable for
        // Landfilled because the backwards check wins.
        var facility = NewFacility();
        var intake = AddIntake(facility);
        AdvanceToStage(facility, intake, IntakeStage.Landfilled);
        SetupFacility(facility);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facility.Id,
                    intake.Id,
                    Request("Recovered")),
                CancellationToken.None));

        Assert.Contains("backwards", ex.Message, StringComparison.OrdinalIgnoreCase);

        await _repository
            .DidNotReceive()
            .SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SaveChangesThrowsConcurrencyConflict_ExceptionPropagates()
    {
        var facility = NewFacility();
        var intake = AddIntake(facility);
        SetupFacility(facility);

        _repository
            .SaveChangesAsync(Arg.Any<CancellationToken>())
            .ThrowsAsync(new ConcurrencyConflictException(
                "The facility was modified by another user."));

        await Assert.ThrowsAsync<ConcurrencyConflictException>(() =>
            _handler.Handle(
                new AdvanceIntakeCommand(
                    facility.Id,
                    intake.Id,
                    Request("Sorted")),
                CancellationToken.None));
    }
}