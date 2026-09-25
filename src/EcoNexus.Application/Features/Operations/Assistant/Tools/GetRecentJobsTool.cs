using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Application.Features.Operations.Assistant.Tools;

internal sealed class GetRecentJobsTool : IAssistantTool
{
    private readonly ICollectionJobRepository _jobs;

    public GetRecentJobsTool(ICollectionJobRepository jobs)
        => _jobs = jobs;

    public ToolDescriptor Descriptor { get; } = new(
        "GetRecentJobs",
        "Returns recent collection jobs with their status and scheduled time. Use when the operator asks about collection runs, jobs, or today's schedule.",
        """
        {
          "type": "object",
          "properties": {
            "take": { "type": "integer", "minimum": 1, "maximum": 50, "default": 10 }
          }
        }
        """);

    public async Task<AssistantToolResult> ExecuteAsync(
        IReadOnlyDictionary<string, object?> parameters,
        CancellationToken cancellationToken = default)
    {
        var take = ToolParameterHelpers.GetInt(parameters, "take", defaultValue: 10);

        var all = await _jobs.ListAsync(cancellationToken);

        var recent = all
            .OrderByDescending(j => j.ScheduledFor)
            .Take(take)
            .Select(j => new
            {
                ScheduledFor = j.ScheduledFor,
                Status = j.Status.ToString(),
                StopCount = j.Stops.Count,
            })
            .ToList();

        return new AssistantToolResult(
            Descriptor.Name,
            new
            {
                Count = recent.Count,
                TotalJobs = all.Count,
                Jobs = recent,
            });
    }
}
