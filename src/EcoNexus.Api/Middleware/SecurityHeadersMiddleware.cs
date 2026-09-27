namespace EcoNexus.Api.Middleware;

/// <summary>
/// Adds standard HTTP security headers to every response.
/// These mitigate a broad set of browser-side attacks:
///   - X-Content-Type-Options: prevents MIME-type sniffing
///   - X-Frame-Options: prevents clickjacking via iframes
///   - Referrer-Policy: limits referrer leakage to third parties
///   - Permissions-Policy: disables browser features the API does not use
///   - X-XSS-Protection: legacy header, still respected by some browsers
///
/// Content-Security-Policy is intentionally NOT set here. This is a JSON API;
/// CSP is meaningful for HTML responses. If we later serve HTML from this
/// origin, that route should set its own CSP.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;

    public SecurityHeadersMiddleware(RequestDelegate next)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var headers = context.Response.Headers;

        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";
        headers["X-XSS-Protection"] = "1; mode=block";

        await _next(context);
    }
}

