using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcoNexus.Contracts.Auth;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Auth;

/// <summary>
/// End-to-end tests for the authentication endpoints.
/// Each test class instance gets a fresh test database.
/// </summary>
public sealed class AuthEndpointTests : IAsyncLifetime
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

    // ============================================================
    // Register
    // ============================================================

    [Fact]
    public async Task Register_WithValidInput_Returns201AndUserInfo()
    {
        var request = new RegisterRequest("alice@example.com", "ValidPass123!", "Alice");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.UserId);
        Assert.Equal("alice@example.com", body.Email);
        Assert.Equal("Alice", body.DisplayName);
        Assert.Contains("Citizen", body.Roles);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_Returns409()
    {
        var request = new RegisterRequest("bob@example.com", "ValidPass123!", "Bob");
        await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidEmail_Returns400()
    {
        var request = new RegisterRequest("not-an-email", "ValidPass123!", "Charlie");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithWeakPassword_Returns400()
    {
        var request = new RegisterRequest("dave@example.com", "weak", "Dave");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ============================================================
    // Login
    // ============================================================

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        await RegisterUserAsync("erin@example.com", "ValidPass123!", "Erin");
        var loginRequest = new LoginRequest("erin@example.com", "ValidPass123!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.True(body.AccessTokenExpiresAt > DateTimeOffset.UtcNow);
        Assert.True(body.RefreshTokenExpiresAt > DateTimeOffset.UtcNow);
        Assert.Equal("erin@example.com", body.User.Email);
    }

    [Fact]
    public async Task Login_WithWrongPassword_Returns401()
    {
        await RegisterUserAsync("frank@example.com", "ValidPass123!", "Frank");
        var loginRequest = new LoginRequest("frank@example.com", "WrongPassword123!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_Returns401()
    {
        var loginRequest = new LoginRequest("nobody@example.com", "ValidPass123!");

        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Me
    // ============================================================

    [Fact]
    public async Task Me_WithValidToken_ReturnsCurrentUser()
    {
        var (login, _) = await RegisterAndLoginAsync("gina@example.com", "ValidPass123!", "Gina");
        SetBearerToken(login.AccessToken);

        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(body);
        Assert.Equal("gina@example.com", body!.Email);
        Assert.Equal("Gina", body.DisplayName);
    }

    [Fact]
    public async Task Me_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Refresh
    // ============================================================

    [Fact]
    public async Task Refresh_WithValidToken_ReturnsNewPair()
    {
        var (login, _) = await RegisterAndLoginAsync("henry@example.com", "ValidPass123!", "Henry");
        var refreshRequest = new RefreshTokenRequest(login.RefreshToken);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(login.AccessToken, body!.AccessToken);
        Assert.NotEqual(login.RefreshToken, body.RefreshToken);
    }

    [Fact]
    public async Task Refresh_WithRevokedToken_Returns401()
    {
        var (login, accessToken) = await RegisterAndLoginAsync("iris@example.com", "ValidPass123!", "Iris");
        // Logout first to revoke the refresh token
        SetBearerToken(accessToken);
        await _client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest(login.RefreshToken));

        var refreshRequest = new RefreshTokenRequest(login.RefreshToken);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Logout
    // ============================================================

    [Fact]
    public async Task Logout_WithValidToken_Returns204()
    {
        var (login, accessToken) = await RegisterAndLoginAsync("jack@example.com", "ValidPass123!", "Jack");
        SetBearerToken(accessToken);

        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest(login.RefreshToken));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Logout_WithoutToken_Returns401()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/auth/logout", new RefreshTokenRequest("some-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private async Task RegisterUserAsync(string email, string password, string displayName)
    {
        var request = new RegisterRequest(email, password, displayName);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/register", request);
        response.EnsureSuccessStatusCode();
    }

    private async Task<(AuthResponse login, string accessToken)> RegisterAndLoginAsync(
        string email, string password, string displayName)
    {
        await RegisterUserAsync(email, password, displayName);
        var loginRequest = new LoginRequest(email, password);
        var response = await _client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
        response.EnsureSuccessStatusCode();
        var login = await response.Content.ReadFromJsonAsync<AuthResponse>();
        return (login!, login!.AccessToken);
    }

    private void SetBearerToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
