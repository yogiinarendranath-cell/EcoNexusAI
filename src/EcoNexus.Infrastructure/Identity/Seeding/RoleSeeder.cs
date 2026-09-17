using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EcoNexus.Infrastructure.Identity.Seeding;

/// <summary>
/// Hosted service that ensures the canonical EcoNexus roles exist in the
/// AspNetRoles table on application startup. Idempotent: safe to run on every
/// boot. Logs what it creates.
/// </summary>
public sealed class RoleSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<RoleSeeder> _logger;

    public RoleSeeder(IServiceProvider serviceProvider, ILogger<RoleSeeder> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();

        foreach (var roleName in EcoNexusRoles.All)
        {
            var exists = await roleManager.RoleExistsAsync(roleName);
            if (exists)
            {
                continue;
            }

            var result = await roleManager.CreateAsync(new ApplicationRole(roleName));

            if (result.Succeeded)
            {
                _logger.LogInformation("Created role {RoleName}", roleName);
            }
            else
            {
                var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                _logger.LogError("Failed to create role {RoleName}: {Errors}", roleName, errors);
            }
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
