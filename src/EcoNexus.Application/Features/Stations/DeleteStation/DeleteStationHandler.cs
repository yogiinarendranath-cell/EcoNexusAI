using EcoNexus.Application.Abstractions.Persistence;
using MediatR;

namespace EcoNexus.Application.Features.Stations.DeleteStation;

internal sealed class DeleteStationHandler : IRequestHandler<DeleteStationCommand>
{
    private readonly IWasteStationRepository _repository;

    public DeleteStationHandler(IWasteStationRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(
        DeleteStationCommand command,
        CancellationToken cancellationToken)
    {
        var station = await _repository.GetByIdAsync(command.StationId, cancellationToken);

        if (station is null)
        {
            return;
        }

        _repository.Remove(station);
        await _repository.SaveChangesAsync(cancellationToken);
    }
}
