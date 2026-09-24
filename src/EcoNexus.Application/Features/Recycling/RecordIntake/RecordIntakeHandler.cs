using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.RecordIntake;

internal sealed class RecordIntakeHandler
    : IRequestHandler<RecordIntakeCommand, RecordIntakeResponse>
{
    private readonly IRecyclingFacilityRepository _repository;

    public RecordIntakeHandler(IRecyclingFacilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<RecordIntakeResponse> Handle(
        RecordIntakeCommand request,
        CancellationToken cancellationToken)
    {
        var facility = await _repository.GetByIdAsync(request.FacilityId, cancellationToken);

        if (facility is null)
        {
            throw new NotFoundException(
                $"Recycling facility '{request.FacilityId}' was not found.");
        }

        var material = Enum.Parse<WasteCategory>(request.Request.Material, ignoreCase: true);
        var weight = Weight.FromKilograms(request.Request.WeightKilograms);

        var intake = facility.RecordIntake(material, weight, request.Request.RecordedAt);

        await _repository.SaveChangesAsync(cancellationToken);

        return new RecordIntakeResponse(
            facility.Id,
            intake.Id,
            intake.Material.ToString(),
            intake.Weight.Kilograms,
            intake.Stage.ToString(),
            intake.RecordedAt);
    }
}
