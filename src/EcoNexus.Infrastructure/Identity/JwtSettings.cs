namespace EcoNexus.Infrastructure.Identity;

/// <summary>
/// Configuration for JWT access-token issuance. Bound from the "Jwt" section
/// of appsettings.json. The SecretKey must never be committed to source control
/// for production — it will move to Azure Key Vault in Step 16.
/// </summary>
public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    /// <summary>Issuer (iss claim). Identifies who issued the token.</summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>Audience (aud claim). Identifies intended recipient.</summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>HMAC-SHA256 signing key. Must be at least 32 bytes (256 bits).</summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>Access token lifetime in minutes. Default 15.</summary>
    public int AccessTokenMinutes { get; set; } = 15;

    /// <summary>Refresh token lifetime in days. Default 7.</summary>
    public int RefreshTokenDays { get; set; } = 7;
}
