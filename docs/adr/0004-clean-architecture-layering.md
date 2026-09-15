# ADR-0004: Clean Architecture Layering

- Status: Accepted
- Date: 2026-09-15

## Context
We need enforced dependency direction and a pure domain.

## Decision
- `EcoNexus.Domain` — no dependencies
- `EcoNexus.Contracts` — no dependencies
- `EcoNexus.Application` -> Domain, Contracts
- `EcoNexus.Infrastructure` -> Application, Domain
- `EcoNexus.Api` -> Application, Infrastructure, Contracts
- `EcoNexus.Worker` -> Application, Infrastructure

## Consequences
**Positive:** enforced boundaries, testable domain, easy to extract services.
**Negative:** more wiring, must respect reference rules.
**Follow-ups:** enforce via `EcoNexus.ArchitectureTests` (NetArchTest).
