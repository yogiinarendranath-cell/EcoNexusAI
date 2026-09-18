using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;

namespace EcoNexus.Infrastructure;

/// <summary>
/// Registers infrastructure-layer services: repositories and any other
/// persistence, messaging, or external-service adapters.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IWasteStationRepository, WasteStationRepository>();

        return services;
    }
}
