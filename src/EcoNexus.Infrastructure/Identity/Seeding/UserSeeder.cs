using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoNexus.Infrastructure.Identity.Seeding;

/// <summary>
/// Hosted service that ensures the demo user accounts exist on startup.
/// Matches the credentials advertised on the frontend login page and the
/// citizen mobile app's demo hints.
///
/// Idempotent: safe to run on every boot. Existing users are skipped,
/// but missing role assignments are still added.
/// </summary>
public sealed class UserSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<UserSeeder> _logger;

    public UserSeeder(IServiceProvider serviceProvider, ILogger<UserSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        // Demo operator — CityAdmin for future ops features, Citizen so the
        // /app mobile experience works today.
        await EnsureUserAsync(
            userManager,
            email: "demo@econexus.local",
            password: "Demo!Pass123",
            displayName: "Demo Operator",
            roles: new[] { EcoNexusRoles.CityAdmin, EcoNexusRoles.Citizen });

        await EnsureUserAsync(
            userManager,
            email: "citizen@econexus.local",
            password: "Citizen!Pass123",
            displayName: "Demo Citizen",
            roles: new[] { EcoNexusRoles.Citizen });
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName,
        string[] roles)
    {
        var user = await userManager.FindByEmailAsync(email);

        if (user is null)
        {
            user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                DisplayName = displayName,
                CreatedAt = DateTimeOffset.UtcNow,
            };

            var createResult = await userManager.CreateAsync(user, password);
            if (!createResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to create {Email}: {Errors}",
                    email,
                    string.Join("; ", createResult.Errors.Select(e => e.Description)));
                return;
            }

            _logger.LogInformation("Seeded user {Email}.", email);
        }
        else
        {
            _logger.LogInformation("User {Email} already exists.", email);
        }

        // Ensure all expected roles are assigned (idempotent)
        var currentRoles = await userManager.GetRolesAsync(user);
        foreach (var role in roles)
        {
            if (currentRoles.Contains(role))
            {
                continue;
            }

            var roleResult = await userManager.AddToRoleAsync(user, role);
            if (!roleResult.Succeeded)
            {
                _logger.LogError(
                    "Failed to assign {Role} to {Email}: {Errors}",
                    role,
                    email,
                    string.Join("; ", roleResult.Errors.Select(e => e.Description)));
                continue;
            }

            _logger.LogInformation("Assigned {Role} to {Email}.", role, email);
        }
    }
}