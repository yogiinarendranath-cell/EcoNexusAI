# ADR-0014: Adopt URL-Segment API Versioning

**Status:** Accepted
**Date:** 2026-09-27
**Decision Makers:** CTO, Lead Engineer

---

## Context

Before this decision, API routes were hardcoded as `[Route("api/v1/...")]`.
Adding a v2 would have required duplicating every controller, updating every
client, and managing the split manually.

We need a versioning strategy that:
- Preserves existing v1 URLs.
- Allows adding v2 without touching v1 code.
- Reports supported versions to clients.
- Is visible in the URL (so logs, caches, and clients see it).

Options:
- URL segment: `/api/v1/stations`
- Header: `api-version: 1.0`
- Query string: `/api/stations?api-version=1.0`
- Media type: `Accept: application/vnd.econexus.v1+json`

---

## Decision

Adopt **URL-segment versioning** via `Asp.Versioning.Mvc` 8.1.1.

- Routes: `[Route("api/v{version:apiVersion}/...")]`.
- Controllers: `[ApiVersion("1.0")]`.
- Default version is 1.0.
- `AssumeDefaultVersionWhenUnspecified = true` (helps future controllers with
  unversioned templates, e.g. `HealthController`).
- `ReportApiVersions = true` — every response carries an
  `api-supported-versions` header.

The `HealthController` is intentionally **not versioned** — it is a system
endpoint, not a product API.

---

## Consequences

**Positive:**
- Version is visible in URLs, logs, proxies, and caches.
- No client changes for v1 — same URL as before (`/api/v1/stations`).
- Adding v2 is a new controller annotation, no v1 changes required.
- The `api-supported-versions` response header lets clients discover versions.

**Negative:**
- A URL with an unsupported version (e.g. `/api/v2/stations`) returns 404, not
  400 — because no controller declares v2. This is intentional: "this resource
  does not exist at this version".
- Because the route template requires the version segment,
  `/api/stations` (no version) returns 404. Callers must include `/v1/`.

---

## Alternatives Considered

- **Header versioning** — version invisible in URLs; harder to debug; caches
  key on URL.
- **Query-string versioning** — pollutes every URL and looks less like a
  resource.
- **Media-type versioning** — the "correct" REST approach but burdens every
  client with content negotiation.
- **Hardcoded v1 in routes** — what we had before; unscalable.