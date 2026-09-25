using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.ValueObjects;

/// <summary>
/// The pure result of an AI waste-classification call. Immutable value
/// object: same data, same equality. Deliberately provider-agnostic —
/// whether the source was LLaVA, GPT-4o, or a mock, this is the shape
/// the domain and application layers see.
/// </summary>
public sealed record WasteClassificationResult(
    WasteCategory Category,
    double Confidence,
    bool IsRecyclable,
    bool IsCompostable,
    string DisposalInstruction)
{
    /// <summary>
    /// Minimum confidence below which a classification is treated as
    /// "unclear". Chosen so that a low-confidence guess (e.g. General
    /// with 0.2) does not mislead the citizen.
    /// </summary>
    public const double MinimumConfidence = 0.6;

    /// <summary>
    /// Returns true when this result is confident enough to be
    /// surfaced to the citizen as a definite answer.
    /// </summary>
    public bool IsConfident => Confidence >= MinimumConfidence;
}
