namespace EcoNexus.Contracts.Citizen;

/// <summary>A single AI classification in the citizen's history.</summary>
public sealed record ClassificationListItemResponse(
    Guid Id,
    string Category,
    double Confidence,
    bool IsRecyclable,
    bool IsCompostable,
    string DisposalInstruction,
    string ImageReference,
    string ProviderName,
    DateTimeOffset ClassifiedAt);
