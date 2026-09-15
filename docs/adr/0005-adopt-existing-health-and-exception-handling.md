# ADR-0005: Adopt Existing Health Endpoint and Global Exception Handler

- Status: Accepted
- Date: 2026-09-15

## Context
Prior to formal Step 0, a `HealthController` and `GlobalExceptionHandler` were added
to `EcoNexus.Api`. Both align with our NFRs (availability, observability).

## Decision
Adopt both as part of the baseline. Review for correctness in Step 2. Do not remove.

## Consequences
**Positive:** foundation already in place; less work in Step 2.
**Negative:** slight plan deviation; to be reviewed.
**Follow-ups:** extend exception handler for domain exceptions in a later step.
