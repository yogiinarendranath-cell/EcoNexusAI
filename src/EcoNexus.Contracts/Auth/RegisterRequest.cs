namespace EcoNexus.Contracts.Auth;

/// <summary>
/// Request body for POST /api/v1/auth/register.
/// </summary>
public sealed record RegisterRequest(
    string Email,
    string Password,
    string DisplayName);
