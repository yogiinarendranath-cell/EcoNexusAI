using EcoNexus.Domain.Abstractions;

namespace EcoNexus.Application.Abstractions.Dispatching;

/// <summary>
/// Dispatches domain events raised by aggregates. Implemented in the
/// infrastructure layer (typically backed by MediatR). Called by the
/// persistence layer after SaveChanges so that all subscribers observe
/// events only for successfully committed aggregates.
/// </summary>
public interface IDomainEventDispatcher
{
    Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default);
}
