using Microsoft.AspNetCore.SignalR;

namespace EcoNexus.Infrastructure.Realtime;

/// <summary>
/// SignalR hub for the city operations dashboard. Clients connect to
/// <c>/hubs/operations</c> and receive real-time station updates.
///
/// The hub itself exposes no methods today — the server pushes to clients.
/// We can add client-invokable methods later (acknowledge alert, etc.).
///
/// Lives in the Infrastructure layer (not the API) so that the notifier
/// implementation — which is also infrastructure — can reference it without
/// creating a circular dependency with the API layer.
/// </summary>
public sealed class OperationsHub : Hub
{
}
