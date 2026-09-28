# ADR-0006: Use MediatR for CQRS

**Status:** Accepted
**Date:** 2026-09-22
**Decision Makers:** CTO, Lead Engineer

---

## Context

The Application layer needs a consistent way to route commands and queries to
their handlers. Two patterns are common:

1. Direct service injection — controller calls a service interface.
2. Mediator pattern — controller sends a request object to a dispatcher,
   which routes it to the appropriate handler.

We also want cross-cutting concerns (validation, logging, transactions) to
apply uniformly to every request without duplicating code in every controller.

---

## Decision

Adopt **MediatR 12** and the CQRS pattern.

- Every command and query is a `record` implementing `IRequest<TResponse>`.
- Every handler implements `IRequestHandler<TRequest, TResponse>`.
- Cross-cutting concerns are implemented as pipeline behaviors
  (`IPipelineBehavior<TRequest, TResponse>`).
- Controllers inject `ISender` (not `IMediator`) and call `_sender.Send(...)`.

---

## Consequences

**Positive:**
- Every request flows through a uniform pipeline; validation and logging are
  one behavior each, not repeated in 25 handlers.
- Handlers are unit-testable in isolation — no ASP.NET dependencies.
- Command/query objects are self-describing DTOs.

**Negative:**
- Extra indirection: following a controller call to its handler requires
  "go to definition" on the request type, not just the service.
- Runtime resolution overhead (negligible at our scale).
- Team members unfamiliar with CQRS take a short ramp-up.

---

## Alternatives Considered

- **Direct service injection** — simpler, but cross-cutting concerns leak into
  every controller.
- **Custom dispatcher** — reinventing MediatR; less ecosystem support.
- **Wolverine / MassTransit mediator** — heavier; overkill for v1.