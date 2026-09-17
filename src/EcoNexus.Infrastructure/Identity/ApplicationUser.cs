using Microsoft.AspNetCore.Identity;

namespace EcoNexus.Infrastructure.Identity;

/// <summary>
/// The application user. Extends the framework's IdentityUser with fields
/// specific to EcoNexus. Uses Guid keys for consistency with our domain entities.
/// </summary>
public sealed class ApplicationUser : IdentityUser<Guid>
{
    /// <summary>
    /// Human-readable display name (e.g. "Narendra Nath"). Shown in the UI.
    /// </summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// The tenant (city/organization) this user belongs to.
    /// Null for SuperAdmin — they operate across all tenants.
    /// </summary>
    public Guid? TenantId { get; set; }

    /// <summary>
    /// UTC timestamp when the user account was created.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }

    /// <summary>
    /// UTC timestamp of the last successful login, or null if never logged in.
    /// </summary>
    public DateTimeOffset? LastLoginAt { get; set; }
}
