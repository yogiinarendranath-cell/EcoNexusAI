namespace EcoNexus.Domain.Enums;

/// <summary>
/// Direction of a green points transaction: earning, spending, or an
/// administrative correction. SignedDelta on the transaction carries
/// the actual sign; this enum is for filtering and reporting.
/// </summary>
public enum GreenPointSource
{
    Earned = 0,
    Redeemed = 1,
    Adjusted = 2
}
