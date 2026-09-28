# ADR-0009: SignalR for Real-Time

**Status:** Accepted
**Date:** 2026-09-23
**Decision Makers:** CTO, Lead Engineer

---

## Context

The operations dashboard needs to see station fill-level changes as they happen.
Polling the API every few seconds works but wastes bandwidth and adds latency.

Options:
- Polling (`setInterval` + `fetch`)
- Server-Sent Events (SSE)
- Raw WebSockets
- SignalR

---

## Decision

Use **ASP.NET Core SignalR**, exposed via a single hub at `/hubs/operations`.

- Domain events raised by aggregates are mapped to SignalR broadcasts via
  `IOperationsNotifier` (Application abstraction) and
  `SignalROperationsNotifier` (Infrastructure implementation).
- Application and Domain layers know nothing about SignalR — they talk to
  `IOperationsNotifier`.
- The client uses `@microsoft/signalr`.

---

## Consequences

**Positive:**
- Auto-fallback from WebSockets → SSE → long-polling based on browser/proxy.
- Group management and reconnect handling out of the box.
- Typed hub methods are easy to reason about.
- The `IOperationsNotifier` abstraction means we can swap in a different
  realtime backbone (e.g. Azure SignalR Service) without touching the domain.

**Negative:**
- SignalR keeps an in-memory connection map → a scaled deployment needs
  a backplane (Redis or Azure SignalR Service).
- Broadcasts from the domain event handler require the dispatcher to be in the
  same request scope as the HTTP call (already the case).
- Testing real-time is harder than testing HTTP — hence dedicated
  integration tests.

---

## Alternatives Considered

- **Polling** — simple but inefficient and adds visible latency.
- **SSE** — unidirectional; we may later want server→client *and* client→server.
- **Raw WebSockets** — more work than SignalR for no benefit.
- **Azure SignalR Service** — deferred; useful once we scale out.