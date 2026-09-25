using EcoNexus.Application.Abstractions.Identity;
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
    private readonly IUserDirectory _userDirectory;

    public GetCitizenProfileHandler(
        ICitizenProfileRepository repository,
        IUserDirectory userDirectory)
    {
        _repository = repository;
        _userDirectory = userDirectory;
    }

    public async Task<CitizenProfileResponse> Handle(
        GetCitizenProfileQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _repository.GetByUserIdAsync(request.UserId, cancellationToken);

        if (profile is null)
        {
            // First-touch provisioning. Pull the display name from Identity
            // so the profile reflects the user's chosen name, not the fallback.
            var resolvedDisplayName = await _userDirectory.GetDisplayNameAsync(
                request.UserId,
                cancellationToken)
                ?? request.FallbackDisplayName;

            profile = CitizenProfile.Create(
                request.UserId,
                resolvedDisplayName,
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
