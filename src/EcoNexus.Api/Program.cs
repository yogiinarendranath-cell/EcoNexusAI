using System.Text;
using EcoNexus.Infrastructure.Realtime;
using EcoNexus.Api.Middleware;
using EcoNexus.Api.Extensions;
using EcoNexus.Api.HealthChecks;
using EcoNexus.Api.Options;
using Asp.Versioning;
using EcoNexus.Infrastructure.Observability;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using EcoNexus.Application;
using EcoNexus.Application.Abstractions.Identity;
using EcoNexus.Infrastructure;
using EcoNexus.Infrastructure.Identity;
using EcoNexus.Infrastructure.Identity.Seeding;
using EcoNexus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using Serilog;
using Serilog.Enrichers.Span;

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
        .Enrich.WithSpan()
        .Enrich.WithProperty("service", "EcoNexus.Api");
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
// Time abstraction
// ============================================================
builder.Services.AddSingleton(TimeProvider.System);

// ============================================================
// Data Protection (required by Identity token providers)
// ============================================================
builder.Services.AddDataProtection();

// ============================================================
// JWT settings binding
// ============================================================
builder.Services
    .AddOptions<JwtSettings>()
    .Bind(builder.Configuration.GetSection(JwtSettings.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

// ============================================================
// Identity — JWT-friendly registration (no cookie handlers)
// ============================================================
builder.Services
    .AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredUniqueChars = 4;
        options.User.RequireUniqueEmail = true;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.AllowedForNewUsers = true;
        options.SignIn.RequireConfirmedEmail = false;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<EcoNexusDbContext>()
    .AddSignInManager()
    .AddDefaultTokenProviders();

// ============================================================
// JWT Bearer authentication
// ============================================================
var jwtSettings = builder.Configuration.GetSection(JwtSettings.SectionName).Get<JwtSettings>()
    ?? throw new InvalidOperationException("JWT settings are not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.RequireHttpsMetadata = false;
        options.SaveToken = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSettings.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtSettings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            RoleClaimType = "role",
            NameClaimType = "sub"
        };
    });

builder.Services.AddAuthorization();

// ============================================================
// Application services
// ============================================================
builder.Services.AddScoped<ITokenService, JwtTokenService>();

// ============================================================
// Application + Infrastructure layers
// ============================================================
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// ============================================================
// SignalR
// ============================================================
builder.Services.AddSignalR();

// ============================================================
// Hosted services
// ============================================================
builder.Services.AddHostedService<RoleSeeder>();

// ============================================================
// Controllers + Swagger
// ============================================================
// ============================================================
// API versioning — URL-segment based (v1, v2, ...).
//
// The version segment is REQUIRED by the route template:
//   [Route("api/v{version:apiVersion}/...")]
// So /api/v1/stations is canonical; /api/stations returns 404.
//
// AssumeDefaultVersionWhenUnspecified is kept true so any future
// controller with an unversioned template (e.g. HealthController)
// will still resolve to v1.0.
//
// Supported versions are reported via the "api-supported-versions"
// response header.
// ============================================================
builder.Services
    .AddApiVersioning(options =>
    {
        options.DefaultApiVersion = new ApiVersion(1, 0);
        options.AssumeDefaultVersionWhenUnspecified = true;
        options.ReportApiVersions = true;
        options.ApiVersionReader = new UrlSegmentApiVersionReader();
    })
    .AddApiExplorer(options =>
    {
        options.GroupNameFormat = "'v'V";
        options.SubstituteApiVersionInUrl = true;
    });

builder.Services.AddControllers();

// ============================================================
// Output caching (HTTP-level cache for idempotent reads)
// ============================================================
builder.Services.AddOutputCache(options =>
{
    options.AddPolicy("StationsList", policy =>
    {
        policy
            .Expire(TimeSpan.FromSeconds(30))
            .SetVaryByQuery("*");
    });

    options.AddPolicy("FacilitiesList", policy =>
    {
        policy.Expire(TimeSpan.FromSeconds(60));
    });
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "EcoNexus AI API",
        Version = "v1",
        Description = "AI-Powered Smart Waste & Recycling Network"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste your JWT access token here. Swagger will send it as `Authorization: Bearer {token}`."
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document, null)] = new List<string>()
    });
});

builder.Services.AddOpenApi();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

// ============================================================
// CORS — origins are configuration-driven (see "Cors" section in appsettings).
// Development origins live in appsettings.Development.json.
// Production origins must be supplied via environment variables
// (e.g. Cors__AllowedOrigins__0=https://econexus.example.com).
// ============================================================
const string CorsPolicy = "EcoNexusCors";

builder.Services
    .AddOptions<CorsOptions>()
    .Bind(builder.Configuration.GetSection(CorsOptions.SectionName))
    .ValidateDataAnnotations()
    .ValidateOnStart();

var corsOptions = builder.Configuration
    .GetSection(CorsOptions.SectionName)
    .Get<CorsOptions>() ?? new CorsOptions();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CorsPolicy, policy =>
    {
        if (corsOptions.AllowAnyOrigin)
        {
            policy.AllowAnyOrigin();
        }
        else
        {
            policy.WithOrigins(corsOptions.AllowedOrigins);
        }

        policy.AllowAnyHeader().AllowAnyMethod();

        if (corsOptions.AllowCredentials && !corsOptions.AllowAnyOrigin)
        {
            policy.AllowCredentials();
        }
    });
});
// ============================================================
// Rate limiting (per-endpoint policies; endpoints opt in)
// ============================================================
builder.Services.AddEcoNexusRateLimiting();

// ============================================================
// Health checks (liveness / readiness)
// ============================================================
builder.Services.AddEcoNexusHealthChecks();

// ============================================================
// OpenTelemetry — tracing + metrics
//
// Auto-instruments: incoming HTTP, outgoing HTTP (Ollama),
// EF Core queries, and .NET runtime (GC, thread pool).
// Custom MediatR and business sources are added in later tasks.
// ============================================================
const string ServiceName = "EcoNexus.Api";

builder.Services
    .AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(ServiceName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddEntityFrameworkCoreInstrumentation())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter(EcoNexusMeters.BusinessMeterName)
        .AddPrometheusExporter());

var app = builder.Build();

// ============================================================
// HTTP Request Pipeline
// ============================================================
app.UseExceptionHandler();

    // HSTS: force HTTPS-only for one year. Enabled outside Development so we
    // don't lock localhost into HTTPS during day-to-day work.
    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }

    // Security headers on every response (success, error, 404). Applied early
    // so even short-circuited requests carry them.
    app.UseSecurityHeaders();

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

// CORS must sit before auth so preflight OPTIONS requests are answered
// without hitting the authentication middleware.
app.UseCors(CorsPolicy);

// Rate limiter runs before authentication so anonymous endpoints
// (login, register) are also protected from brute-force attempts.
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Output caching must run after authorization so cached responses
// are only served to callers who already passed auth checks.
app.UseOutputCache();

app.MapControllers();

// Health endpoints
app.MapEcoNexusHealthChecks();

// Prometheus metrics scrape endpoint.
// Available in all environments so scrapers inside the cluster
// (Prometheus, OTel Collector, Grafana Agent) can reach it.
// In production this must NOT be exposed on the public ingress —
// firewall it to internal traffic only.
app.MapPrometheusScrapingEndpoint();

// SignalR hub — real-time station updates
app.MapHub<OperationsHub>("/hubs/operations");

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
