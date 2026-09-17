using EcoNexus.Api.Middleware;
using EcoNexus.Infrastructure.Identity;
using EcoNexus.Infrastructure.Identity.Seeding;
using EcoNexus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
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
// Database — EF Core
// ============================================================
var connectionString = builder.Configuration.GetConnectionString("Default")
    ?? throw new InvalidOperationException("Connection string 'Default' is not configured.");

builder.Services.AddDbContext<EcoNexusDbContext>(options =>
    options.UseSqlServer(connectionString, sqlOptions =>
    {
        sqlOptions.MigrationsAssembly(typeof(EcoNexusDbContext).Assembly.FullName);
        sqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorNumbersToAdd: null);
    }));

// ============================================================
// Data Protection (required by Identity token providers)
// ============================================================
builder.Services.AddDataProtection();

// ============================================================
// Identity — JWT-friendly registration (no cookie handlers)
// ============================================================
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        // Password policy
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 4;

        // User policy
        options.User.RequireUniqueEmail = true;

        // Lockout policy
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;

        // Sign-in policy
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<EcoNexusDbContext>()
    .AddDefaultTokenProviders();

// ============================================================
// Hosted services
// ============================================================
builder.Services.AddHostedService<RoleSeeder>();

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
