using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Stations.UpdateStation;

internal sealed class UpdateStationHandler : IRequestHandler<UpdateStationCommand>
{
    private readonly IWasteStationRepository _repository;

    public UpdateStationHandler(IWasteStationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        UpdateStationCommand command,
        CancellationToken cancellationToken)
    {
        var station = await _repository.GetByIdAsync(command.StationId, cancellationToken)
            ?? throw new NotFoundException(
                $"Station '{command.StationId}' was not found.");

        var location = Location.Create(
            command.Request.Latitude,
            command.Request.Longitude);

        var capacity = Weight.FromKilograms(
            command.Request.CapacityKilograms);

        var category = Enum.Parse<WasteCategory>(
            command.Request.PrimaryCategory,
            ignoreCase: true);

        station.UpdateMetadata(location, capacity, category);

        await _repository.SaveChangesAsync(cancellationToken);
    }
}
