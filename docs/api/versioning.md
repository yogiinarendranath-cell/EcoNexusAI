# API Versioning Policy

**Last Updated:** 2026-10-02
**Owner:** CTO

> For the *why* behind URL-segment versioning, see
> [ADR-0014](../adr/0014-adopt-url-segment-api-versioning.md).
> This document covers the *how*: when to introduce v2, what counts
> as breaking, and how deprecation works.

---

## Current state

- **Version in use:** v1.0, at every `/api/v1/*` route.
- **Mechanism:** URL segment via `Asp.Versioning.Mvc`.
- **Reader:** `UrlSegmentApiVersionReader` — the version is part of the
  path, not a header or query parameter.
- **Discovery:** every response carries an
  `api-supported-versions: 1.0` header.
- **OpenAPI:** served by Scalar at `/scalar/v1`.

Every business controller declares:

```csharp
[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class StationsController : ControllerBase
```

System endpoints (`/health/live`, `/health/ready`, `/metrics`) are
**not versioned** — they are infrastructure, not product API.

---

## What counts as a breaking change

**Breaking (requires a new major version):**

- Removing a field from a response.
- Renaming a field in a response.
- Changing a field's type (e.g. `int` to `string`, `double` to `decimal`).
- Changing a field's semantics without changing its type or name.
- Removing an endpoint.
- Changing an endpoint's HTTP method or path.
- Adding a required field to a request body.
- Changing an existing HTTP status code to a different class
  (e.g. `200` to `204`, or `400` to `409`).

**Not breaking (safe to ship in the same version):**

- Adding a new optional field to a request.
- Adding a new field to a response (JSON clients ignore unknown fields).
- Adding a new endpoint.
- Adding a new optional query parameter.
- Changing an error message string.
- Adding a new value to an enum the client does not have to handle.

When in doubt, treat it as breaking.

---

## Version lifecycle

A new major version is introduced only when breaking changes have
accumulated to the point where keeping them out of v1 is impossible.
Multiple versions coexist indefinitely — v1 will not be removed because
v2 exists.

**Introducing v2 — the mechanical steps:**

1. Add a new controller (or duplicate the affected action) with
   `[ApiVersion("2.0")]`.
2. Keep the route template as-is:
   `[Route("api/v{version:apiVersion}/[controller]")]`.
3. Leave v1 code untouched. Both controllers are discovered by
   `AddApiVersioning`; the `api-supported-versions` header will
   report both.
4. The new v2 endpoint is automatically visible in Scalar (one
   grouped document per version).

If a controller's v2 differs from v1 in only one action, that action
can be overridden in the same controller using
`[MapToApiVersion("2.0")]` on the alternate action method.

---

## Deprecation policy

There is no formal sunset policy today because the API has a single
consumer (the EcoNexus frontend in this repo). The following is the
intended policy once external consumers exist:

- **Announce** — add a `Deprecation: true` note to the affected
  endpoint's Scalar documentation and to `docs/CHANGELOG.md`.
- **Warn** — add a `Sunset: <date>` response header on deprecated
  endpoints. HTTP-deprecation drafts describe this convention.
- **Sunset** — remove the deprecated version no earlier than 6 months
  after the announcement.

Until then, breaking changes to v1 are avoided entirely; additive
changes ship in v1 as usual.

---

## Client contract

What clients can rely on:

- **Field presence is additive.** New fields may appear in any
  response; clients must not fail on unknown keys.
- **Field types and semantics are stable within a version.** If either
  changes, a new version is introduced.
- **Status codes carry the documented meaning.** A `200` today is a
  `200` tomorrow.
- **Routes are stable within a version.** `/api/v1/stations` will not
  silently become `/api/v1/waste-stations`.

What clients must not rely on:

- Field ordering in JSON (order is not part of the contract).
- Specific error message text (structure is stable, prose is not).
- Internal identifiers' formats (Guid, string, opaque — treat as opaque).

---

## Related documents

- [ADR-0014 — URL-Segment API Versioning](../adr/0014-adopt-url-segment-api-versioning.md)
- [CHANGELOG](../CHANGELOG.md) — records every version-affecting change
- [Testing Strategy](../testing/strategy.md) — integration tests cover
  each endpoint's happy path and error paths per version

