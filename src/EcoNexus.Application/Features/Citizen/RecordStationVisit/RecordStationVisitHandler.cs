using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.RecordStationVisit;

internal sealed class RecordStationVisitHandler
    : IRequestHandler<RecordStationVisitCommand, RecordStationVisitResponse>
{
    /// <summary>Points awarded for a valid station visit.</summary>
    private const int StationVisitPoints = 10;

    private readonly ICitizenProfileRepository _profileRepository;
    private readonly IWasteStationRepository _stationRepository;

    public RecordStationVisitHandler(
        ICitizenProfileRepository profileRepository,
        IWasteStationRepository stationRepository)
    {
        _profileRepository = profileRepository;
        _stationRepository = stationRepository;
    }

    public async Task<RecordStationVisitResponse> Handle(
        RecordStationVisitCommand request,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException(
                $"Citizen profile for user '{request.UserId}' was not found.");
        }

        var station = await _stationRepository.GetByIdAsync(
            request.Request.StationId,
            cancellationToken);

        if (station is null)
        {
            throw new NotFoundException(
                $"Station '{request.Request.StationId}' was not found.");
        }

        var occurredAt = DateTimeOffset.UtcNow;

        EcoNexus.Domain.Entities.GreenPointTransaction tx;
        try
        {
            tx = profile.RecordStationVisit(
                request.Request.StationId,
                request.Request.VisitDate,
                StationVisitPoints,
                occurredAt);
        }
        catch (InvalidOperationException ex)
        {
            // Domain invariant (rate limit) → HTTP 409.
            throw new ConflictException(ex.Message);
        }

        await _profileRepository.SaveChangesAsync(cancellationToken);

        return new RecordStationVisitResponse(
            profile.Id,
            tx.Id,
            request.Request.StationId,
            StationVisitPoints,
            profile.GreenPointsBalance,
            profile.CurrentStreakDays,
            occurredAt);
    }
}
