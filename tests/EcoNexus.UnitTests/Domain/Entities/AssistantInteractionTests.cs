using EcoNexus.Domain.Entities;
using Xunit;

namespace EcoNexus.UnitTests.Domain.Entities;

/// <summary>
/// Unit tests for <see cref="AssistantInteraction"/> — the audit record
/// of a single operations-assistant exchange.
/// </summary>
public sealed class AssistantInteractionTests
{
    private static readonly Guid UserId = Guid.Parse("44444444-4444-4444-4444-444444444444");
    private static readonly DateTimeOffset T0 = new(2026, 9, 27, 12, 0, 0, TimeSpan.Zero);

    private static AssistantInteraction RecordValid(
        Guid? userId = null,
        string question = "Which stations are critical?",
        string toolName = "GetCriticalStations",
        IReadOnlyDictionary<string, object?>? parameters = null,
        object? toolResult = null,
        string answer = "Three stations are critical.",
        string providerName = "Ollama",
        long latencyMs = 42)
        => AssistantInteraction.Record(
            userId ?? UserId,
            question,
            toolName,
            parameters ?? new Dictionary<string, object?>(),
            toolResult,
            answer,
            providerName,
            latencyMs,
            T0);

    [Fact]
    public void Record_ValidInputs_StoresAllFields()
    {
        var interaction = RecordValid();

        Assert.Equal(UserId, interaction.UserId);
        Assert.Equal("Which stations are critical?", interaction.Question);
        Assert.Equal("GetCriticalStations", interaction.ToolName);
        Assert.Equal("Three stations are critical.", interaction.Answer);
        Assert.Equal("Ollama", interaction.ProviderName);
        Assert.Equal(42, interaction.LatencyMs);
        Assert.Equal(T0, interaction.OccurredAt);
    }

    [Fact]
    public void Record_TrimsTextFields()
    {
        var interaction = RecordValid(
            question: "   What?   ",
            toolName: "  ToolName  ",
            answer: "   Answer   ",
            providerName: "  Ollama  ");

        Assert.Equal("What?", interaction.Question);
        Assert.Equal("ToolName", interaction.ToolName);
        Assert.Equal("Answer", interaction.Answer);
        Assert.Equal("Ollama", interaction.ProviderName);
    }

    [Fact]
    public void Record_EmptyUserId_Throws()
    {
        Assert.Throws<ArgumentException>(() => RecordValid(userId: Guid.Empty));
    }

    [Fact]
    public void Record_EmptyQuestion_Throws()
    {
        Assert.Throws<ArgumentException>(() => RecordValid(question: ""));
    }

    [Fact]
    public void Record_WhitespaceQuestion_Throws()
    {
        Assert.Throws<ArgumentException>(() => RecordValid(question: "   "));
    }

    [Fact]
    public void Record_QuestionOver2000Chars_Throws()
    {
        var tooLong = new string('Q', 2001);
        Assert.Throws<ArgumentException>(() => RecordValid(question: tooLong));
    }

    [Fact]
    public void Record_EmptyToolName_Throws()
    {
        Assert.Throws<ArgumentException>(() => RecordValid(toolName: ""));
    }

    [Fact]
    public void Record_EmptyAnswer_Throws()
    {
        Assert.Throws<ArgumentException>(() => RecordValid(answer: ""));
    }

    [Fact]
    public void Record_AnswerOver4000Chars_Throws()
    {
        var tooLong = new string('A', 4001);
        Assert.Throws<ArgumentException>(() => RecordValid(answer: tooLong));
    }

    [Fact]
    public void Record_EmptyProviderName_Throws()
    {
        Assert.Throws<ArgumentException>(() => RecordValid(providerName: ""));
    }

    [Fact]
    public void Record_NegativeLatency_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RecordValid(latencyMs: -1));
    }

    [Fact]
    public void Record_ZeroLatency_Allowed()
    {
        var interaction = RecordValid(latencyMs: 0);

        Assert.Equal(0, interaction.LatencyMs);
    }

    [Fact]
    public void Record_SerializesParametersToJson()
    {
        var parameters = new Dictionary<string, object?>
        {
            ["limit"] = 10,
            ["category"] = "Plastic"
        };

        var interaction = RecordValid(parameters: parameters);

        Assert.Contains("limit", interaction.ToolParametersJson);
        Assert.Contains("10", interaction.ToolParametersJson);
        Assert.Contains("Plastic", interaction.ToolParametersJson);
    }

    [Fact]
    public void Record_NullToolResult_SerializesAsLiteralNull()
    {
        var interaction = RecordValid(toolResult: null);

        Assert.Equal("null", interaction.ToolResultJson);
    }

    [Fact]
    public void Record_NonNullToolResult_SerializesToJson()
    {
        var interaction = RecordValid(toolResult: new { count = 5 });

        Assert.Contains("count", interaction.ToolResultJson);
        Assert.Contains("5", interaction.ToolResultJson);
    }
}

