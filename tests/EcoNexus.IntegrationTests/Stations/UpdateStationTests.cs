using System.Net;
using System.Net.Http.Json;
using EcoNexus.Contracts.Stations;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Stations;

public sealed class UpdateStationTests : IAsyncLifetime
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

    private static CreateStationRequest SampleStation(string code = "ST-3001")
        => new(
            Code: code,
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

    private async Task<Guid> CreateStationAsync(string code = "ST-3001")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/stations", SampleStation(code));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateStationResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task Update_ExistingStation_Returns204AndPersistsChanges()
    {
        var id = await CreateStationAsync("ST-3002");
        var update = new UpdateStationRequest(40.7128, -74.0060, 250.0, "Paper");

        var response = await _client.PutAsJsonAsync($"/api/v1/stations/{id}", update);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        var fetched = await _client.GetFromJsonAsync<StationDetailResponse>($"/api/v1/stations/{id}");
        Assert.NotNull(fetched);
        Assert.Equal(40.7128, fetched!.Latitude, 4);
        Assert.Equal(-74.0060, fetched.Longitude, 4);
        Assert.Equal(250.0, fetched.CapacityKilograms, 2);
        Assert.Equal("Paper", fetched.PrimaryCategory);
    }

    [Fact]
    public async Task Update_UnknownStation_Returns404()
    {
        var update = new UpdateStationRequest(0, 0, 100, "Plastic");

        var response = await _client.PutAsJsonAsync(
            $"/api/v1/stations/{Guid.NewGuid()}", update);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Update_InvalidCategory_Returns400()
    {
        var id = await CreateStationAsync("ST-3003");
        var update = new UpdateStationRequest(0, 0, 100, "NotARealCategory");

        var response = await _client.PutAsJsonAsync($"/api/v1/stations/{id}", update);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
