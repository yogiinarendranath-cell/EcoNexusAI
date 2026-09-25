using System.Diagnostics;
using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Operations.Assistant.Tools;
using EcoNexus.Contracts.Operations;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using MediatR;

namespace EcoNexus.Application.Features.Operations.Assistant;

internal sealed class AskAssistantHandler
    : IRequestHandler<AskAssistantCommand, AskAssistantResponse>
{
    private readonly IAssistantLlm _llm;
    private readonly IToolRegistry _registry;
    private readonly IAssistantInteractionRepository _interactionRepository;

    public AskAssistantHandler(
        IAssistantLlm llm,
        IToolRegistry registry,
        IAssistantInteractionRepository interactionRepository)
    {
        _llm = llm;
        _registry = registry;
        _interactionRepository = interactionRepository;
    }

    public async Task<AskAssistantResponse> Handle(
        AskAssistantCommand request,
        CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();

        // 1. Ask the LLM to pick a tool from the whitelist.
        ToolCall toolCall;
        try
        {
            toolCall = await _llm.ResolveIntentAsync(
                request.Question,
                _registry.Descriptors,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            throw new ConflictException(
                $"The assistant is temporarily unavailable ({_llm.ProviderName}).");
        }

        if (string.IsNullOrWhiteSpace(toolCall.ToolName))
        {
            throw new ConflictException(
                "I couldn't understand that question. Try rephrasing.");
        }

        // 2. Look up the chosen tool. If the LLM invented a name, reject.
        var tool = _registry.Find(toolCall.ToolName);
        if (tool is null)
        {
            throw new ConflictException(
                $"I don't know how to answer that. Try asking about stations, alerts, or recycling.");
        }

        // 3. Execute the tool. Failures inside the tool become a
        //    graceful "query failed" answer rather than a 500.
        AssistantToolResult toolResult;
        try
        {
            toolResult = await tool.ExecuteAsync(toolCall.Parameters, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception)
        {
            toolResult = new AssistantToolResult(
                tool.Descriptor.Name,
                new { error = "The query failed to execute. Please try again." });
        }

        // 4. Ask the LLM to shape the structured result into prose.
        string answer;
        try
        {
            answer = await _llm.ShapeAnswerAsync(
                request.Question,
                tool.Descriptor.Name,
                toolResult.Data,
                cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // Fallback: serialize the raw data. Ugly but honest.
            answer = $"Query result: {System.Text.Json.JsonSerializer.Serialize(toolResult.Data)}";
        }

        stopwatch.Stop();

        // 5. Record the interaction — every decision, auditable.
        var occurredAt = DateTimeOffset.UtcNow;
        var interaction = AssistantInteraction.Record(
            request.UserId,
            request.Question,
            tool.Descriptor.Name,
            toolCall.Parameters,
            toolResult.Data,
            answer,
            _llm.ProviderName,
            stopwatch.ElapsedMilliseconds,
            occurredAt);

        await _interactionRepository.AddAsync(interaction, cancellationToken);

        // 6. Return the response.
        return new AskAssistantResponse(
            interaction.Id,
            interaction.Question,
            interaction.Answer,
            interaction.ToolName,
            interaction.ToolParametersJson,
            interaction.ProviderName,
            interaction.LatencyMs,
            interaction.OccurredAt);
    }
}
