namespace EcoNexus.Application.Abstractions.Identity;

/// <summary>
/// Read-only access to Identity user data (display name, email) for cases
/// where the JWT does not carry that information.
/// </summary>
public interface IUserDirectory
{
    /// <summary>Returns the display name for a user, or null if not found.</summary>
    Task<string?> GetDisplayNameAsync(Guid userId, CancellationToken cancellationToken = default);
}
