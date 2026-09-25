using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;
using EcoNexus.Infrastructure.AI;
using Xunit;

namespace EcoNexus.UnitTests.Infrastructure.AI;

/// <summary>
/// Contract tests for MockWasteClassificationService. These lock in the
/// behavior that callers depend on: determinism, confidence range, and
/// provider identification.
///
/// Note: the mock uses reflection-free construction via the internal
/// parameterless constructor exposed to the test assembly. We call
/// the public ClassifyAsync API like any other caller would.
/// </summary>
public sealed class MockWasteClassificationServiceTests
{
    private static MockWasteClassificationService CreateSut()
        => new();

    [Fact]
    public async Task ClassifyAsync_IsDeterministic_SameUrlReturnsSameCategoryAndConfidence()
    {
        var sut = CreateSut();
        const string url = "https://example.com/plastic-bottle.jpg";

        var first = await sut.ClassifyAsync(url);
        var second = await sut.ClassifyAsync(url);

        Assert.Equal(first.Category, second.Category);
        Assert.Equal(first.Confidence, second.Confidence, precision: 6);
        Assert.Equal(first.IsRecyclable, second.IsRecyclable);
        Assert.Equal(first.IsCompostable, second.IsCompostable);
        Assert.Equal(first.DisposalInstruction, second.DisposalInstruction);
    }

    [Theory]
    [InlineData("https://example.com/a.jpg")]
    [InlineData("https://example.com/b.png")]
    [InlineData("https://example.com/c.webp")]
    [InlineData("https://example.com/very-long-filename-with-many-chars.jpg")]
    [InlineData("data:image/png;base64,iVBORw0KGgo=")]
    [InlineData("http://localhost/test1")]
    [InlineData("http://localhost/test2")]
    [InlineData("http://x.y/z")]
    [InlineData("http://x.y/w")]
    [InlineData("ftp://odd-but-accepted")]
    [InlineData("test1")]
    [InlineData("test2")]
    [InlineData("test3")]
    [InlineData("test4")]
    [InlineData("test5")]
    [InlineData("test6")]
    [InlineData("test7")]
    [InlineData("test8")]
    [InlineData("test9")]
    [InlineData("test10")]
    public async Task ClassifyAsync_ConfidenceAlwaysInExpectedRange(string url)
    {
        var sut = CreateSut();

        var result = await sut.ClassifyAsync(url);

        Assert.InRange(result.Confidence, 0.75, 0.98);
    }

    [Fact]
    public async Task ClassifyAsync_DifferentUrlsProduceDifferentResults()
    {
        var sut = CreateSut();

        var a = await sut.ClassifyAsync("https://example.com/url-a.jpg");
        var b = await sut.ClassifyAsync("https://example.com/url-b.jpg");

        // At least one observable field should differ across two distinct URLs.
        var same =
            a.Category == b.Category &&
            Math.Abs(a.Confidence - b.Confidence) < 0.0001;

        Assert.False(same, "Two different URLs should not produce identical classifications.");
    }

    [Fact]
    public async Task ClassifyAsync_ReturnsMockProviderName()
    {
        var sut = CreateSut();

        var result = await sut.ClassifyAsync("https://example.com/any.jpg");

        Assert.Equal("Mock", sut.ProviderName);
        // Provider name is a property of the service, not the result.
        // The result record intentionally has no ProviderName field —
        // that is attached by the entity (WasteClassification).
        Assert.NotNull(result);
    }

    [Theory]
    [InlineData("https://example.com/plastic-bottle.jpg")]
    [InlineData("https://example.com/banana-peel.jpg")]
    [InlineData("https://example.com/glass-jar.jpg")]
    [InlineData("https://example.com/old-phone.jpg")]
    public async Task ClassifyAsync_ResultIsConfident(string url)
    {
        var sut = CreateSut();

        var result = await sut.ClassifyAsync(url);

        // Because confidence is always >= 0.75 and MinimumConfidence is 0.6,
        // every result from the mock is confident by construction.
        Assert.True(
            result.IsConfident,
            $"Mock returned confidence {result.Confidence}, which is below MinimumConfidence.");
        Assert.True(result.Confidence >= WasteClassificationResult.MinimumConfidence);
    }

    [Fact]
    public async Task ClassifyAsync_ThrowsOnNullOrEmptyUrl()
    {
        var sut = CreateSut();

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => sut.ClassifyAsync(null!));
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ClassifyAsync(""));
        await Assert.ThrowsAsync<ArgumentException>(
            () => sut.ClassifyAsync("   "));
    }

    [Theory]
    [InlineData(WasteCategory.Organic, false, true)]
    [InlineData(WasteCategory.Plastic, true, false)]
    [InlineData(WasteCategory.Paper, true, true)]
    [InlineData(WasteCategory.Glass, true, false)]
    [InlineData(WasteCategory.Metal, true, false)]
    [InlineData(WasteCategory.EWaste, true, false)]
    [InlineData(WasteCategory.Hazardous, false, false)]
    [InlineData(WasteCategory.General, false, false)]
    public async Task ClassifyAsync_ResultHasCategoryConsistentFlags(
        WasteCategory expectedCategory,
        bool expectedRecyclable,
        bool expectedCompostable)
    {
        // We can't force the mock to pick a specific category without
        // reversing the hash. So we search a small range of URLs until
        // we find one that produces the target category — then we assert
        // the flags are consistent with that category.
        var sut = CreateSut();
        WasteClassificationResult? match = null;

        for (var i = 0; i < 500; i++)
        {
            var result = await sut.ClassifyAsync($"probe://{expectedCategory}-{i}");
            if (result.Category == expectedCategory)
            {
                match = result;
                break;
            }
        }

        Assert.NotNull(match);
        Assert.Equal(expectedRecyclable, match!.IsRecyclable);
        Assert.Equal(expectedCompostable, match.IsCompostable);
    }
}
