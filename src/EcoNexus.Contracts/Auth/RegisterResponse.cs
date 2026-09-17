namespace EcoNexus.Contracts.Auth;

/// <summary>
/// Response body for POST /api/v1/auth/register.
/// </summary>
public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string DisplayName,
    string[] Roles);
