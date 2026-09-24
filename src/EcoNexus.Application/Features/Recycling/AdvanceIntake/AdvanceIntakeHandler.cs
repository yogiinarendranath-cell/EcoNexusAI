using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Enums;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.AdvanceIntake;

internal sealed class AdvanceIntakeHandler
    : IRequestHandler<AdvanceIntakeCommand, AdvanceIntakeResponse>
{
    private readonly IRecyclingFacilityRepository _repository;

    public AdvanceIntakeHandler(IRecyclingFacilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<AdvanceIntakeResponse> Handle(
        AdvanceIntakeCommand request,
        CancellationToken cancellationToken)
    {
        var facility = await _repository.GetByIdAsync(request.FacilityId, cancellationToken);

        if (facility is null)
        {
            throw new NotFoundException(
                $"Recycling facility '{request.FacilityId}' was not found.");
        }

        var intake = facility.Intakes.FirstOrDefault(i => i.Id == request.IntakeId);

        if (intake is null)
        {
            throw new NotFoundException(
                $"Intake '{request.IntakeId}' was not found at facility '{request.FacilityId}'.");
        }

        var previousStage = intake.Stage;
        var nextStage = Enum.Parse<IntakeStage>(request.Request.NextStage, ignoreCase: true);

        facility.AdvanceIntake(request.IntakeId, nextStage, request.Request.AdvancedAt);

        await _repository.SaveChangesAsync(cancellationToken);

        return new AdvanceIntakeResponse(
            facility.Id,
            intake.Id,
            previousStage.ToString(),
            intake.Stage.ToString(),
            request.Request.AdvancedAt);
    }
}
