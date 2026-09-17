namespace EcoNexus.Application.Abstractions.Identity;

/// <summary>
/// A pair of tokens issued to an authenticated user.
/// AccessToken is a signed JWT used as a Bearer token.
/// RefreshToken is an opaque random string stored hashed on the server.
/// </summary>
public sealed record TokenPair(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt);
