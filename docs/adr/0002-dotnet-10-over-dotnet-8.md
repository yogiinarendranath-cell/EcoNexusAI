# ADR-0002: Use .NET 10

- Status: Accepted
- Date: 2026-09-15

## Context
Developer machine has .NET 10.0.204 SDK. .NET 10 is current but not LTS.
Azure App Service native support may lag behind .NET 8 LTS.

## Decision
Target `net10.0` for all projects. Reassess at Phase 16 (Azure deployment).

## Consequences
**Positive:** latest language features, no downgrade friction.
**Negative:** shorter support window; possible retarget before Azure deploy.
**Follow-ups:** revisit at Phase 16.
