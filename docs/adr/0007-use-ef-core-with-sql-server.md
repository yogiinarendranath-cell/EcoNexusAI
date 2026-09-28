# ADR-0007: Use EF Core with SQL Server

**Status:** Accepted
**Date:** 2026-09-22
**Decision Makers:** CTO, Lead Engineer

---

## Context

The platform needs an ORM and a relational database. The ecosystem choice
affects performance, tooling, migrations, and cloud deployment options.

Candidates considered:
- EF Core + SQL Server
- EF Core + PostgreSQL
- Dapper + any relational DB
- MongoDB

---

## Decision

Adopt **EF Core 10** as the ORM and **SQL Server** as the database.

- All persistence lives in `EcoNexus.Infrastructure.Persistence`.
- The `EcoNexusDbContext` is the only place EF Core is referenced from
  outside the `Infrastructure` project.
- All schema changes are done via **EF Core migrations** (never manual SQL).
- Repositories translate `DbUpdateConcurrencyException` into
  `ConcurrencyConflictException` at the boundary so the Application layer
  stays EF-agnostic.

---

## Consequences

**Positive:**
- First-class tooling: migrations, scaffolding, LINQ translation.
- SQL Server is ubiquitous in enterprise .NET; hiring is easy.
- Azure SQL is a drop-in target when we deploy.
- The value-converter support is mature enough for our value objects.

**Negative:**
- SQL Server licensing cost for production (mitigated by Azure SQL tiers).
- Some LINQ expressions do not translate (e.g. complex value-object
  projections) — see ADR-0012 for the mitigation.
- Ties the deployment story somewhat to the Microsoft ecosystem.

---

## Alternatives Considered

- **PostgreSQL** — excellent OSS option; deferred. The cost of switching later
  is a one-file provider change plus migration regeneration.
- **Dapper** — faster but requires hand-written SQL and mapping. Not worth the
  loss of migrations and LINQ.
- **MongoDB** — the domain is highly relational (stations own readings, jobs
  own stops). Document modelling would fight the invariants.