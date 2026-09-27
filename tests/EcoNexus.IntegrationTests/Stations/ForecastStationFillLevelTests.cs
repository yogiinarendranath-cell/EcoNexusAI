using System.Net;
using System.Net.Http.Json;
using EcoNexus.Contracts.Stations;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Stations;

/// <summary>
/// End-to-end tests for GET /api/v1/stations/{id}/forecast.
/// Seeds readings over HTTP (with explicit RecordedAt timestamps) so the
/// linear-regression forecaster has a real signal to work with.
/// </summary>
public sealed class ForecastStationFillLevelTests : IAsyncLifetime
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

    private static CreateStationRequest SampleStation(string code = "ST-9001")
        => new(
            Code: code,
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

    private async Task<Guid> CreateStationAsync(string code = "ST-9001")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/stations", SampleStation(code));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateStationResponse>();
        return body!.Id;
    }

    private async Task RecordReadingAsync(Guid stationId, double fillPercent, DateTimeOffset recordedAt)
    {
        var request = new RecordReadingRequest(
            FillLevelPercent: fillPercent,
            TemperatureCelsius: 20.0,
            BatteryPercent: 100,
            RecordedAt: recordedAt);

        var response = await _client.PostAsJsonAsync($"/api/v1/stations/{stationId}/readings", request);
        response.EnsureSuccessStatusCode();
    }

    [Fact]
    public async Task Forecast_LinearGrowth_PredictsOverflow()
    {
        // Arrange: station + 6 readings, 1h apart, growing 5%/hour from 40% to 65%.
        var id = await CreateStationAsync("ST-9001");
        var start = DateTimeOffset.UtcNow.AddHours(-6);

        for (var i = 0; i < 6; i++)
        {
            var fill = 40.0 + 5.0 * i;
            await RecordReadingAsync(id, fill, start.AddHours(i));
        }

        // Act
        var response = await _client.GetAsync($"/api/v1/stations/{id}/forecast?windowHours=24");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastStationFillLevelResponse>();
        Assert.NotNull(forecast);
        Assert.Equal(id, forecast!.StationId);
        Assert.Equal("LinearRegression", forecast.Method);
        Assert.Equal(6, forecast.SampleSize);
        Assert.Equal(65.0, forecast.CurrentFillPercent, precision: 1);
        Assert.Equal(5.0, forecast.FillRatePercentPerHour, precision: 2);
        Assert.True(forecast.IsOverflowPredicted);
        Assert.NotNull(forecast.PredictedOverflowAt);
        Assert.NotNull(forecast.HoursUntilOverflow);
        Assert.True(forecast.Confidence > 0, $"Confidence {forecast.Confidence} should be positive for linear data.");
    }


    [Fact]
    public async Task Forecast_UnknownStation_Returns404()
    {
        var unknownId = Guid.NewGuid();

        var response = await _client.GetAsync($"/api/v1/stations/{unknownId}/forecast?windowHours=24");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Forecast_NoReadings_ReturnsInsufficientData()
    {
        // Station exists, but no readings recorded yet.
        var id = await CreateStationAsync("ST-9002");

        var response = await _client.GetAsync($"/api/v1/stations/{id}/forecast?windowHours=24");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastStationFillLevelResponse>();
        Assert.NotNull(forecast);
        Assert.Equal("InsufficientData", forecast!.Method);
        Assert.Equal(0, forecast.SampleSize);
        Assert.False(forecast.IsOverflowPredicted);
        Assert.Null(forecast.PredictedOverflowAt);
        Assert.Null(forecast.HoursUntilOverflow);
        Assert.Equal(0, forecast.Confidence);
    }

    [Fact]
    public async Task Forecast_FewerThanMinimumSamples_ReturnsInsufficientData()
    {
        // Only 3 readings — below MinimumSamples (5).
        var id = await CreateStationAsync("ST-9003");
        var start = DateTimeOffset.UtcNow.AddHours(-3);

        for (var i = 0; i < 3; i++)
        {
            await RecordReadingAsync(id, 40.0 + 5.0 * i, start.AddHours(i));
        }

        var response = await _client.GetAsync($"/api/v1/stations/{id}/forecast?windowHours=24");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastStationFillLevelResponse>();
        Assert.NotNull(forecast);
        Assert.Equal("InsufficientData", forecast!.Method);
        Assert.Equal(3, forecast.SampleSize);
        Assert.False(forecast.IsOverflowPredicted);
    }

    [Fact]
    public async Task Forecast_FlatReadings_ReturnsNoOverflow()
    {
        // Fill stays at 40% for the whole window — no overflow predicted.
        var id = await CreateStationAsync("ST-9004");
        var start = DateTimeOffset.UtcNow.AddHours(-6);

        for (var i = 0; i < 6; i++)
        {
            await RecordReadingAsync(id, 40.0, start.AddHours(i));
        }

        var response = await _client.GetAsync($"/api/v1/stations/{id}/forecast?windowHours=24");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastStationFillLevelResponse>();
        Assert.NotNull(forecast);
        Assert.Equal("LinearRegression", forecast!.Method);
        Assert.False(forecast.IsOverflowPredicted);
        Assert.Null(forecast.PredictedOverflowAt);
        Assert.Null(forecast.HoursUntilOverflow);
        Assert.Equal(0, forecast.FillRatePercentPerHour);
    }

    [Fact]
    public async Task Forecast_WindowExcludesOldReadings_ReportsLimitedSamples()
    {
        // 6 readings: 3 recent (within 1h) + 3 old (30h ago).
        // Query with windowHours=2 → only the 3 recent readings count → insufficient.
        var id = await CreateStationAsync("ST-9005");
        var now = DateTimeOffset.UtcNow;

        // 3 old readings (30, 29, 28 hours ago)
        for (var i = 0; i < 3; i++)
        {
            await RecordReadingAsync(id, 10.0 + i, now.AddHours(-30 + i));
        }

        // 3 recent readings (1.0, 0.5, 0 hours ago)
        for (var i = 0; i < 3; i++)
        {
            await RecordReadingAsync(id, 50.0 + i, now.AddHours(-1 + 0.5 * i));
        }

        var response = await _client.GetAsync($"/api/v1/stations/{id}/forecast?windowHours=2");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var forecast = await response.Content.ReadFromJsonAsync<ForecastStationFillLevelResponse>();
        Assert.NotNull(forecast);
        Assert.Equal("InsufficientData", forecast!.Method);
        Assert.Equal(3, forecast.SampleSize);
    }

    [Fact]
    public async Task Forecast_InvalidWindowHours_Returns400()
    {
        var id = await CreateStationAsync("ST-9006");

        // windowHours=0 violates InclusiveBetween(1, 168) in the validator.
        var response = await _client.GetAsync($"/api/v1/stations/{id}/forecast?windowHours=0");

        // Depending on pipeline behavior wiring, validation may produce 400 or
        // propagate as a different status. We accept 400 (correct) and skip if
        // the pipeline currently throws (documented as a follow-up).
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}

