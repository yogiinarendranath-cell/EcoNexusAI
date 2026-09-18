using Microsoft.AspNetCore.SignalR;

namespace EcoNexus.Api.Hubs;

/// <summary>
/// SignalR hub for the city operations dashboard. Clients connect to
/// <c>/hubs/operations</c> and receive real-time station updates.
///
/// The hub itself exposes no methods today — the server pushes to clients.
/// We can add client-invokable methods later (acknowledge alert, etc.).
/// </summary>
public sealed class OperationsHub : Hub
{
}
