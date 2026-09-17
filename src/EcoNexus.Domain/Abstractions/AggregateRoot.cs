namespace EcoNexus.Domain.Abstractions;

/// <summary>
/// Base class for aggregate roots. An aggregate is a cluster of entities and
/// value objects treated as a single unit for consistency. Only aggregate
/// roots may be loaded/saved as a whole and may raise domain events.
/// </summary>
public abstract class AggregateRoot : Entity
{
    private readonly List<IDomainEvent> _domainEvents = new();

    protected AggregateRoot()
        : base()
    {
    }

    protected AggregateRoot(Guid id)
        : base(id)
    {
    }

    /// <summary>
    /// Domain events raised by this aggregate but not yet dispatched.
    /// </summary>
    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Raises a domain event. The event is queued and will be dispatched
    /// later (typically after the aggregate is persisted).
    /// </summary>
    protected void RaiseDomainEvent(IDomainEvent domainEvent)
    {
        ArgumentNullException.ThrowIfNull(domainEvent);
        _domainEvents.Add(domainEvent);
    }

    /// <summary>
    /// Clears queued domain events. Called by the infrastructure layer after
    /// dispatching.
    /// </summary>
    public void ClearDomainEvents() => _domainEvents.Clear();
}
