using System.Net.Http.Json;
using System.Threading.Channels;
using EcoNexus.Contracts.Realtime;
using EcoNexus.Contracts.Stations;
using EcoNexus.Infrastructure.Realtime;
using EcoNexus.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Xunit;

namespace EcoNexus.IntegrationTests.Realtime;

/// <summary>
/// End-to-end proof that a recorded reading produces a SignalR push
/// to connected clients. This is the integration test for Step 8.
/// </summary>
public sealed class StationRealtimeBroadcastTests : IAsyncLifetime
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

    [Fact]
    public async Task RecordReading_PushesFillLevelChangedToSignalRClients()
    {
        // -------- Arrange --------
        // 1. Create a station via HTTP
        var createReq = new CreateStationRequest(
            Code: "ST-RT-001",
            Latitude: 12.9716,
            Longitude: 77.5946,
            CapacityKilograms: 500,
            PrimaryCategory: "Plastic");

        var createResp = await _client.PostAsJsonAsync("/api/v1/stations", createReq);
        createResp.EnsureSuccessStatusCode();
        var created = await createResp.Content.ReadFromJsonAsync<CreateStationResponse>();
        Assert.NotNull(created);

        // 2. Open a real SignalR client against the in-process TestServer
        var received = Channel.CreateUnbounded<StationFillLevelChangedNotification>();

        await using var connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_client.BaseAddress!, "/hubs/operations"), options =>
            {
                options.Transports = HttpTransportType.LongPolling;
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        connection.On<StationFillLevelChangedNotification>(
            SignalROperationsNotifier.FillLevelChangedClientMethod,
            n => received.Writer.TryWrite(n));

        await connection.StartAsync();

        // -------- Act --------
        var readingReq = new RecordReadingRequest(
            FillLevelPercent: 42.5,
            TemperatureCelsius: 21.0,
            BatteryPercent: 88,
            RecordedAt: DateTimeOffset.UtcNow);

        var readResp = await _client.PostAsJsonAsync(
            $"/api/v1/stations/{created!.Id}/readings",
            readingReq);
        readResp.EnsureSuccessStatusCode();

        // -------- Assert --------
        var notification = await ReadWithTimeoutAsync(received.Reader, TimeSpan.FromSeconds(10));

        Assert.NotNull(notification);
        Assert.Equal(created.Id, notification!.StationId);
        Assert.Equal("ST-RT-001", notification.StationCode);
        Assert.Equal(42.5, notification.FillLevelPercent, 1);
        Assert.False(notification.IsCritical);
    }

    private static async Task<StationFillLevelChangedNotification?> ReadWithTimeoutAsync(
        ChannelReader<StationFillLevelChangedNotification> reader,
        TimeSpan timeout)
    {
        using var cts = new CancellationTokenSource(timeout);
        try
        {
            return await reader.ReadAsync(cts.Token);
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }
}
