using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.CollectionVehicles;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.CollectionVehicles.CreateVehicle;

internal sealed class CreateVehicleHandler
    : IRequestHandler<CreateVehicleCommand, CollectionVehicleResponse>
{
    private readonly ICollectionVehicleRepository _repository;

    public CreateVehicleHandler(ICollectionVehicleRepository repository)
    {
        _repository = repository;
    }

    public async Task<CollectionVehicleResponse> Handle(
        CreateVehicleCommand command,
        CancellationToken cancellationToken)
    {
        var registrationNumber = command.Request.RegistrationNumber.Trim().ToUpperInvariant();

        if (await _repository.RegistrationNumberExistsAsync(registrationNumber, cancellationToken))
        {
            throw new ConflictException(
                $"A vehicle with registration number '{registrationNumber}' already exists.");
        }

        var capacity = Weight.FromKilograms(command.Request.CapacityKilograms);
        var vehicle = CollectionVehicle.Create(registrationNumber, capacity);

        await _repository.AddAsync(vehicle, cancellationToken);

        return new CollectionVehicleResponse(
            vehicle.Id,
            vehicle.RegistrationNumber,
            vehicle.Capacity.Kilograms,
            vehicle.IsActive);
    }
}
