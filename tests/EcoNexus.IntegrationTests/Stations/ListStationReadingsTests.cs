using System.Net;
using System.Net.Http.Json;
using EcoNexus.Contracts.Stations;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Stations;

/// <summary>
/// End-to-end tests for GET /api/v1/stations/{id}/readings.
/// </summary>
public sealed class ListStationReadingsTests : IAsyncLifetime
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
        _factory.Dispose();
    }

    private static CreateStationRequest SampleStation(string code = "ST-9101")
        => new(
            Code: code,
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

    private async Task<Guid> CreateStationAsync(string code = "ST-9101")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/stations", SampleStation(code));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateStationResponse>();
        return body!.Id;
    }

    private async Task RecordReadingAsync(Guid stationId, double fillPercent, DateTimeOffset recordedAt)
    {
        var request = new RecordReadingRequest(fillPercent, 20.0, 100, recordedAt);
        var response = await _client.PostAsJsonAsync($"/api/v1/stations/{stationId}/readings", request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Readings_ReturnsOrderedAscendingByRecordedAt()
    {
        // Arrange: 4 readings recorded deliberately out-of-order in time.
        var id = await CreateStationAsync("ST-9101");
        var now = DateTimeOffset.UtcNow;

        await RecordReadingAsync(id, 10.0, now.AddHours(-4));
        await RecordReadingAsync(id, 40.0, now.AddHours(-1));
        await RecordReadingAsync(id, 20.0, now.AddHours(-3));
        await RecordReadingAsync(id, 30.0, now.AddHours(-2));

        // Act
        var response = await _client.GetAsync($"/api/v1/stations/{id}/readings?windowHours=24&limit=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var readings = await response.Content.ReadFromJsonAsync<List<StationReadingResponse>>();
        Assert.NotNull(readings);
        Assert.Equal(4, readings!.Count);

        // Must be ordered ascending by RecordedAt.
        for (var i = 1; i < readings.Count; i++)
        {
            Assert.True(
                readings[i].RecordedAt >= readings[i - 1].RecordedAt,
                $"Readings not sorted: [{i - 1}]={readings[i - 1].RecordedAt:o}, [{i}]={readings[i].RecordedAt:o}");
        }

        // Values should match the timestamps we sent.
        Assert.Equal(10.0, readings[0].FillLevelPercent, precision: 2);
        Assert.Equal(20.0, readings[1].FillLevelPercent, precision: 2);
        Assert.Equal(30.0, readings[2].FillLevelPercent, precision: 2);
        Assert.Equal(40.0, readings[3].FillLevelPercent, precision: 2);
    }

    [Fact]
    public async Task Readings_UnknownStation_Returns404()
    {
        var unknownId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/stations/{unknownId}/readings?windowHours=24");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Readings_WindowFiltersOutOlderReadings()
    {
        // Arrange: 2 recent (1h, 2h ago) + 2 old (30h, 40h ago).
        var id = await CreateStationAsync("ST-9102");
        var now = DateTimeOffset.UtcNow;

        await RecordReadingAsync(id, 70.0, now.AddHours(-30));
        await RecordReadingAsync(id, 80.0, now.AddHours(-40));
        await RecordReadingAsync(id, 50.0, now.AddHours(-1));
        await RecordReadingAsync(id, 45.0, now.AddHours(-2));

        // Act: windowHours=4 → only the two recent readings.
        var response = await _client.GetAsync($"/api/v1/stations/{id}/readings?windowHours=4&limit=100");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var readings = await response.Content.ReadFromJsonAsync<List<StationReadingResponse>>();
        Assert.NotNull(readings);
        Assert.Equal(2, readings!.Count);
        Assert.Equal(45.0, readings[0].FillLevelPercent, precision: 2);
        Assert.Equal(50.0, readings[1].FillLevelPercent, precision: 2);
    }

    [Fact]
    public async Task Readings_LimitCapsResultCount()
    {
        // Arrange: 6 readings within the window.
        var id = await CreateStationAsync("ST-9103");
        var now = DateTimeOffset.UtcNow;

        for (var i = 0; i < 6; i++)
        {
            await RecordReadingAsync(id, 10.0 + i, now.AddHours(-i));
        }

        // Act: limit=3 → only the first 3 (after ascending sort) are returned.
        var response = await _client.GetAsync($"/api/v1/stations/{id}/readings?windowHours=24&limit=3");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var readings = await response.Content.ReadFromJsonAsync<List<StationReadingResponse>>();
        Assert.NotNull(readings);
        Assert.Equal(3, readings!.Count);
    }
}

