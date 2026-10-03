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
/// Idempotent: safe to run on every boot. Existing users are skipped.
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

        await EnsureUserAsync(
            userManager,
            email: "demo@econexus.local",
            password: "Demo!Pass123",
            displayName: "Demo Operator",
            role: EcoNexusRoles.CityAdmin);

        await EnsureUserAsync(
            userManager,
            email: "citizen@econexus.local",
            password: "Citizen!Pass123",
            displayName: "Demo Citizen",
            role: EcoNexusRoles.Citizen);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private async Task EnsureUserAsync(
        UserManager<ApplicationUser> userManager,
        string email,
        string password,
        string displayName,
        string role)
    {
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            _logger.LogInformation("User {Email} already exists — skipping.", email);
            return;
        }

        var user = new ApplicationUser
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

        var roleResult = await userManager.AddToRoleAsync(user, role);
        if (!roleResult.Succeeded)
        {
            _logger.LogError(
                "Failed to assign {Role} to {Email}: {Errors}",
                role,
                email,
                string.Join("; ", roleResult.Errors.Select(e => e.Description)));
            return;
        }

        _logger.LogInformation("Seeded user {Email} with role {Role}.", email, role);
    }
}