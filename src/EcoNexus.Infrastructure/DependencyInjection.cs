using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Abstractions.Realtime;
using EcoNexus.Infrastructure.Dispatching;
using EcoNexus.Infrastructure.Persistence.Repositories;
using EcoNexus.Infrastructure.Realtime;
using Microsoft.Extensions.DependencyInjection;

namespace EcoNexus.Infrastructure;

/// <summary>
/// Registers infrastructure-layer services: repositories, dispatchers, and
/// any other persistence, messaging, or external-service adapters.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IWasteStationRepository, WasteStationRepository>();
        services.AddScoped<ICollectionVehicleRepository, CollectionVehicleRepository>();
        services.AddScoped<ICollectionJobRepository, CollectionJobRepository>();
        services.AddScoped<IRecyclingFacilityRepository, RecyclingFacilityRepository>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IOperationsNotifier, SignalROperationsNotifier>();

        return services;
    }
}
