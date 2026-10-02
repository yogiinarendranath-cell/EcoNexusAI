# ADR-0016: Event-Driven Architecture

**Status:** Accepted
**Date:** 2026-10-02
**Decision Makers:** CTO, Lead Engineer

---

## Context

Several operations in the domain produce side effects that must be observed
later, without the aggregate root becoming a magnet for unrelated concerns:

- A station reading that crosses 90% fill must notify operations clients
  in real time (SignalR).
- A citizen filing a report must award green points.
- A collection job completing must update station metrics.
- An intake reaching the "Recovered" stage must update recycling metrics.

Three patterns are common:

1. **Direct method calls** — the aggregate calls out to services in the same
   method. Couples Domain to Infrastructure.
2. **Events published after commit** — the aggregate queues events; the
   persistence layer dispatches them after a successful save.
3. **Event sourcing** — the event log *is* the state. Very different model.

The Domain must not reference MediatR, ASP.NET, EF Core, or any other
infrastructure library. The architecture tests enforce this.

---
## Decision

Adopt **domain events dispatched post-commit**.

- Aggregates raise events via `AggregateRoot.RaiseDomainEvent(IDomainEvent)`.
  Events queue on the aggregate; they do not fire immediately.
- `IDomainEvent` is a pure marker interface in the Domain layer.
- `EcoNexusDbContext.SaveChangesAsync` gathers queued events from all tracked
  aggregates and hands them to `IDomainEventDispatcher` **after** the commit
  succeeds. A failed save never dispatches.
- The dispatcher (in Infrastructure) wraps each event in
  `DomainEventNotification<TEvent>` — a MediatR `INotification` — so
  Application handlers can subscribe without Domain referencing MediatR.
- Subscribers live in the Application layer as
  `INotificationHandler<DomainEventNotification<TEvent>>`.

Twelve events exist at the time of this ADR: `AlertRaised`,
`CitizenReportFiled`, `CollectionJobCompleted`, `CollectionJobStarted`,
`GreenPointsEarned`, `RecyclingIntakeAdvanced`, `RecyclingIntakeRecorded`,
`RewardRedeemed`, `RouteStopCompleted`, `WasteStationCollected`,
`WasteStationFillLevelChanged`, `WasteStationReachedCriticalFill`.

---
## Consequences

**Positive:**
- Domain stays free of infrastructure — the only dependency is `IDomainEvent`.
- Publishers don't know subscribers; adding a new reaction requires no change
  to the aggregate.
- Events are testable on the aggregate in isolation: assert `DomainEvents`
  after invoking a method, no dispatcher needed.
- One-way flow: the write transaction completes before any handler runs, so
  a slow subscriber cannot block the user's request.

**Negative:**
- Handlers are eventually consistent, not synchronous. A handler failure does
  not roll back the original save.
- Traceability is harder than a direct call — correlation IDs
  (`trace:{TraceId}`) are the mitigation.
- The `DomainEventNotification<T>` wrapper is extra ceremony, but necessary
  to keep Domain pure.
- No retry / dead-letter queue today. If a handler throws, that event's
  reaction is lost. Acceptable at v1 scale; would need revisiting for
  multi-instance deployments or critical side effects.

---

## Alternatives Considered

- **Direct method calls from the aggregate** — rejected. Couples Domain to
  the reaction's implementation and forces the aggregate to know about
  concerns it should not own (real-time push, metrics, point ledger).
- **Events published synchronously via a static bus** — rejected. Breaks
  testability and reintroduces hidden global state.
- **Service bus / message broker (RabbitMQ, Azure Service Bus)** — rejected
  for v1. Adds operational complexity without a clear benefit at single-node
  scale. The `IDomainEventDispatcher` abstraction leaves the door open if
  cross-process events become necessary.
- **Event sourcing** — rejected. The current model (aggregates hold current
  state, events describe transitions) fits the problem; event sourcing would
  require rebuilding the entire persistence strategy.
