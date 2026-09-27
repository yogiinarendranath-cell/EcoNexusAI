namespace EcoNexus.Api.Middleware;

/// <summary>
/// Extension helpers for wiring <see cref="SecurityHeadersMiddleware"/>
/// into the request pipeline.
/// </summary>
public static class SecurityHeadersExtensions
{
    /// <summary>
    /// Adds the security-headers middleware to the pipeline.
    /// Should be added early — before authentication, routing, or endpoints —
    /// so that error responses and 404s also carry the headers.
    /// </summary>
    public static IApplicationBuilder UseSecurityHeaders(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseMiddleware<SecurityHeadersMiddleware>();
    }
}

