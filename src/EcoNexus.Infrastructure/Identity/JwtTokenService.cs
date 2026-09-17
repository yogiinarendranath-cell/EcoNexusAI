using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EcoNexus.Application.Abstractions.Identity;
using EcoNexus.Domain.Entities;
using EcoNexus.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace EcoNexus.Infrastructure.Identity;

/// <summary>
/// Issues signed JWT access tokens and manages refresh tokens with rotation.
/// </summary>
public sealed class JwtTokenService : ITokenService
{
    private readonly JwtSettings _settings;
    private readonly EcoNexusDbContext _db;
    private readonly TimeProvider _timeProvider;

    public JwtTokenService(
        IOptions<JwtSettings> settings,
        EcoNexusDbContext db,
        TimeProvider timeProvider)
    {
        _settings = settings.Value;
        _db = db;
        _timeProvider = timeProvider;
    }

    public async Task<TokenPair> IssueTokenPairAsync(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();

        var accessToken = GenerateAccessToken(userId, email, roles, now, out var accessExpires);
        var rawRefreshToken = GenerateRawRefreshToken();
        var refreshTokenHash = HashToken(rawRefreshToken);

        var refreshLifetime = TimeSpan.FromDays(_settings.RefreshTokenDays);
        var refreshEntity = RefreshToken.Issue(userId, refreshTokenHash, now, refreshLifetime);

        _db.RefreshTokens.Add(refreshEntity);
        await _db.SaveChangesAsync(cancellationToken);

        return new TokenPair(
            accessToken,
            accessExpires,
            rawRefreshToken,
            refreshEntity.ExpiresAt);
    }

    public async Task<TokenPair> RefreshAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var incomingHash = HashToken(refreshToken);

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHash, cancellationToken)
            ?? throw new UnauthorizedAccessException("Refresh token not found.");

        if (!stored.IsActive(now))
        {
            throw new UnauthorizedAccessException("Refresh token is no longer active.");
        }

        // Look up the user to re-issue claims
        var user = await _db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == stored.UserId, cancellationToken)
            ?? throw new UnauthorizedAccessException("User no longer exists.");

        var roles = await (
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where ur.UserId == user.Id
            select r.Name!)
            .ToListAsync(cancellationToken);

        // Generate new pair
        var accessToken = GenerateAccessToken(user.Id, user.Email ?? string.Empty, roles, now, out var accessExpires);
        var newRawRefresh = GenerateRawRefreshToken();
        var newHash = HashToken(newRawRefresh);

        // Mark old as replaced (revokes it)
        stored.MarkReplaced(newHash, now);

        // Persist new one
        var refreshLifetime = TimeSpan.FromDays(_settings.RefreshTokenDays);
        var newEntity = RefreshToken.Issue(user.Id, newHash, now, refreshLifetime);
        _db.RefreshTokens.Add(newEntity);

        await _db.SaveChangesAsync(cancellationToken);

        return new TokenPair(
            accessToken,
            accessExpires,
            newRawRefresh,
            newEntity.ExpiresAt);
    }

    public async Task RevokeAsync(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var now = _timeProvider.GetUtcNow();
        var hash = HashToken(refreshToken);

        var stored = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        if (stored is null)
        {
            // Silent: logout with unknown token is not an error.
            return;
        }

        stored.Revoke(now);
        await _db.SaveChangesAsync(cancellationToken);
    }

    // ---- Private helpers ----

    private string GenerateAccessToken(
        Guid userId,
        string email,
        IEnumerable<string> roles,
        DateTimeOffset now,
        out DateTimeOffset expiresAt)
    {
        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(JwtRegisteredClaimNames.Email, email),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new(JwtRegisteredClaimNames.Iat,
                now.ToUnixTimeSeconds().ToString(System.Globalization.CultureInfo.InvariantCulture),
                ClaimValueTypes.Integer64)
        };

        foreach (var role in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, role));
        }

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        expiresAt = now.AddMinutes(_settings.AccessTokenMinutes);

        var token = new JwtSecurityToken(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            notBefore: now.UtcDateTime,
            expires: expiresAt.UtcDateTime,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateRawRefreshToken()
    {
        // 64 cryptographically random bytes, base64url-encoded.
        var bytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(bytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }

    private static string HashToken(string rawToken)
    {
        var bytes = Encoding.UTF8.GetBytes(rawToken);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
