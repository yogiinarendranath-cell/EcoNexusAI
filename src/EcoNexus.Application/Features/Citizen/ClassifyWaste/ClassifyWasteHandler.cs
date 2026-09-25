using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Citizen.ClassifyWaste;

internal sealed class ClassifyWasteHandler
    : IRequestHandler<ClassifyWasteCommand, ClassifyWasteResponse>
{
    private readonly ICitizenProfileRepository _profileRepository;
    private readonly IWasteClassificationService _classificationService;

    public ClassifyWasteHandler(
        ICitizenProfileRepository profileRepository,
        IWasteClassificationService classificationService)
    {
        _profileRepository = profileRepository;
        _classificationService = classificationService;
    }

    public async Task<ClassifyWasteResponse> Handle(
        ClassifyWasteCommand request,
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

        // The AI service NEVER throws for malformed AI output — it
        // degrades to a low-confidence "General" result. If it throws
        // anything else (network, provider down), we surface a 409 so
        // the client can retry without polluting our error logs with 500s.
        WasteClassificationResult result;
        try
        {
            result = await _classificationService.ClassifyAsync(
                request.ImageUrl,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ConflictException(
                $"Waste classification is temporarily unavailable ({_classificationService.ProviderName}).");
        }

        var classifiedAt = DateTimeOffset.UtcNow;

        var classification = profile.RecordClassification(
            result,
            request.ImageUrl,
            _classificationService.ProviderName,
            classifiedAt);

        await _profileRepository.SaveChangesAsync(cancellationToken);

        return new ClassifyWasteResponse(
            classification.Id,
            classification.Category.ToString(),
            classification.Confidence,
            classification.IsRecyclable,
            classification.IsCompostable,
            classification.DisposalInstruction,
            result.IsConfident,
            classification.ProviderName,
            classification.ClassifiedAt);
    }
}
