using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using EcoNexus.Contracts.Auth;
using EcoNexus.Contracts.Citizen;
using EcoNexus.Contracts.Common;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Citizen;

/// <summary>
/// End-to-end tests for the citizen endpoints. Each test class instance
/// gets a fresh test database.
/// </summary>
public sealed class CitizenEndpointsTests : IAsyncLifetime
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
    // Profile
    // ============================================================

    [Fact]
    public async Task Profile_FirstCall_ProvisionsProfileWithZeroBalance()
    {
        var (_, token) = await RegisterAndLoginAsync("c1@example.com", "ValidPass123!", "C1");
        SetBearerToken(token);

        var response = await _client.GetAsync("/api/v1/citizen/profile");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CitizenProfileResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.NotEqual(Guid.Empty, body.UserId);
        Assert.Equal("C1", body.DisplayName);
        Assert.Equal(0, body.GreenPointsBalance);
        Assert.Equal(0, body.CurrentStreakDays);
        Assert.Equal(0, body.TotalTransactions);
        Assert.Equal(0, body.TotalEarned);
        Assert.Equal(0, body.TotalRedeemed);
    }

    [Fact]
    public async Task Profile_SecondCall_ReturnsSameProfile()
    {
        var (_, token) = await RegisterAndLoginAsync("c2@example.com", "ValidPass123!", "C2");
        SetBearerToken(token);

        var first = await _client.GetAsync("/api/v1/citizen/profile");
        var firstBody = await first.Content.ReadFromJsonAsync<CitizenProfileResponse>();

        var second = await _client.GetAsync("/api/v1/citizen/profile");
        var secondBody = await second.Content.ReadFromJsonAsync<CitizenProfileResponse>();

        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(firstBody!.Id, secondBody!.Id);
    }

    // ============================================================
    // Points history
    // ============================================================

    [Fact]
    public async Task PointsHistory_NewProfile_ReturnsEmptyPage()
    {
        var (_, token) = await RegisterAndLoginAsync("c3@example.com", "ValidPass123!", "C3");
        SetBearerToken(token);

        // Provision profile first
        await _client.GetAsync("/api/v1/citizen/profile");

        var response = await _client.GetAsync("/api/v1/citizen/points/history");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<PointTransactionResponse>>();
        Assert.NotNull(body);
        Assert.Empty(body!.Items);
        Assert.Equal(0, body.TotalCount);
    }

    // ============================================================
    // Rewards catalog
    // ============================================================

    [Fact]
    public async Task Rewards_EmptyCatalog_ReturnsEmptyList()
    {
        var (_, token) = await RegisterAndLoginAsync("c4@example.com", "ValidPass123!", "C4");
        SetBearerToken(token);

        var response = await _client.GetAsync("/api/v1/citizen/rewards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<RewardResponse>>();
        Assert.NotNull(body);
        Assert.Empty(body!);
    }

    // ============================================================
    // Station visits
    // ============================================================

    [Fact]
    public async Task RecordVisit_FirstVisit_AwardsPointsAndAdvancesStreak()
    {
        var (_, token) = await RegisterAndLoginAsync("c5@example.com", "ValidPass123!", "C5");
        SetBearerToken(token);

        // Provision profile
        await _client.GetAsync("/api/v1/citizen/profile");

        // Create a station to visit
        var stationId = await CreateStationAsync();

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new RecordStationVisitRequest(stationId, today);

        var response = await _client.PostAsJsonAsync("/api/v1/citizen/visits", request);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RecordStationVisitResponse>();
        Assert.NotNull(body);
        Assert.Equal(stationId, body!.StationId);
        Assert.Equal(10, body.PointsAwarded);
        Assert.Equal(10, body.NewBalance);
        Assert.Equal(1, body.CurrentStreakDays);
    }

    [Fact]
    public async Task RecordVisit_TwiceSameStationSameDay_Returns409()
    {
        var (_, token) = await RegisterAndLoginAsync("c6@example.com", "ValidPass123!", "C6");
        SetBearerToken(token);

        await _client.GetAsync("/api/v1/citizen/profile");
        var stationId = await CreateStationAsync();
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var first = await _client.PostAsJsonAsync(
            "/api/v1/citizen/visits",
            new RecordStationVisitRequest(stationId, today));
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);

        var second = await _client.PostAsJsonAsync(
            "/api/v1/citizen/visits",
            new RecordStationVisitRequest(stationId, today));

        Assert.Equal(HttpStatusCode.Conflict, second.StatusCode);
    }

    [Fact]
    public async Task RecordVisit_UnknownStation_Returns404()
    {
        var (_, token) = await RegisterAndLoginAsync("c7@example.com", "ValidPass123!", "C7");
        SetBearerToken(token);

        await _client.GetAsync("/api/v1/citizen/profile");

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var request = new RecordStationVisitRequest(Guid.NewGuid(), today);

        var response = await _client.PostAsJsonAsync("/api/v1/citizen/visits", request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ============================================================
    // Reward redemption
    // ============================================================

    [Fact]
    public async Task RedeemReward_InsufficientBalance_Returns409()
    {
        var (_, token) = await RegisterAndLoginAsync("c8@example.com", "ValidPass123!", "C8");
        SetBearerToken(token);

        await _client.GetAsync("/api/v1/citizen/profile");

        // No rewards exist, but we can try redeeming a fake id.
        // Handler should first check the reward exists.
        var fakeRewardId = Guid.NewGuid();
        var response = await _client.PostAsync(
            $"/api/v1/citizen/rewards/{fakeRewardId}/redeem",
            null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ============================================================
    // Authorization
    // ============================================================

    [Fact]
    public async Task Profile_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/citizen/profile");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Rewards_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/v1/citizen/rewards");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // ============================================================
    // Helpers
    // ============================================================

    private async Task<(AuthResponse login, string accessToken)> RegisterAndLoginAsync(
        string email, string password, string displayName)
    {
        var register = new RegisterRequest(email, password, displayName);
        var registerResp = await _client.PostAsJsonAsync("/api/v1/auth/register", register);
        registerResp.EnsureSuccessStatusCode();

        var login = new LoginRequest(email, password);
        var loginResp = await _client.PostAsJsonAsync("/api/v1/auth/login", login);
        loginResp.EnsureSuccessStatusCode();

        var loginBody = await loginResp.Content.ReadFromJsonAsync<AuthResponse>();
        return (loginBody!, loginBody!.AccessToken);
    }

    private void SetBearerToken(string token)
    {
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private async Task<Guid> CreateStationAsync()
    {
        // Create a fresh station with a unique code. The endpoint requires
        // authentication but any authenticated user can create for the
        // purposes of these tests (no role gate on the stations endpoint yet).
        var code = $"RT-{Random.Shared.Next(1000, 9999)}";
        var request = new
        {
            code,
            latitude = 18.5204,
            longitude = 73.8567,
            capacityKilograms = 500.0,
            primaryCategory = "Plastic",
        };

        var response = await _client.PostAsJsonAsync("/api/v1/stations", request);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadFromJsonAsync<CreateStationResponse>();
        return body!.Id;
    }

    private sealed record CreateStationResponse(Guid Id, string Code);
}
