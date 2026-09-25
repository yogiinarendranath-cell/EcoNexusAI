using EcoNexus.Domain.Abstractions;
using EcoNexus.Domain.DomainEvents;
using EcoNexus.Domain.Enums;
using EcoNexus.Domain.ValueObjects;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A citizen's profile in the platform: their green points ledger,
/// streak, and preferences. Aggregate root: owns its transactions.
/// Balance is derived from the ledger — never stored as a counter.
/// </summary>
public sealed class CitizenProfile : AggregateRoot
{
    private readonly List<GreenPointTransaction> _transactions = new();
    private readonly List<WasteClassification> _classifications = new();

    public Guid UserId { get; private set; }
    public string DisplayName { get; private set; }
    public string? HomeAddress { get; private set; }
    public double? HomeLatitude { get; private set; }
    public double? HomeLongitude { get; private set; }
    public int CurrentStreakDays { get; private set; }
    public DateOnly? LastVisitDate { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastUpdatedAt { get; private set; }

    /// <summary>Immutable ledger of all point movements for this citizen.</summary>
    public IReadOnlyCollection<GreenPointTransaction> Transactions => _transactions.AsReadOnly();

    /// <summary>All waste-classification events requested by this citizen, newest first.</summary>
    public IReadOnlyCollection<WasteClassification> Classifications => _classifications.AsReadOnly();

    /// <summary>Derived balance. Never stored.</summary>
    public int GreenPointsBalance => _transactions.Sum(t => t.SignedDelta);

    // Required by EF Core
    private CitizenProfile()
    {
        DisplayName = null!;
    }

    private CitizenProfile(Guid userId, string displayName, DateTimeOffset createdAt)
    {
        UserId = userId;
        DisplayName = ValidateDisplayName(displayName);
        CurrentStreakDays = 0;
        CreatedAt = createdAt;
        LastUpdatedAt = createdAt;
    }

    public static CitizenProfile Create(
        Guid userId,
        string displayName,
        DateTimeOffset createdAt)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        }

