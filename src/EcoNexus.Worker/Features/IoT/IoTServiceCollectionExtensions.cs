
namespace EcoNexus.Worker.Features.IoT;

/// <summary>
/// Registers the IoT simulator: options, reading generator, and background worker.
/// </summary>
public static class IoTServiceCollectionExtensions
{
    public static IServiceCollection AddIotSimulator(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<StationSimulatorOptions>(
            configuration.GetSection("IoT"));

        services.AddSingleton<ReadingGenerator>();
        services.AddHostedService<StationSimulatorWorker>();

        return services;
    }
}
