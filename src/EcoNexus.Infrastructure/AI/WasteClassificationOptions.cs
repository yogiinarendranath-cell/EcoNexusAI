namespace EcoNexus.Infrastructure.AI;

/// <summary>
/// Configuration for the waste-classification subsystem. Bound from
/// the "AI" section of appsettings.json.
///
/// Provider values: "Mock" (default) or "Ollama".
/// </summary>
public sealed class WasteClassificationOptions
{
    public const string SectionName = "AI";

    /// <summary>
    /// Which provider to use: "Mock" or "Ollama". Defaults to "Mock"
    /// so development works without any local AI setup.
    /// </summary>
    public string Provider { get; set; } = "Mock";

    /// <summary>Ollama-specific settings. Ignored when Provider is Mock.</summary>
    public OllamaOptions Ollama { get; set; } = new();

    /// <summary>Assistant-specific settings. Bound from AI:Assistant.</summary>
    public AssistantOptions Assistant { get; set; } = new();

    public sealed class OllamaOptions
    {
        /// <summary>Base URL of the local Ollama server.</summary>
        public string BaseUrl { get; set; } = "http://localhost:11434";

        /// <summary>Model name (must be pulled first: `ollama pull llava`).</summary>
        public string Model { get; set; } = "llava";

        /// <summary>
        /// Per-request timeout in seconds. CPU inference on LLaVA can
        /// take 30–90 seconds; give it room.
        /// </summary>
        public int TimeoutSeconds { get; set; } = 120;
    }

    public sealed class AssistantOptions
    {
        /// <summary>Ollama model for the assistant (e.g. llama3).</summary>
        public string OllamaModel { get; set; } = "llama3";

        /// <summary>Timeout in seconds for a single assistant call.</summary>
        public int TimeoutSeconds { get; set; } = 60;
    }
}
