using System.Net;
using System.Net.Http.Json;
using EcoNexus.Contracts.Stations;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Stations;

public sealed class StationsEndpointTests : IAsyncLifetime
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

    private static CreateStationRequest SampleRequest(
        string code = "ST-2001",
        string category = "Plastic")
        => new(
            Code: code,
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: category);

    [Fact]
    public async Task Create_ValidRequest_Returns201WithLocation()
    {
        // Act
        var response = await _client.PostAsJsonAsync("/api/v1/stations", SampleRequest());

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        var body = await response.Content.ReadFromJsonAsync<CreateStationResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.Id);
        Assert.Equal("ST-2001", body.Code);
    }

    [Fact]
    public async Task CreateThenGet_RoundTripsStation()
    {
        // Act
        var createResponse = await _client.PostAsJsonAsync("/api/v1/stations", SampleRequest("ST-2002"));
        createResponse.EnsureSuccessStatusCode();

        var created = await createResponse.Content.ReadFromJsonAsync<CreateStationResponse>();
        Assert.NotNull(created);

        // Act
        var getResponse = await _client.GetAsync($"/api/v1/stations/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);

        var station = await getResponse.Content.ReadFromJsonAsync<StationDetailResponse>();
        Assert.NotNull(station);
        Assert.Equal(created.Id, station!.Id);
        Assert.Equal("ST-2002", station.Code);
        Assert.Equal("Plastic", station.PrimaryCategory);
        Assert.Equal("Online", station.Status);
        Assert.Equal(0, station.CurrentFillPercent);
    }

    [Fact]
    public async Task Get_UnknownId_Returns404()
    {
        // Act
        var response = await _client.GetAsync($"/api/v1/stations/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InvalidCategory_Returns400()
    {
        // Act
        var response = await _client.PostAsJsonAsync(
            "/api/v1/stations",
            SampleRequest("ST-2003", "NotARealCategory"));

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
