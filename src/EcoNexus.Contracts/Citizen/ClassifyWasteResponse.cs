namespace EcoNexus.Contracts.Citizen;

/// <summary>
/// Response returned after classifying a waste image. Contains the
/// classification result plus a flag indicating whether it is confident
/// enough to act on.
/// </summary>
public sealed record ClassifyWasteResponse(
    Guid ClassificationId,
    string Category,
    double Confidence,
    bool IsRecyclable,
    bool IsCompostable,
    string DisposalInstruction,
    bool IsConfident,
    string ProviderName,
    DateTimeOffset ClassifiedAt);
