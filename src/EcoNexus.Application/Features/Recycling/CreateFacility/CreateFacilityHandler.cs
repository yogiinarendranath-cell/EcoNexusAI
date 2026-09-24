using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Recycling;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Recycling.CreateFacility;

internal sealed class CreateFacilityHandler
    : IRequestHandler<CreateFacilityCommand, CreateFacilityResponse>
{
    private readonly IRecyclingFacilityRepository _repository;

    public CreateFacilityHandler(IRecyclingFacilityRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateFacilityResponse> Handle(
        CreateFacilityCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var name = dto.Name.Trim();

        if (await _repository.NameExistsAsync(name, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A recycling facility named '{name}' already exists.");
        }

        var location = Location.Create(dto.Latitude, dto.Longitude);
        var capacity = Weight.FromKilograms(dto.DailyCapacityKilograms);

        var facility = RecyclingFacility.Create(name, location, capacity);

        await _repository.AddAsync(facility, cancellationToken);

        return new CreateFacilityResponse(facility.Id, facility.Name);
    }
}
