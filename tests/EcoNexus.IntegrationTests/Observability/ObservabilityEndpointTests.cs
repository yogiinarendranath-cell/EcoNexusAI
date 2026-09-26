using System.Net;
using System.Net.Http.Json;
using EcoNexus.Contracts.Stations;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Observability;

/// <summary>
/// Proves the three observability surfaces (health, metrics, and
/// end-to-end business metric emission) work against the real in-process
/// API with a real database.
/// </summary>
public sealed class ObservabilityEndpointTests : IAsyncLifetime
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
    // Health checks
    // ============================================================

    [Fact]
    public async Task HealthLive_Returns200Healthy()
    {
        var response = await _client.GetAsync("/health/live");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    [Fact]
    public async Task HealthReady_WhenDbReachable_Returns200Healthy()
    {
        var response = await _client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.Equal("Healthy", body);
    }

    // ============================================================
    // Prometheus metrics endpoint
    // ============================================================

    [Fact]
    public async Task Metrics_ReturnsPrometheusFormat_WithHttpServerMetric()
    {
        var response = await _client.GetAsync("/metrics");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var body = await response.Content.ReadAsStringAsync();

        // Prometheus text exposition format always includes HELP and TYPE
        // comment markers for every metric family.
        Assert.Contains("# HELP", body);
        Assert.Contains("# TYPE", body);

        // The ASP.NET Core HTTP server meter is emitted on every running
        // instance — a reliable "we're instrumented" signal.
        Assert.Contains("http_server_", body);
    }

    // ============================================================
    // End-to-end business metric emission
    //
    // Creating a station and recording a reading raises the
    // WasteStationFillLevelChangedEvent domain event. The metrics
    // handler subscribes to it and increments the custom counter.
    // We then scrape /metrics and confirm the counter appears.
    // ============================================================

    [Fact]
    public async Task Metrics_AfterRecordingReading_ContainsEcoNexusCounter()
    {
        // ---- Arrange: create a station (unauthenticated endpoint) ----
        var createReq = new CreateStationRequest(
            Code: $"OB-{Random.Shared.Next(1000, 9999)}",
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

        var createResponse = await _client.PostAsJsonAsync("/api/v1/stations", createReq);
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<CreateStationResponse>();
        Assert.NotNull(created);

        // ---- Act: record a reading (unauthenticated endpoint) ----
        var readingReq = new RecordReadingRequest(
            FillLevelPercent: 55.0,
            TemperatureCelsius: 21.0,
            BatteryPercent: 88,
            RecordedAt: DateTimeOffset.UtcNow);

        var readingResponse = await _client.PostAsJsonAsync(
            $"/api/v1/stations/{created!.Id}/readings",
            readingReq);
        Assert.Equal(HttpStatusCode.OK, readingResponse.StatusCode);

        // Give MediatR a moment to dispatch the domain event to the
        // metrics handler. In-process publish is synchronous, but the
        // metric is incremented after the handler runs, and the meter
        // may need one collection cycle to become visible.
        await Task.Delay(TimeSpan.FromMilliseconds(200));

        // ---- Assert: scrape /metrics and look for the custom counter ----
        var metrics = await _client.GetStringAsync("/metrics");

        Assert.Contains("econexus_station_reading_recorded", metrics);
    }
}