        return new CitizenProfile(userId, displayName, createdAt);
    }

    /// <summary>
    /// Sets or updates the citizen's home location. Used for the
    /// "nearby stations" feature.
    /// </summary>
    public void SetHomeLocation(
        string? address,
        double? latitude,
        double? longitude,
        DateTimeOffset at)
    {
        if (latitude is < -90 or > 90)
        {
            throw new ArgumentException(
                "Latitude must be between -90 and 90.",
                nameof(latitude));
        }

        if (longitude is < -180 or > 180)
        {
            throw new ArgumentException(
                "Longitude must be between -180 and 180.",
                nameof(longitude));
        }

        HomeAddress = address?.Trim();
        HomeLatitude = latitude;
        HomeLongitude = longitude;
        LastUpdatedAt = at;
    }

    /// <summary>
    /// Checks whether the citizen is allowed to log a visit to the
    /// given station on the given date. Rate limit: one visit per
    /// station per calendar day.
    /// </summary>
    public bool CanVisitStationOn(Guid stationId, DateOnly date)
    {
        return !_transactions.Any(t =>
            t.Reason == GreenPointReason.StationVisit &&
            t.RelatedEntityId == stationId &&
            DateOnly.FromDateTime(t.OccurredAt.Date) == date);
    }

    /// <summary>
    /// Records a station visit. Enforces the one-visit-per-station-per-day
    /// rate limit as a domain invariant. Advances the daily streak.
    /// </summary>
    public GreenPointTransaction RecordStationVisit(
        Guid stationId,
        DateOnly date,
        int points,
        DateTimeOffset occurredAt)
    {
        if (!CanVisitStationOn(stationId, date))
        {
            throw new InvalidOperationException(
                $"Citizen already logged a visit to station '{stationId}' on {date}.");
        }

        var tx = new GreenPointTransaction(
            Id,
            signedDelta: points,
            source: GreenPointSource.Earned,
            reason: GreenPointReason.StationVisit,
            description: $"Visit to station {stationId}",
            occurredAt: occurredAt,
            relatedEntityId: stationId);

        _transactions.Add(tx);
        LastUpdatedAt = occurredAt;

        AdvanceStreak(date);
        RaiseDomainEvent(new GreenPointsEarnedEvent(
            Id,
            points,
            GreenPointReason.StationVisit,
            occurredAt));

        return tx;
    }

    /// <summary>
    /// Awards points for a filed report that has been verified.
    /// No rate limit — reports are already limited at file time.
    /// </summary>
    public GreenPointTransaction AwardReportPoints(
        Guid reportId,
        int points,
        DateTimeOffset occurredAt)
    {
        if (points <= 0)
        {
            throw new ArgumentException(
                "Report award must be positive.",
                nameof(points));
        }

        var tx = new GreenPointTransaction(
            Id,
            signedDelta: points,
            source: GreenPointSource.Earned,
            reason: GreenPointReason.ReportFiled,
            description: $"Reward for report {reportId}",
            occurredAt: occurredAt,
            relatedEntityId: reportId);

        _transactions.Add(tx);
        LastUpdatedAt = occurredAt;

        RaiseDomainEvent(new GreenPointsEarnedEvent(
            Id,
            points,
            GreenPointReason.ReportFiled,
            occurredAt));

        return tx;
    }

    /// <summary>
    /// Redeems a reward if the citizen has sufficient balance.
    /// Enforces the non-negative-balance invariant.
    /// </summary>
    public GreenPointTransaction RedeemReward(
        Reward reward,
        DateTimeOffset occurredAt)
    {
        ArgumentNullException.ThrowIfNull(reward);

        if (!reward.IsActive)
        {
            throw new InvalidOperationException(
                $"Reward '{reward.Name}' is not currently available.");
        }

        if (GreenPointsBalance < reward.CostInPoints)
        {
            throw new InvalidOperationException(
                $"Insufficient balance. Required: {reward.CostInPoints}, available: {GreenPointsBalance}.");
        }

        var tx = new GreenPointTransaction(
            Id,
            signedDelta: -reward.CostInPoints,
            source: GreenPointSource.Redeemed,
            reason: GreenPointReason.RewardRedemption,
            description: $"Redeemed: {reward.Name}",
            occurredAt: occurredAt,
            relatedEntityId: reward.Id);

        _transactions.Add(tx);
        LastUpdatedAt = occurredAt;

        RaiseDomainEvent(new RewardRedeemedEvent(
            Id,
            reward.Id,
            reward.Name,
            reward.CostInPoints,
            occurredAt));

        return tx;
    }

    /// <summary>
    /// Records a waste-classification event. No rate limit on the domain
    /// side — the API layer enforces per-user throttling.
    /// </summary>
    public WasteClassification RecordClassification(
        WasteClassificationResult result,
        string imageReference,
        string providerName,
        DateTimeOffset classifiedAt)
    {
        var classification = new WasteClassification(
            Id,
            result,
            imageReference,
            providerName,
            classifiedAt);

        _classifications.Add(classification);
        LastUpdatedAt = classifiedAt;

        return classification;
    }

    private void AdvanceStreak(DateOnly today)
    {
        if (LastVisitDate is null)
        {
            CurrentStreakDays = 1;
            LastVisitDate = today;
            return;
        }

        var daysSince = today.DayNumber - LastVisitDate.Value.DayNumber;

        if (daysSince == 0)
        {
            // Same day — streak unchanged
            return;
        }

        if (daysSince == 1)
        {
            CurrentStreakDays += 1;
            LastVisitDate = today;
            return;
        }

        // Gap of more than one day — reset
        CurrentStreakDays = 1;
        LastVisitDate = today;
    }

    private static string ValidateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new ArgumentException("DisplayName must not be empty.", nameof(displayName));
        }

        var trimmed = displayName.Trim();
        if (trimmed.Length > 120)
        {
            throw new ArgumentException(
                "DisplayName must be 120 characters or fewer.",
                nameof(displayName));
        }

        return trimmed;
    }
}
