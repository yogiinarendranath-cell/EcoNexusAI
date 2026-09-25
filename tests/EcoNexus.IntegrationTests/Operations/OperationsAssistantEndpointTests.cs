using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcoNexus.Contracts.Auth;
using EcoNexus.Contracts.Operations;
using EcoNexus.Infrastructure.Identity;
using EcoNexus.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace EcoNexus.IntegrationTests.Operations;

/// <summary>
/// End-to-end tests for the operations assistant endpoint. Proves the
/// whole Phase 14 pipeline: HTTP -> JWT -> controller -> MediatR ->
/// AskAssistantHandler -> Mock LLM intent -> tool registry -> tool
/// execution -> Mock LLM shaping -> persistence -> HTTP response.
/// </summary>
public sealed class OperationsAssistantEndpointTests : IAsyncLifetime
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
    // Happy path — the mock LLM picks GetCriticalStations for a
    // question containing the word "critical".
    // ============================================================

    [Fact]
    public async Task Ask_AsOperationsManager_ReturnsAnswerWithExpectedTool()
    {
        // Arrange: a real user with a real JWT and the OperationsManager role.
        var (_, token) = await RegisterLoginAndPromoteAsync("ops-manager@econexus.test");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var body = new AskAssistantRequest("Which stations are critical right now?");

        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/operations/assistant/ask", body);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AskAssistantResponse>();
        Assert.NotNull(payload);

        // The mock LLM keyword-matches "critical" -> GetCriticalStations.
        Assert.Equal("GetCriticalStations", payload!.ToolName);
        Assert.Equal("Mock", payload.ProviderName);
        Assert.False(string.IsNullOrWhiteSpace(payload.Answer));
        Assert.NotEqual(Guid.Empty, payload.InteractionId);
        Assert.True(payload.LatencyMs >= 0);
        Assert.Equal("Which stations are critical right now?", payload.Question);
    }

    // ============================================================
    // Question with no matching tool -> handler returns 409.
    // ============================================================

    [Fact]
    public async Task Ask_WithUnrecognizedQuestion_Returns409()
    {
        var (_, token) = await RegisterLoginAndPromoteAsync("ops-manager-2@econexus.test");

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", token);

        var body = new AskAssistantRequest("Tell me a joke about penguins");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/operations/assistant/ask", body);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    // ============================================================
    // Unauthenticated caller -> 401.
    // ============================================================

    [Fact]
    public async Task Ask_WithoutToken_Returns401()
    {
        _client.DefaultRequestHeaders.Authorization = null;

        var body = new AskAssistantRequest("Which stations are critical?");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/operations/assistant/ask", body);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Authenticated but Citizen (not ops role) -> 403.
    // ============================================================

    [Fact]
    public async Task Ask_AsCitizen_Returns403()
    {
        // Register + login only. No role promotion.
        var email = "citizen-ask@econexus.test";
        const string password = "ValidPass123!";

        var register = new RegisterRequest(email, password, "Citizen User");
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", register);
        Assert.True(registerResponse.IsSuccessStatusCode);

        var login = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", login);
        Assert.True(loginResponse.IsSuccessStatusCode);

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        _client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", auth!.AccessToken);

        var body = new AskAssistantRequest("Which stations are critical?");

        var response = await _client.PostAsJsonAsync(
            "/api/v1/operations/assistant/ask", body);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    // ============================================================
    // Helpers
    // ============================================================

    /// <summary>
    /// Registers a user, logs them in, then grants them the
    /// OperationsManager role via the seeded UserManager so the
    /// assistant endpoint authorizes them.
    /// </summary>
    private async Task<(Guid UserId, string Token)> RegisterLoginAndPromoteAsync(string email)
    {
        const string password = "ValidPass123!";

        var register = new RegisterRequest(email, password, "Ops Manager");
        var registerResponse = await _client.PostAsJsonAsync("/api/v1/auth/register", register);
        Assert.True(
            registerResponse.IsSuccessStatusCode,
            $"Register failed: {await registerResponse.Content.ReadAsStringAsync()}");

        var registerPayload = await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(registerPayload);
        var userId = registerPayload!.UserId;

        // Promote: access the app's UserManager from the test factory's service provider.
        using (var scope = _factory.Services.CreateScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByIdAsync(userId.ToString());
            Assert.NotNull(user);
            await userManager.AddToRoleAsync(user!, EcoNexusRoles.OperationsManager);
        }

        // Re-login so the JWT carries the new role claim.
        var login = new LoginRequest(email, password);
        var loginResponse = await _client.PostAsJsonAsync("/api/v1/auth/login", login);
        Assert.True(
            loginResponse.IsSuccessStatusCode,
            $"Login failed: {await loginResponse.Content.ReadAsStringAsync()}");

        var auth = await loginResponse.Content.ReadFromJsonAsync<AuthResponse>();
        Assert.NotNull(auth);

        return (userId, auth!.AccessToken);
    }
}
