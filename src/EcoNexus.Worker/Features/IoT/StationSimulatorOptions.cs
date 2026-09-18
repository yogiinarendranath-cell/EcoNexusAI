namespace EcoNexus.Worker.Features.IoT;

/// <summary>
/// Configuration for the synthetic IoT sensor simulator.
/// Bound from the "IoT" section of appsettings.json.
/// </summary>
public sealed class StationSimulatorOptions
{
    /// <summary>Whether the simulator runs at all.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>How often each simulation cycle runs, in seconds.</summary>
    public int TickIntervalSeconds { get; set; } = 10;

    /// <summary>How many randomly-chosen active stations receive a reading per tick.</summary>
    public int ReadingsPerTick { get; set; } = 5;
}
