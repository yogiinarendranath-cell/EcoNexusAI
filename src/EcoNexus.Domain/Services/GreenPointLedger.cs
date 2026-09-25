using EcoNexus.Domain.Entities;

namespace EcoNexus.Domain.Services;

/// <summary>
/// Pure domain service for reading a citizen's green points ledger.
/// Keeps aggregation logic out of both the entity and the application
/// layer — everything that reads the ledger goes through here.
/// </summary>
public static class GreenPointLedger
{
    /// <summary>
    /// Returns the citizen's current balance. Derived from the ledger;
    /// never stored.
    /// </summary>
    public static int Balance(CitizenProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.GreenPointsBalance;
    }

    /// <summary>
    /// Returns the total points earned (sum of positive deltas).
    /// </summary>
    public static int TotalEarned(CitizenProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.Transactions
            .Where(t => t.SignedDelta > 0)
            .Sum(t => t.SignedDelta);
    }

    /// <summary>
    /// Returns the total points redeemed (sum of negative deltas, as a
    /// positive number).
    /// </summary>
    public static int TotalRedeemed(CitizenProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return -profile.Transactions
            .Where(t => t.SignedDelta < 0)
            .Sum(t => t.SignedDelta);
    }

    /// <summary>
    /// Returns the number of transactions in the ledger.
    /// </summary>
    public static int TransactionCount(CitizenProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return profile.Transactions.Count;
    }
}
