using Microsoft.AspNetCore.Identity;

namespace EcoNexus.Infrastructure.Identity;

/// <summary>
/// The application role. Uses Guid keys to match ApplicationUser.
/// Currently identical to the base class; kept as a distinct type so we can
/// add role-level fields (permissions, descriptions, etc.) later without a
/// breaking change.
/// </summary>
public sealed class ApplicationRole : IdentityRole<Guid>
{
    public ApplicationRole()
        : base()
    {
    }

    public ApplicationRole(string roleName)
        : base(roleName)
    {
    }
}
