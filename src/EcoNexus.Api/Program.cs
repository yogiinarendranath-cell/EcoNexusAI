using EcoNexus.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Logging — Serilog
// ============================================================
// Replaces the default Microsoft logger. Structured logs to
// console and rolling files.
builder.Host.UseSerilog((context, services, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .WriteTo.Console()
        .WriteTo.File(
            path: "logs/econexus-api-.log",
            rollingInterval: RollingInterval.Day,
            retainedFileCountLimit: 30);
});

// ============================================================
// Services
// ============================================================

builder.Services.AddControllers();

builder.Services.AddOpenApi();

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

var app = builder.Build();

// ============================================================
// HTTP Request Pipeline
// ============================================================

app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

try
{
    Log.Information("Starting EcoNexus API");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "EcoNexus API terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
