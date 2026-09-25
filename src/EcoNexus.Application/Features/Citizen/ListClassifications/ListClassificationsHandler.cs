using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Citizen;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ListClassifications;

internal sealed class ListClassificationsHandler
    : IRequestHandler<ListClassificationsQuery, IReadOnlyList<ClassificationListItemResponse>>
{
    private readonly ICitizenProfileRepository _profileRepository;

    public ListClassificationsHandler(ICitizenProfileRepository profileRepository)
    {
        _profileRepository = profileRepository;
    }

    public async Task<IReadOnlyList<ClassificationListItemResponse>> Handle(
        ListClassificationsQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _profileRepository.GetByUserIdAsync(
            request.UserId,
            cancellationToken);

        if (profile is null)
        {
            throw new NotFoundException(
                $"Citizen profile for user '{request.UserId}' was not found.");
        }

        return profile.Classifications
            .OrderByDescending(c => c.ClassifiedAt)
            .Select(c => new ClassificationListItemResponse(
                c.Id,
                c.Category.ToString(),
                c.Confidence,
                c.IsRecyclable,
                c.IsCompostable,
                c.DisposalInstruction,
                c.ImageReference,
                c.ProviderName,
                c.ClassifiedAt))
            .ToList();
    }
}
