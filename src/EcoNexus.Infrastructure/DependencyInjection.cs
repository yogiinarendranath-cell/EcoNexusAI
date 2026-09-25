using EcoNexus.Application.Abstractions.AI;
using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Application.Abstractions.Identity;
using EcoNexus.Infrastructure.Identity;
using EcoNexus.Application.Abstractions.Persistence;
using EcoNexus.Application.Abstractions.Realtime;
using EcoNexus.Infrastructure.AI;
using EcoNexus.Infrastructure.Dispatching;
using EcoNexus.Infrastructure.Persistence.Repositories;
using EcoNexus.Infrastructure.Realtime;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace EcoNexus.Infrastructure;

/// <summary>
/// Registers infrastructure-layer services: repositories, dispatchers, and
/// any other persistence, messaging, or external-service adapters.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddScoped<IWasteStationRepository, WasteStationRepository>();
        services.AddScoped<ICollectionVehicleRepository, CollectionVehicleRepository>();
        services.AddScoped<ICollectionJobRepository, CollectionJobRepository>();
        services.AddScoped<IRecyclingFacilityRepository, RecyclingFacilityRepository>();
        services.AddScoped<ICitizenProfileRepository, CitizenProfileRepository>();
        services.AddScoped<IRewardRepository, RewardRepository>();
        services.AddScoped<ICitizenReportRepository, CitizenReportRepository>();
        services.AddScoped<IAssistantInteractionRepository, AssistantInteractionRepository>();
        services.AddScoped<IUserDirectory, UserDirectory>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddSingleton<IOperationsNotifier, SignalROperationsNotifier>();

        // ---- AI waste classification ----
        // Binds the "AI" config section, then picks the provider.
        // Defaults to Mock so development works without any local model.
        services.Configure<WasteClassificationOptions>(
            configuration.GetSection(WasteClassificationOptions.SectionName));

        var provider = configuration[$"{WasteClassificationOptions.SectionName}:Provider"] ?? "Mock";

        if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IWasteClassificationService, OllamaWasteClassificationService>();
        }
        else
        {
            services.AddScoped<IWasteClassificationService, MockWasteClassificationService>();
        }


        // ---- Operations assistant LLM ----
        // Same provider flag drives both AI subsystems.
        if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            services.AddHttpClient<IAssistantLlm, OllamaAssistantLlm>();
        }
        else
        {
            services.AddScoped<IAssistantLlm, MockAssistantLlm>();
        }

        return services;
    }
}
