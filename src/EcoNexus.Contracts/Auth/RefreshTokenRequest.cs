namespace EcoNexus.Contracts.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/refresh and POST /api/v1/auth/logout.
/// </summary>
public sealed record RefreshTokenRequest(string RefreshToken);
