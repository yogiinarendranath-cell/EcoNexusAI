using EcoNexus.Domain.Abstractions;
using MediatR;

namespace EcoNexus.Application.Abstractions.Dispatching;

/// <summary>
/// Wraps an <see cref="IDomainEvent"/> so it can be dispatched through MediatR
/// as a notification. Lives in the Application layer — not the Domain — so the
/// domain stays free of any infrastructure or library dependencies.
///
/// Handlers subscribe to <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>
/// where TEvent is the concrete domain event type they care about.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent)
    : INotification
    where TDomainEvent : IDomainEvent;
