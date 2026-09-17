using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Domain.Entities;

/// <summary>
/// A refresh token issued to an authenticated user. The raw token is never
/// stored — only its SHA-256 hash. Supports rotation: when a refresh token is
/// used, the old one is marked replaced, and if a revoked token is presented
/// again, the whole chain can be invalidated (the "token family" pattern).
/// </summary>
public sealed class RefreshToken : AggregateRoot
{
    public Guid UserId { get; private set; }
    public string TokenHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset ExpiresAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public string? ReplacedByTokenHash { get; private set; }

    /// <summary>True if the token can still be used.</summary>
    public bool IsActive(DateTimeOffset now) =>
        RevokedAt is null && now < ExpiresAt;

    // Required by EF Core
    private RefreshToken()
    {
        TokenHash = null!;
    }

    private RefreshToken(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAt,
        DateTimeOffset expiresAt)
    {
        UserId = userId;
        TokenHash = tokenHash;
        CreatedAt = createdAt;
        ExpiresAt = expiresAt;
    }

    /// <summary>
    /// Issues a new refresh token record. The caller is responsible for
    /// generating a cryptographically random raw token, hashing it, and
    /// giving us only the hash.
    /// </summary>
    public static RefreshToken Issue(
        Guid userId,
        string tokenHash,
        DateTimeOffset createdAt,
        TimeSpan lifetime)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("UserId must not be empty.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("TokenHash must not be empty.", nameof(tokenHash));
        }

        if (lifetime <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(lifetime), "Lifetime must be positive.");
        }

        var expiresAt = createdAt.Add(lifetime);
        return new RefreshToken(userId, tokenHash, createdAt, expiresAt);
    }

    /// <summary>
    /// Revokes this token. Idempotent: repeated calls are a no-op.
    /// </summary>
    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = now;
    }

    /// <summary>
    /// Marks this token as replaced by a newer one during rotation.
    /// Also revokes this token.
    /// </summary>
    public void MarkReplaced(string replacementTokenHash, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(replacementTokenHash))
        {
            throw new ArgumentException("Replacement token hash must not be empty.", nameof(replacementTokenHash));
        }

        ReplacedByTokenHash = replacementTokenHash;
        Revoke(now);
    }
}
