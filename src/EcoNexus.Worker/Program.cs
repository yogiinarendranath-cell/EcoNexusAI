using EcoNexus.Application;
using EcoNexus.Infrastructure;
using EcoNexus.Infrastructure.Identity;
using EcoNexus.Infrastructure.Persistence;
using EcoNexus.Worker.Features.IoT;
using Microsoft.AspNetCore.Identity;
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
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================
// Shared dependencies required by Application-layer MediatR
// handlers pulled in via AddApplication(). The Worker does not
// host HTTP or SignalR hubs, but the handlers it dispatches
// (and the ones it merely registers) require these services.
// ============================================================
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddDataProtection();
builder.Services.AddSignalR();

builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.User.RequireUniqueEmail = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<EcoNexusDbContext>()
    .AddDefaultTokenProviders();

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