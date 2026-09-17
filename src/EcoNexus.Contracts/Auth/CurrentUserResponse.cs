namespace EcoNexus.Contracts.Auth;

/// <summary>
/// Response body for GET /api/v1/auth/me.
/// </summary>
public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string DisplayName,
    string[] Roles);
