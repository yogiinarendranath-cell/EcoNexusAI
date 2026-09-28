# ADR-0008: JWT Bearer with Refresh Tokens

**Status:** Accepted
**Date:** 2026-09-22
**Decision Makers:** CTO, Lead Engineer

---

## Context

The platform has two frontends (operator console, citizen app) and an API. It
needs an authentication mechanism that:

- Works across the browser and future mobile clients.
- Supports role-based access control.
- Allows long-lived sessions without exposing long-lived tokens.
- Enables revocation.

---

## Decision

Use **JWT access tokens + opaque refresh tokens**, issued by ASP.NET Core
Identity, validated by the JWT Bearer middleware.

- Access token lifetime: **15 minutes**.
- Refresh token lifetime: **7 days**.
- Refresh tokens are stored server-side; refresh rotates the pair.
- Logout revokes the refresh token.
- Passwords: minimum 8 chars, uppercase + lowercase + digit + symbol,
  minimum 4 unique characters.
- Lockout: 5 failed attempts → 15 minutes.

---

## Consequences

**Positive:**
- Stateless validation of access tokens → no session lookup per request.
- Refresh rotation limits the blast radius of a stolen access token.
- ASP.NET Core Identity gives us password hashing, lockout, and roles for free.
- RBAC via `[Authorize(Roles = ...)]` is declarative.

**Negative:**
- Revocation of an access token is not immediate (up to 15 minutes latency).
- Refresh-token storage requires a persistent store (SQL Server).
- Data Protection keys must be shared across instances in a scaled deployment
  (Azure Key Vault or a shared Redis, later).

---

## Alternatives Considered

- **Cookie-based auth** — poor fit for a future mobile client; CSRF concerns.
- **Long-lived JWTs only** — no revocation path.
- **OAuth 2.0 / OpenID Connect via an external IdP (Auth0, Entra ID)** —
  appropriate for production; adds a dependency and cost we don't need yet.