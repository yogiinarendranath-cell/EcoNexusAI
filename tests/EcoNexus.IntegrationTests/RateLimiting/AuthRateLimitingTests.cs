using System.Net;
using System.Net.Http.Json;
using EcoNexus.Contracts.Auth;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.RateLimiting;

/// <summary>
/// Proves the AuthPolicy rate limiter (5 requests / minute per IP)
/// fires correctly on POST /api/v1/auth/login.
///
/// Every request in this test comes from the same client and the same
/// simulated remote IP, so they share one rate-limit partition.
///
/// The test does not care what the auth endpoint returns for the first
/// 5 calls (probably 401 for a bogus email/password). It only asserts
/// that the 6th is rejected with 429.
/// </summary>
public sealed class AuthRateLimitingTests : IAsyncLifetime
{
    private readonly EcoNexusApiFactory _factory = new();
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _factory.InitializeAsync();
        _client = _factory.CreateClient();
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DropDatabaseAsync();
        await _factory.DisposeAsync();
    }

    [Fact]
    public async Task Login_SixRapidAttempts_SixthIs429()
    {
        var request = new LoginRequest("rate-limit-test@econexus.test", "WrongPassword123!");

        var statuses = new List<HttpStatusCode>();

        for (var i = 0; i < 6; i++)
        {
            var response = await _client.PostAsJsonAsync("/api/v1/auth/login", request);
            statuses.Add(response.StatusCode);
        }

        // First 5 must not be rate-limited.
        for (var i = 0; i < 5; i++)
        {
            Assert.NotEqual(HttpStatusCode.TooManyRequests, statuses[i]);
        }

        // The 6th must be rejected by the rate limiter.
        Assert.Equal(HttpStatusCode.TooManyRequests, statuses[5]);
    }

    [Fact]
    public async Task Login_RateLimitedResponse_HasRetryAfterHeader()
    {
        var request = new LoginRequest("retry-after-test@econexus.test", "WrongPassword123!");

        // Exhaust the bucket (5 requests).
        for (var i = 0; i < 5; i++)
        {
            await _client.PostAsJsonAsync("/api/v1/auth/login", request);
        }

        // The next request is rate-limited.
        var limited = await _client.PostAsJsonAsync("/api/v1/auth/login", request);

        Assert.Equal(HttpStatusCode.TooManyRequests, limited.StatusCode);
        Assert.True(
            limited.Headers.Contains("Retry-After"),
            "Expected Retry-After header on a 429 response.");
    }
}
