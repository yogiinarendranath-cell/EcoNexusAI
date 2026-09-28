using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Common.Exceptions;
using EcoNexus.Application.Features.Operations.Assistant;
using EcoNexus.Application.Features.Operations.Assistant.Tools;
using EcoNexus.Domain.Entities;
using EcoNexus.Domain.ValueObjects;
using NSubstitute;
using Xunit;

namespace EcoNexus.UnitTests.Features.Operations;

public sealed class AskAssistantHandlerTests
{
    private static readonly Guid UserId = Guid.Parse("77777777-7777-7777-7777-777777777777");

    private const string ToolName = "GetCriticalStations";

    private readonly IAssistantLlm _llm = Substitute.For<IAssistantLlm>();
    private readonly IToolRegistry _registry = Substitute.For<IToolRegistry>();
    private readonly IAssistantInteractionRepository _interactions
        = Substitute.For<IAssistantInteractionRepository>();
    private readonly IAssistantTool _tool = Substitute.For<IAssistantTool>();
    private readonly AskAssistantHandler _handler;

    public AskAssistantHandlerTests()
    {
        _llm.ProviderName.Returns("Mock");

        // Capture the descriptor in a local first — nesting a substitute
        // property access inside another .Returns() call confuses
        // NSubstitute's ambient context (it tries to mock the wrong getter).
        var descriptor = new ToolDescriptor(
            ToolName,
            "Returns stations that are at or above the critical fill threshold.",
            "{\"type\":\"object\"}");

        _tool.Descriptor.Returns(descriptor);
        _registry.Descriptors.Returns(new[] { descriptor });

        _handler = new AskAssistantHandler(_llm, _registry, _interactions);
    }

    // -------------------- Test helpers --------------------

    private void LlmResolvesTo(string toolName, IReadOnlyDictionary<string, object?>? parameters = null)
    {
        _llm.ResolveIntentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ToolDescriptor>>(), Arg.Any<CancellationToken>())
            .Returns(new ToolCall(toolName, parameters ?? new Dictionary<string, object?>()));
    }

    private void LlmShapesAnswer(string answer)
    {
        _llm.ShapeAnswerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns(answer);
    }

    private void ToolExecutesWith(object data)
    {
        _tool.ExecuteAsync(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
             .Returns(new AssistantToolResult(ToolName, data));
    }

    private void RegistryKnowsTool()
    {
        _registry.Find(ToolName).Returns(_tool);
    }

    private void RegistryDoesNotKnowTool()
    {
        _registry.Find(Arg.Any<string>()).Returns((IAssistantTool?)null);
    }

    // -------------------- Happy path --------------------

    [Fact]
    public async Task Handle_ValidQuestion_ResolvesToolExecutesAndReturnsAnswer()
    {
        LlmResolvesTo(ToolName);
        LlmShapesAnswer("Three stations are critical.");
        ToolExecutesWith(new { count = 3 });
        RegistryKnowsTool();

        var response = await _handler.Handle(
            new AskAssistantCommand(UserId, "Which stations are critical?"),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.NotEqual(Guid.Empty, response.InteractionId);
        Assert.Equal("Which stations are critical?", response.Question);
        Assert.Equal("Three stations are critical.", response.Answer);
        Assert.Equal(ToolName, response.ToolName);
        Assert.Equal("Mock", response.ProviderName);
        Assert.True(response.LatencyMs >= 0);
        Assert.NotEqual(default, response.OccurredAt);

        await _interactions.Received(1).AddAsync(
            Arg.Is<AssistantInteraction>(i => i.ToolName == ToolName),
            Arg.Any<CancellationToken>());
    }

    // -------------------- LLM intent failure --------------------

    [Fact]
    public async Task Handle_LlmThrowsOnResolve_ThrowsConflictException()
    {
        _llm.ResolveIntentAsync(Arg.Any<string>(), Arg.Any<IReadOnlyList<ToolDescriptor>>(), Arg.Any<CancellationToken>())
            .Returns<Task<ToolCall>>(_ => throw new InvalidOperationException("llm down"));

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new AskAssistantCommand(UserId, "Which stations are critical?"), CancellationToken.None));

        await _interactions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // NOTE: The handler's "empty tool name" branch is unreachable via the
    // public ToolCall constructor — ToolCall rejects empty names at
    // construction time (see ToolCallTests). The defensive check in the
    // handler is kept for future-proofing but cannot be tested via the
    // current public API. If ToolCall ever permits empty names, add a test
    // here.

    // -------------------- Registry doesn't know the tool --------------------

    [Fact]
    public async Task Handle_LlmNamesUnknownTool_ThrowsConflictException()
    {
        LlmResolvesTo("TotallyInventedTool");
        RegistryDoesNotKnowTool();

        await Assert.ThrowsAsync<ConflictException>(() =>
            _handler.Handle(new AskAssistantCommand(UserId, "What's the weather?"), CancellationToken.None));

        await _interactions.DidNotReceiveWithAnyArgs().AddAsync(default!, default);
    }

    // -------------------- Tool execution throws (graceful) --------------------

    [Fact]
    public async Task Handle_ToolThrows_GracefullyReturnsFallbackAndStillPersists()
    {
        LlmResolvesTo(ToolName);
        LlmShapesAnswer("Answer based on the error payload.");
        RegistryKnowsTool();
        _tool.ExecuteAsync(Arg.Any<IReadOnlyDictionary<string, object?>>(), Arg.Any<CancellationToken>())
             .Returns<Task<AssistantToolResult>>(_ => throw new InvalidOperationException("boom"));

        var response = await _handler.Handle(
            new AskAssistantCommand(UserId, "Which stations are critical?"),
            CancellationToken.None);

        // Handler replaced the tool result with an error payload, then asked
        // the LLM to shape an answer. The flow completes.
        Assert.NotNull(response);
        Assert.Equal(ToolName, response.ToolName);

        await _interactions.Received(1).AddAsync(
            Arg.Any<AssistantInteraction>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------- ShapeAnswer throws (fallback) --------------------

    [Fact]
    public async Task Handle_ShapeAnswerThrows_FallsBackToSerializedResult()
    {
        LlmResolvesTo(ToolName);
        RegistryKnowsTool();
        ToolExecutesWith(new { count = 3 });
        _llm.ShapeAnswerAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<object>(), Arg.Any<CancellationToken>())
            .Returns<Task<string>>(_ => throw new InvalidOperationException("shaping down"));

        var response = await _handler.Handle(
            new AskAssistantCommand(UserId, "Which stations are critical?"),
            CancellationToken.None);

        Assert.NotNull(response);
        Assert.StartsWith("Query result: ", response.Answer);

        await _interactions.Received(1).AddAsync(
            Arg.Any<AssistantInteraction>(),
            Arg.Any<CancellationToken>());
    }

    // -------------------- Persistence still recorded on happy path --------------------

    [Fact]
    public async Task Handle_ValidQuestion_RecordsProviderNameFromLlm()
    {
        _llm.ProviderName.Returns("Ollama");
        LlmResolvesTo(ToolName);
        LlmShapesAnswer("Three stations.");
        ToolExecutesWith(new { count = 3 });
        RegistryKnowsTool();

        var response = await _handler.Handle(
            new AskAssistantCommand(UserId, "Which stations are critical?"),
            CancellationToken.None);

        Assert.Equal("Ollama", response.ProviderName);
    }
}