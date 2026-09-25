using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.Enums;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A single immutable entry in a citizen's green points ledger.
/// Child entity of the CitizenProfile aggregate. Balance is derived
/// from the sum of SignedDelta across all transactions.
/// </summary>
public sealed class GreenPointTransaction : Entity
{
    public Guid CitizenProfileId { get; private set; }
    public int SignedDelta { get; private set; }
    public GreenPointSource Source { get; private set; }
    public GreenPointReason Reason { get; private set; }
    public string Description { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>Optional reference to a domain object (reward id, station id, etc.).</summary>
    public Guid? RelatedEntityId { get; private set; }

    // Required by EF Core
    private GreenPointTransaction()
    {
        Description = null!;
    }

    internal GreenPointTransaction(
        Guid citizenProfileId,
        int signedDelta,
        GreenPointSource source,
        GreenPointReason reason,
        string description,
        DateTimeOffset occurredAt,
        Guid? relatedEntityId = null)
    {
        if (citizenProfileId == Guid.Empty)
        {
            throw new ArgumentException("CitizenProfileId must not be empty.", nameof(citizenProfileId));
        }

        if (signedDelta == 0)
        {
            throw new ArgumentException("Transaction delta must not be zero.", nameof(signedDelta));
        }

        if (string.IsNullOrWhiteSpace(description))
        {
            throw new ArgumentException("Description must not be empty.", nameof(description));
        }

        if (description.Length > 500)
        {
            throw new ArgumentException(
                "Description must be 500 characters or fewer.",
                nameof(description));
        }

        CitizenProfileId = citizenProfileId;
        SignedDelta = signedDelta;
        Source = source;
        Reason = reason;
        Description = description.Trim();
        OccurredAt = occurredAt;
        RelatedEntityId = relatedEntityId;
    }
}
