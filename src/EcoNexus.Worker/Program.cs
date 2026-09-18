using EcoNexus.Application;
using EcoNexus.Infrastructure;
using EcoNexus.Infrastructure.Persistence;
using EcoNexus.Worker.Features.IoT;
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

// ============================================================
// Persistence — SQL Server via shared extension
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddEcoNexusPersistence(connectionString);

// ============================================================
// Application + Infrastructure
// ============================================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure();

// ============================================================
// IoT Simulator
// ============================================================
builder.Services.AddIotSimulator(builder.Configuration);

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
