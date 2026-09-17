namespace EcoNexus.Contracts.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/login.
/// </summary>
public sealed record LoginRequest(
    string Email,
    string Password);
