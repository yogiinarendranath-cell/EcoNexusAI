using EcoNexus.Application.Abstractions.Dispatching;
using EcoNexus.Domain.Abstractions;
using MediatR;

namespace EcoNexus.Infrastructure.Dispatching;

/// <summary>
/// MediatR-backed implementation of <see cref="IDomainEventDispatcher"/>.
/// Wraps each domain event in a <see cref="DomainEventNotification{T}"/> so
/// handlers can subscribe to the wrapped type without the domain layer
/// knowing about MediatR.
/// </summary>
internal sealed class DomainEventDispatcher : IDomainEventDispatcher
{
    private readonly IMediator _mediator;

    public DomainEventDispatcher(IMediator mediator)
    {
        _mediator = mediator;
    }

    public async Task DispatchAsync(
        IReadOnlyCollection<IDomainEvent> domainEvents,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(domainEvents);

        foreach (var domainEvent in domainEvents)
        {
            var notificationType = typeof(DomainEventNotification<>)
                .MakeGenericType(domainEvent.GetType());

            var notification = Activator.CreateInstance(notificationType, domainEvent)
                ?? throw new InvalidOperationException(
                    $"Could not create notification wrapper for {domainEvent.GetType().Name}.");

            await _mediator.Publish(notification, cancellationToken);
        }
    }
}
