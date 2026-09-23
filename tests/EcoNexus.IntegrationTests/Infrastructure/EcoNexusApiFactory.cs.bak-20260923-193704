using EcoNexus.Infrastructure.Identity;
using EcoNexus.Infrastructure.Identity.Seeding;
using EcoNexus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace EcoNexus.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real EcoNexus API in-process for integration tests.
/// Each test class instance gets a fresh LocalDB database that is migrated
/// and role-seeded at InitializeAsync time.
/// </summary>
public sealed class EcoNexusApiFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"EcoNexus_Test_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.UseEnvironment("Testing");

        builder.UseSetting(
            "ConnectionStrings:Default",
            $"Server=(localdb)\\MSSQLLocalDB;Database={_databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True");

        builder.UseSetting("Jwt:Issuer", "EcoNexus.Api.Tests");
        builder.UseSetting("Jwt:Audience", "EcoNexus.Client.Tests");
        builder.UseSetting("Jwt:SecretKey", "IntegrationTest-Only-Secret-Key-That-Is-Long-Enough-For-HmacSha256");
        builder.UseSetting("Jwt:AccessTokenMinutes", "15");
        builder.UseSetting("Jwt:RefreshTokenDays", "7");

        builder.ConfigureServices(services =>
        {
            // Remove the production RoleSeeder — it would try to seed roles
            // *during host startup*, before our migrations have run. We seed
            // roles explicitly in InitializeAsync, after the schema exists.
            services.RemoveAll<IHostedService>();
            services.RemoveAll<RoleSeeder>();
        });
    }

    /// <summary>
    /// Creates the test database, applies migrations, and seeds roles.
    /// </summary>
    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();

        // 1. Apply migrations
        var db = scope.ServiceProvider.GetRequiredService<EcoNexusDbContext>();
        await db.Database.MigrateAsync();

        // 2. Seed the 7 canonical roles
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ApplicationRole>>();
        foreach (var roleName in EcoNexusRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ApplicationRole(roleName));
            }
        }
    }

    /// <summary>
    /// Drops the test database.
    /// </summary>
    public async Task DropDatabaseAsync()
    {
        try
        {
            using var scope = Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<EcoNexusDbContext>();
            await db.Database.EnsureDeletedAsync();
        }
        catch (ObjectDisposedException)
        {
            // Host already disposed; nothing to clean up.
        }
    }
}
