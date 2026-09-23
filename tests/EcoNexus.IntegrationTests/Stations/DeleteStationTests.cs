using System.Net;
using System.Net.Http.Json;
using EcoNexus.Contracts.Stations;
using EcoNexus.IntegrationTests.Infrastructure;
using Xunit;

namespace EcoNexus.IntegrationTests.Stations;

public sealed class DeleteStationTests : IAsyncLifetime
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

    private static CreateStationRequest SampleStation(string code = "ST-4001")
        => new(
            Code: code,
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

    private async Task<Guid> CreateStationAsync(string code = "ST-4001")
    {
        var response = await _client.PostAsJsonAsync("/api/v1/stations", SampleStation(code));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<CreateStationResponse>();
        return body!.Id;
    }

    [Fact]
    public async Task Delete_ExistingStation_Returns204AndRemovesIt()
    {
        var id = await CreateStationAsync("ST-4002");

        var deleteResponse = await _client.DeleteAsync($"/api/v1/stations/{id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getResponse = await _client.GetAsync($"/api/v1/stations/{id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task Delete_UnknownStation_Returns204Idempotent()
    {
        var response = await _client.DeleteAsync($"/api/v1/stations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
