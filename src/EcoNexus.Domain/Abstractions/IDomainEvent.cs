namespace EcoNexus.Domain.Abstractions;

/// <summary>
/// Marker interface for a domain event. A domain event represents something
/// meaningful that happened in the domain (e.g. a waste station reached
/// critical fill level). Aggregate roots raise domain events; other layers
/// (application, infrastructure) may subscribe to them.
/// </summary>
public interface IDomainEvent
{
}
