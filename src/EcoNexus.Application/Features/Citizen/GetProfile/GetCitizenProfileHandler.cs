using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.Services;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.GetProfile;

internal sealed class GetCitizenProfileHandler
    : IRequestHandler<GetCitizenProfileQuery, CitizenProfileResponse>
{
    private readonly ICitizenProfileRepository _repository;

    public GetCitizenProfileHandler(ICitizenProfileRepository repository)
    {
        _repository = repository;
    }

    public async Task<CitizenProfileResponse> Handle(
        GetCitizenProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            // First-touch provisioning: create the profile on demand.
            profile = CitizenProfile.Create(
                request.UserId,
                request.FallbackDisplayName,
                DateTimeOffset.UtcNow);

            await _repository.AddAsync(profile, cancellationToken);
        }

        return ToResponse(profile);
    }

    private static CitizenProfileResponse ToResponse(CitizenProfile p) => new(
        p.Id,
        p.UserId,
        p.DisplayName,
        p.HomeAddress,
        p.HomeLatitude,
        p.HomeLongitude,
        p.GreenPointsBalance,
        p.CurrentStreakDays,
        p.LastVisitDate,
        GreenPointLedger.TransactionCount(p),
        GreenPointLedger.TotalEarned(p),
        GreenPointLedger.TotalRedeemed(p),
        p.CreatedAt,
        p.LastUpdatedAt);
}
