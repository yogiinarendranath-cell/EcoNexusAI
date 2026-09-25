using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.GetProfile;

/// <summary>
/// Query: fetch the current citizen's profile. Creates the profile on
/// first call if the user has not yet been provisioned.
/// </summary>
public sealed record GetCitizenProfileQuery(Guid UserId, string FallbackDisplayName)
    : IRequest<CitizenProfileResponse>;
