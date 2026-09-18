using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace EcoNexus.Infrastructure.Persistence;

/// <summary>
/// Registers the EcoNexus DbContext against a SQL Server connection string.
/// Used by both the API and Worker so they share configuration logic.
/// </summary>
public static class PersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddEcoNexusPersistence(
        this IServiceCollection services,
        string connectionString)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        services.AddDbContext<EcoNexusDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOptions =>
                sqlOptions.MigrationsAssembly(
                    typeof(EcoNexusDbContext).Assembly.FullName)));

        return services;
    }
}
