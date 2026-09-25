using EcoNexus.Contracts.Operations;
using MediatR;

namespace EcoNexus.Application.Features.Operations.Assistant;

/// <summary>
/// Command: ask the operations assistant a natural-language question.
/// The handler resolves the intent, executes the chosen tool, shapes
/// the answer, and records the interaction.
/// </summary>
public sealed record AskAssistantCommand(
    Guid UserId,
    string Question) : IRequest<AskAssistantResponse>;
