using EcoNexus.Application.Abstractions.Identity;
using Microsoft.AspNetCore.Identity;

namespace EcoNexus.Infrastructure.Identity;

/// <summary>
/// Reads display names from the ASP.NET Core Identity user store.
/// Registered as a scoped service (UserManager is scoped).
/// </summary>
internal sealed class UserDirectory : IUserDirectory
{
    private readonly UserManager<ApplicationUser> _userManager;

    public UserDirectory(UserManager<ApplicationUser> userManager)
    {
        _userManager = userManager;
    }

    public async Task<string?> GetDisplayNameAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var user = await _userManager.FindByIdAsync(userId.ToString());
        return user?.DisplayName;
    }
}
