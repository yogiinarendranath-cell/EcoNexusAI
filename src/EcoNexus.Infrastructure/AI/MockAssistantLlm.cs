using System.Text;
using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Infrastructure.AI;

/// <summary>
/// Deterministic implementation of IAssistantLlm that matches keywords
/// in the operator's question to tools. No external calls, no waiting.
///
/// Used for development, tests, and offline demos. Deliberately simple:
/// the point is to exercise the full assistant pipeline without needing
/// a real LLM.
/// </summary>
internal sealed class MockAssistantLlm : IAssistantLlm
{
    public string ProviderName => "Mock";

    public Task<ToolCall> ResolveIntentAsync(
        string question,
        IReadOnlyList<ToolDescriptor> availableTools,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);

        var q = question.ToLowerInvariant();

        // Order matters — first match wins.
        if (Contains(q, "critical", "overflow", "full", "urgent", "priorit"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetCriticalStations"));
        }

        if (Contains(q, "recycl", "recovery", "co2", "impact", "divert"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetRecyclingMetrics"));
        }

        if (Contains(q, "facilit", "operational", "online"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetActiveFacilities"));
        }

        if (Contains(q, "vehicle", "fleet", "truck"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetActiveVehicles"));
        }

        if (Contains(q, "job", "route", "schedule"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetRecentJobs"));
        }

        if (Contains(q, "categor", "waste type", "mix"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetWasteByCategory"));
        }

        if (Contains(q, "how many", "count", "how much stations"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetStationCount"));
        }

        if (Contains(q, "overview", "status", "state"))
        {
            return Task.FromResult(ToolCall.NoParameters("GetFacilityOverview"));
        }

        // No match — return a tool name the registry doesn't have.
        // The handler will convert this to a friendly ConflictException.
        return Task.FromResult(ToolCall.NoParameters("__unknown__"));
    }

    public Task<string> ShapeAnswerAsync(
        string question,
        string toolName,
        object toolResult,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(question);
        ArgumentException.ThrowIfNullOrWhiteSpace(toolName);
        ArgumentNullException.ThrowIfNull(toolResult);

        // The mock's "shaping" is a simple template per tool. The data
        // is real — only the prose is canned. This is honest: it does
        // not invent numbers, it just formats what the tool returned.
        var json = System.Text.Json.JsonSerializer.Serialize(toolResult);
        var sb = new StringBuilder();
        sb.Append($"Tool '{toolName}' returned: ");
        sb.Append(json);
        return Task.FromResult(sb.ToString());
    }

    private static bool Contains(string haystack, params string[] needles)
        => needles.Any(n => haystack.Contains(n, StringComparison.Ordinal));
}
