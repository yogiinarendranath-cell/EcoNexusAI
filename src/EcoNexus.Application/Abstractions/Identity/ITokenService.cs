namespace EcoNexus.Application.Abstractions.Identity;

/// <summary>
/// Issues and validates JWT access tokens and their associated refresh tokens.
/// Implemented by EcoNexus.Infrastructure.Identity.JwtTokenService.
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Issues a fresh access/refresh token pair for the given user.
    /// </summary>
    Task<TokenPair> IssueTokenPairAsync(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a refresh token and, if valid, rotates it: the old token is
    /// revoked and a new access/refresh pair is returned.
    /// </summary>
    Task<TokenPair> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a refresh token (used on logout).
    /// </summary>
    Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default);
}
