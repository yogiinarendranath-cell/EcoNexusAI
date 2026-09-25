using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A reward that citizens can redeem with green points.
/// Aggregate root: owns its own lifecycle (creation, activation,
/// deactivation). Reward redemption events are raised by CitizenProfile,
/// not here — the citizen holds the ledger.
/// </summary>
public sealed class Reward : AggregateRoot
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public int CostInPoints { get; private set; }
    public bool IsActive { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }

    // Required by EF Core
    private Reward()
    {
        Name = null!;
        Description = null!;
    }

    private Reward(string name, string description, int costInPoints, DateTimeOffset createdAt)
    {
        Name = ValidateName(name);
        Description = ValidateDescription(description);
        CostInPoints = ValidateCost(costInPoints);
        IsActive = true;
        CreatedAt = createdAt;
    }

    public static Reward Create(
        string name,
        string description,
        int costInPoints,
        DateTimeOffset createdAt)
        => new(name, description, costInPoints, createdAt);

    public void Deactivate()
    {
        if (!IsActive)
        {
            throw new InvalidOperationException("Reward is already inactive.");
        }
        IsActive = false;
    }

    public void Reactivate()
    {
        if (IsActive)
        {
            throw new InvalidOperationException("Reward is already active.");
        }
        IsActive = true;
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Reward name must not be empty.", nameof(name));
        }

        var trimmed = name.Trim();
        if (trimmed.Length > 120)
        {
            throw new ArgumentException(
                "Reward name must be 120 characters or fewer.",
                nameof(name));
        }

        return trimmed;
    }

    private static string ValidateDescription(string description)
    {
        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Reward description must not be empty.", nameof(description));
        }

        var trimmed = description.Trim();
        if (trimmed.Length > 1000)
        {
            throw new ArgumentException(
                "Reward description must be 1000 characters or fewer.",
                nameof(description));
        }

        return trimmed;
    }

    private static int ValidateCost(int costInPoints)
    {
        if (costInPoints <= 0)
        {
            throw new ArgumentException(
                "Reward cost must be greater than zero.",
                nameof(costInPoints));
        }

        return costInPoints;
    }
}
