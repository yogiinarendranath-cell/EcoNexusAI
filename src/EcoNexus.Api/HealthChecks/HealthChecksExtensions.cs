using EcoNexus.Infrastructure.Persistence;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace EcoNexus.Api.HealthChecks;

/// <summary>
/// Health-check endpoints for liveness and readiness probes.
///
///   /health/live  — process-level liveness. No external dependency
///                   checks. Always 200 if the app is running. Used by
///                   orchestrators to decide whether to restart.
///
///   /health/ready — dependency readiness. Checks the EF Core DbContext
///                   (which pings SQL Server with a trivial query).
///                   Returns 200 when healthy, 503 when not. Used by
///                   load balancers to decide whether to route traffic.
///
/// Detailed JSON output is emitted for non-Development environments
/// only when the caller is authenticated (future work — currently the
/// response is minimal to avoid leaking internal state).
/// </summary>
public static class HealthChecksExtensions
{
    public const string LiveTag = "live";
    public const string ReadyTag = "ready";

    public static IServiceCollection AddEcoNexusHealthChecks(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services
            .AddHealthChecks()
            .AddDbContextCheck<EcoNexusDbContext>(
                name: "database",
                failureStatus: HealthStatus.Unhealthy,
                tags: new[] { ReadyTag });

        return services;
    }

    public static IEndpointRouteBuilder MapEcoNexusHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        ArgumentNullException.ThrowIfNull(endpoints);

        // Liveness: no tag filter — returns 200 if the process is up.
        // We deliberately exclude "ready" tagged checks by filtering on "live".
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,        // never run any registered check
            ResponseWriter = WriteMinimalResponse,
        });

        // Readiness: run only checks tagged "ready" (currently just DB).
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = reg => reg.Tags.Contains(ReadyTag),
            ResponseWriter = WriteMinimalResponse,
        });

        return endpoints;
    }

    private static Task WriteMinimalResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "text/plain";
        return context.Response.WriteAsync(report.Status.ToString());
    }
}
