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
using Xunit;

namespace EcoNexus.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real EcoNexus API in-process for integration tests.
/// Each factory gets a fresh LocalDB database, migrated and role-seeded
/// at InitializeAsync time. The database is dropped only when the factory
/// itself is disposed, AFTER the host has shut down.
/// </summary>
public sealed class EcoNexusApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Serialize DB-create + migration across parallel test classes sharing the
    // same SQL Server instance. Without this, sp_getapplock contention and
    // concurrent CREATE DATABASE calls cause flaky failures.
    private static readonly SemaphoreSlim MigrationGate = new(initialCount: 1, maxCount: 1);

    private readonly string _databaseName = $"EcoNexus_Test_{Guid.NewGuid():N}";
    private bool _initialized;

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
            services.RemoveAll<IHostedService>();
            services.RemoveAll<RoleSeeder>();
        });
    }

    public async Task InitializeAsync()
    {
        if (_initialized) return;

        // Ensure the host is created before we touch Services.
        _ = Server;

        await MigrationGate.WaitAsync();
        try
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
        finally
        {
            MigrationGate.Release();
        }

        _initialized = true;
    }

    /// <summary>
    /// Drops the test database. Called only after the host is disposed,
    /// so it is safe to invoke from DisposeAsync.
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

    public new async Task DisposeAsync()
    {
        // 1. Dispose host first (stops background workers, releases DbContext pool).
        //    Wrapped so a host-disposal failure does not prevent DB cleanup.
        try
        {
            await base.DisposeAsync();
        }
        catch
        {
            // Swallow — we still want to attempt DB cleanup below.
        }

        // 2. Then drop the DB. `DropDatabaseAsync` already swallows
        //    ObjectDisposedException. Any other failure here is logged by
        //    xUnit but should not prevent the test run from completing.
        await DropDatabaseAsync();
    }
}