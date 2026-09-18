using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Stations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Stations.CreateStation;

internal sealed class CreateStationHandler
    : IRequestHandler<CreateStationCommand, CreateStationResponse>
{
    private readonly IWasteStationRepository _repository;

    public CreateStationHandler(IWasteStationRepository repository)
    {
        _repository = repository;
    }

    public async Task<CreateStationResponse> Handle(
        CreateStationCommand request,
        CancellationToken cancellationToken)
    {
        var dto = request.Request;

        var code = StationCode.Create(dto.Code);
        var location = Location.Create(dto.Latitude, dto.Longitude);
        var capacity = Weight.FromKilograms(dto.CapacityKilograms);
        var category = Enum.Parse<WasteCategory>(dto.PrimaryCategory, ignoreCase: true);

        if (await _repository.CodeExistsAsync(code, cancellationToken))
        {
            throw new InvalidOperationException(
                $"A station with code '{code.Value}' already exists.");
        }

        var station = WasteStation.Create(code, location, capacity, category);

        await _repository.AddAsync(station, cancellationToken);

        return new CreateStationResponse(station.Id, station.Code.Value);
    }
}
