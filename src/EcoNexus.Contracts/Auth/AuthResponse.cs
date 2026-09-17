namespace EcoNexus.Contracts.Auth;

/// <summary>
/// Response body for successful login or refresh. Contains both the JWT access
/// token and the opaque refresh token, along with their expiry timestamps.
/// </summary>
public sealed record AuthResponse(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    UserSummary User);

/// <summary>Minimal user info returned alongside the tokens.</summary>
public sealed record UserSummary(
    Guid Id,
    string Email,
    string DisplayName,
    string[] Roles);
