using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A record of a single AI waste-classification event, owned by the
/// citizen who requested it. Child entity of CitizenProfile.
///
/// We snapshot the full result on the entity (category, confidence,
/// flags, instruction) rather than only storing the category, so that
/// the citizen's history stays meaningful even if the AI service's
/// classification heuristics change later.
/// </summary>
public sealed class WasteClassification : Entity
{
    public Guid CitizenProfileId { get; private set; }
    public WasteCategory Category { get; private set; }
    public double Confidence { get; private set; }
    public bool IsRecyclable { get; private set; }
    public bool IsCompostable { get; private set; }
    public string DisposalInstruction { get; private set; }

    /// <summary>The image URL or data-URI that was classified.</summary>
    public string ImageReference { get; private set; }

    /// <summary>
    /// Identifier of the provider that produced the result
    /// (e.g. "Ollama", "Mock", "AzureOpenAI"). Useful for auditing
    /// and for correlating accuracy issues to a specific provider.
    /// </summary>
    public string ProviderName { get; private set; }

    public DateTimeOffset ClassifiedAt { get; private set; }

    // Required by EF Core
    private WasteClassification()
    {
        DisposalInstruction = null!;
        ImageReference = null!;
        ProviderName = null!;
    }

    internal WasteClassification(
        Guid citizenProfileId,
        WasteClassificationResult result,
        string imageReference,
        string providerName,
        DateTimeOffset classifiedAt)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (citizenProfileId == Guid.Empty)
        {
            throw new ArgumentException(
                "CitizenProfileId must not be empty.",
                nameof(citizenProfileId));
        }

        if (string.IsNullOrWhiteSpace(imageReference))
        {
            throw new ArgumentException(
                "ImageReference must not be empty.",
                nameof(imageReference));
        }

        if (imageReference.Length > 2048)
        {
            throw new ArgumentException(
                "ImageReference must be 2048 characters or fewer.",
                nameof(imageReference));
        }

        if (string.IsNullOrWhiteSpace(providerName))
        {
            throw new ArgumentException(
                "ProviderName must not be empty.",
                nameof(providerName));
        }

        if (result.Confidence is < 0 or > 1)
        {
            throw new ArgumentException(
                "Confidence must be between 0 and 1.",
                nameof(result));
        }

        CitizenProfileId = citizenProfileId;
        Category = result.Category;
        Confidence = result.Confidence;
        IsRecyclable = result.IsRecyclable;
        IsCompostable = result.IsCompostable;
        DisposalInstruction = result.DisposalInstruction.Trim();
        ImageReference = imageReference.Trim();
        ProviderName = providerName.Trim();
        ClassifiedAt = classifiedAt;
    }
}
