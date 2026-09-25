using System.Reflection;
using EcoNexus.Application.Behaviors;
using FluentValidation;
using EcoNexus.Application.Features.Operations.Assistant.Tools;
using Microsoft.Extensions.DependencyInjection;

namespace EcoNexus.Application;

/// <summary>
/// Registers all application-layer services with the DI container:
/// MediatR handlers, FluentValidation validators, and MediatR pipeline behaviors.
/// Call this once from Program.cs via builder.Services.AddApplication().
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        var assembly = Assembly.GetExecutingAssembly();

        // Register all MediatR handlers in this assembly.
        services.AddMediatR(config =>
        {
            config.RegisterServicesFromAssembly(assembly);

            // Order matters: outer behaviors run first.
            // Logging wraps validation so failures are still logged with the request.
            config.AddOpenBehavior(typeof(LoggingBehavior<,>));
            config.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });

        // Register all FluentValidation validators in this assembly.
        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);

        // ---- Operations assistant ----
        // Register every tool. The IToolRegistry aggregates them.
        services.AddScoped<IAssistantTool, GetCriticalStationsTool>();
        services.AddScoped<IAssistantTool, GetStationCountTool>();
        services.AddScoped<IAssistantTool, GetRecyclingMetricsTool>();
        services.AddScoped<IAssistantTool, GetActiveFacilitiesTool>();
        services.AddScoped<IAssistantTool, GetRecentJobsTool>();
        services.AddScoped<IAssistantTool, GetActiveVehiclesTool>();
        services.AddScoped<IAssistantTool, GetWasteByCategoryTool>();
        services.AddScoped<IAssistantTool, GetFacilityOverviewTool>();
        services.AddScoped<IToolRegistry, ToolRegistry>();

        return services;
    }
}
