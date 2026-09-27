using System.ComponentModel.DataAnnotations;

namespace EcoNexus.Api.Options;

/// <summary>
/// CORS configuration bound from the "Cors" section of appsettings.
/// AllowedOrigins must be an absolute origin (scheme + host + optional port),
/// without a trailing slash. Empty or missing entries are rejected at startup.
/// </summary>
public sealed class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>
    /// Origins permitted to make cross-origin requests to the API.
    /// Example: ["https://econexus.example.com", "http://localhost:5173"]
    /// </summary>
    [Required]
    [MinLength(1, ErrorMessage = "At least one CORS origin must be configured.")]
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();

    /// <summary>
    /// If true, the wildcard origin "*" is used. Only valid when
    /// AllowCredentials is false — a browser cannot send credentials
    /// to a wildcard origin.
    /// </summary>
    public bool AllowAnyOrigin { get; set; }

    /// <summary>Whether cookies and Authorization headers are allowed.</summary>
    public bool AllowCredentials { get; set; } = true;
}

