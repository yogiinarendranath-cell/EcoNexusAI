using System.Security.Claims;
using System.Threading.RateLimiting;

namespace EcoNexus.Api.Extensions;

/// <summary>
/// Rate limiting policies for the EcoNexus API.
///
/// Three tiers, sized for a small SaaS workload:
///
///   AuthPolicy      — 5 requests / minute. Brute-force protection on
///                     login, register, refresh. Keyed by remote IP for
///                     anonymous callers.
///   WritePolicy     — 30 requests / minute. POST/PUT/DELETE endpoints
///                     that mutate state. Keyed by user id when
///                     authenticated, remote IP otherwise.
///   ReadPolicy      — 120 requests / minute. GET endpoints. Loose,
///                     because most reads are already cached and the
///                     limit is only a safety net against scraping.
///   AssistantPolicy — 10 requests / minute. LLM calls are expensive and
///                     slow; keep a tight budget per user.
///
/// All policies return 429 Too Many Requests with a Retry-After header
/// when exceeded. Health, metrics, and Swagger are excluded via
/// [DisableRateLimiting] on their endpoints (not shown here — the
/// default policy is off; endpoints opt in explicitly).
/// </summary>
public static class RateLimitingExtensions
{
    public const string AuthPolicy = "auth";
    public const string WritePolicy = "writes";
    public const string ReadPolicy = "reads";
    public const string AssistantPolicy = "assistant";

    public static IServiceCollection AddEcoNexusRateLimiting(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddRateLimiter(options =>
        {
            // Return 429 with a Retry-After header. The default
            // (503) is less idiomatic for rate limiting.
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            // Emit a Retry-After header so well-behaved clients back off.
            // The fixed-window limiter doesn't set this by default.
            options.OnRejected = async (context, ct) =>
            {
                if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                {
                    context.HttpContext.Response.Headers.RetryAfter =
                        ((int)retryAfter.TotalSeconds).ToString();
                }
                else
                {
                    // Fallback: standard window length when metadata is absent.
                    context.HttpContext.Response.Headers.RetryAfter = "60";
                }

                await Task.CompletedTask;
            };

            // ------------------------------------------------------------
            // AuthPolicy — 5 requests / minute per IP. Strict.
            // ------------------------------------------------------------
            options.AddPolicy(AuthPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            // ------------------------------------------------------------
            // WritePolicy — 30 requests / minute per user (or IP).
            // ------------------------------------------------------------
            options.AddPolicy(WritePolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 30,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            // ------------------------------------------------------------
            // ReadPolicy — 120 requests / minute per user (or IP).
            // ------------------------------------------------------------
            options.AddPolicy(ReadPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 120,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            // ------------------------------------------------------------
            // AssistantPolicy — 10 requests / minute per user (or IP).
            // ------------------------------------------------------------
            options.AddPolicy(AssistantPolicy, httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetClientKey(httpContext),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0,
                    }));

            // ------------------------------------------------------------
            // Global fallback — no policy attached to an endpoint means
            // no limit. Endpoints opt in explicitly via [EnableRateLimiting].
            // ------------------------------------------------------------
            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(_ =>
                RateLimitPartition.GetNoLimiter("global-no-limit"));
        });

        return services;
    }

    /// <summary>
    /// Partition key strategy. Prefer the authenticated user id (from
    /// the JWT "sub" claim, which is what NameClaimType is set to in
    /// Program.cs). Fall back to remote IP for anonymous callers.
    /// </summary>
    private static string GetClientKey(HttpContext context)
    {
        var sub = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User?.FindFirstValue("sub");

        if (!string.IsNullOrEmpty(sub))
        {
            return $"user:{sub}";
        }

        var ip = context.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip";
        return $"ip:{ip}";
    }
}
