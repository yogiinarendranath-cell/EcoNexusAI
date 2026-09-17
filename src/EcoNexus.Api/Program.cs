using EcoNexus.Api.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// ============================================================
// Logging — Serilog
// ============================================================
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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new Microsoft.OpenApi.OpenApiInfo
    {
        Title = "EcoNexus AI API",
        Version = "v1",
        Description = "AI-Powered Smart Waste & Recycling Network"
    });
});

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
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "EcoNexus AI API v1");
        options.RoutePrefix = "swagger";
    });
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// Versioned ping endpoint — template for /api/v1/* routes
app.MapGet("/api/v1/ping", () => Results.Ok(new
{
    service = "EcoNexus.Api",
    version = "v1",
    pong = true,
    timestamp = DateTimeOffset.UtcNow
}))
.WithName("PingV1")
.WithTags("System");

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
