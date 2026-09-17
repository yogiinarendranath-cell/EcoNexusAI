using EcoNexus.Worker;
using Serilog;

var builder = Host.CreateApplicationBuilder(args);

// ============================================================
// Logging — Serilog
// ============================================================
builder.Services.AddSerilog((services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(builder.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/econexus-worker-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

builder.Services.AddHostedService<Worker>();

var host = builder.Build();

try
{
    Log.Information("Starting EcoNexus Worker");
    host.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EcoNexus Worker terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
