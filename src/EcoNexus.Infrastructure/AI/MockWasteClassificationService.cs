using System.Security.Cryptography;
using System.Text;
using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Infrastructure.AI;

/// <summary>
/// Deterministic, no-I/O implementation of IWasteClassificationService.
/// Same image URL always produces the same classification — this makes
/// it reliable for tests, demos, and offline development.
///
/// Uses a stable hash of the image URL to pick a category and confidence.
/// The mapping from category to (recyclable, compostable, instruction)
/// matches what a real vision model would return.
/// </summary>
internal sealed class MockWasteClassificationService : IWasteClassificationService
{
    public string ProviderName => "Mock";

    public Task<WasteClassificationResult> ClassifyAsync(
        string imageUrl,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(imageUrl);

        var hash = StableHash(imageUrl);
        var category = PickCategory(hash);
        var confidence = 0.75 + (Math.Abs(hash) % 24) / 100.0;  // 0.75 – 0.98

        var result = BuildResult(category, confidence);

        // Small artificial delay so callers can exercise async paths
        // without a 5 ms return.
        return Task.FromResult(result);
    }

    private static int StableHash(string input)
    {
        var bytes = Encoding.UTF8.GetBytes(input);
        var hash = SHA256.HashData(bytes);
        // Take the first 4 bytes as an unsigned int
        return (hash[0] << 24) | (hash[1] << 16) | (hash[2] << 8) | hash[3];
    }

    private static WasteCategory PickCategory(int hash)
    {
        var categories = new[]
        {
            WasteCategory.Organic,
            WasteCategory.Plastic,
            WasteCategory.Paper,
            WasteCategory.Glass,
            WasteCategory.Metal,
            WasteCategory.EWaste,
            WasteCategory.General,
            WasteCategory.Hazardous,
        };
        var index = Math.Abs(hash) % categories.Length;
        return categories[index];
    }

    private static WasteClassificationResult BuildResult(
        WasteCategory category,
        double confidence)
    {
        return category switch
        {
            WasteCategory.Organic => new(category, confidence, false, true,
                "Place in the organic / compostable bin."),

            WasteCategory.Plastic => new(category, confidence, true, false,
                "Rinse and place in the recyclable plastics compartment."),

            WasteCategory.Paper => new(category, confidence, true, true,
                "Keep dry and place in the paper recycling bin."),

            WasteCategory.Glass => new(category, confidence, true, false,
                "Rinse and place in the glass recycling container."),

            WasteCategory.Metal => new(category, confidence, true, false,
                "Rinse and place in the metals recycling bin."),

            WasteCategory.EWaste => new(category, confidence, true, false,
                "Take to an e-waste drop-off point — do not place in general waste."),

            WasteCategory.Hazardous => new(category, confidence, false, false,
                "Hazardous material. Take to a designated hazardous waste facility."),

            WasteCategory.General => new(category, confidence, false, false,
                "Place in the general waste bin."),

            _ => new(WasteCategory.General, confidence, false, false,
                "Place in the general waste bin."),
        };
    }
}
