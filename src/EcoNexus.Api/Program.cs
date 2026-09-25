using System.Text;
using EcoNexus.Infrastructure.Realtime;
using EcoNexus.Api.Middleware;
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
// CORS — allow the Vite dev frontend during development
// ============================================================
const string DevCorsPolicy = "EcoNexusDev";

builder.Services.AddCors(options =>
{
    options.AddPolicy(DevCorsPolicy, policy =>
    {
        policy
            .WithOrigins("http://localhost:5173", "http://127.0.0.1:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});
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

// CORS must sit before auth so preflight OPTIONS requests are answered
// without hitting the authentication middleware.
app.UseCors(DevCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

// Output caching must run after authorization so cached responses
// are only served to callers who already passed auth checks.
app.UseOutputCache();

app.MapControllers();

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
