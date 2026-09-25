using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Abstractions.AI;

/// <summary>
/// Application-layer abstraction over an AI vision service that classifies
/// waste from an image. Implementations may call a local Ollama model, a
/// cloud vision API, or return deterministic results for testing.
///
/// The Application layer depends only on this interface, never on a
/// specific provider. Which implementation is used is a DI decision.
/// </summary>
public interface IWasteClassificationService
{
    /// <summary>
    /// Classifies the primary waste item in the image at the given URL.
    /// MUST NOT throw on malformed AI output — implementations should
    /// degrade gracefully to a low-confidence "General" result so the
    /// caller can render an "unclear image" message.
    /// </summary>
    /// <param name="imageUrl">HTTP(S) URL or data URI of the image.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A provider-agnostic classification result.</returns>
    Task<WasteClassificationResult> ClassifyAsync(
        string imageUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Identifies the provider for audit purposes (e.g. "Ollama", "Mock",
    /// "AzureOpenAI"). Persisted with each classification record.
    /// </summary>
    string ProviderName { get; }
}
