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

        // In-memory cache used by decorators (e.g. CachedRecyclingFacilityRepository).
        services.AddMemoryCache();

        services.AddScoped<IWasteStationRepository, WasteStationRepository>();
        services.AddScoped<ICollectionVehicleRepository, CollectionVehicleRepository>();
        services.AddScoped<ICollectionJobRepository, CollectionJobRepository>();
        services.AddKeyedScoped<IRecyclingFacilityRepository, RecyclingFacilityRepository>(
            CachedRecyclingFacilityRepository.InnerServiceKey);
        services.AddScoped<IRecyclingFacilityRepository, CachedRecyclingFacilityRepository>();
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
            services
                .AddHttpClient<IWasteClassificationService, OllamaWasteClassificationService>()
                .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);

                options.Retry.MaxRetryAttempts = 2;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;

                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            services.AddScoped<IWasteClassificationService, MockWasteClassificationService>();
        }


        // ---- Operations assistant LLM ----
        // Same provider flag drives both AI subsystems.
        if (string.Equals(provider, "Ollama", StringComparison.OrdinalIgnoreCase))
        {
            services
                .AddHttpClient<IAssistantLlm, OllamaAssistantLlm>()
                .AddStandardResilienceHandler(options =>
            {
                options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(60);
                options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(20);

                options.Retry.MaxRetryAttempts = 2;
                options.Retry.BackoffType = Polly.DelayBackoffType.Exponential;
                options.Retry.UseJitter = true;

                options.CircuitBreaker.FailureRatio = 0.5;
                options.CircuitBreaker.MinimumThroughput = 5;
                options.CircuitBreaker.SamplingDuration = TimeSpan.FromSeconds(30);
                options.CircuitBreaker.BreakDuration = TimeSpan.FromSeconds(30);
            });
        }
        else
        {
            services.AddScoped<IAssistantLlm, MockAssistantLlm>();
        }

        return services;
    }
}
