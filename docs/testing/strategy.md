# Testing Strategy

**Last Updated:** 2026-10-02
**Owner:** CTO

---

## Purpose

EcoNexus AI has a three-tier test suite. Each tier answers a different
question, runs at a different speed, and catches a different class of
defect. This document records what each tier covers, when to write each
kind of test, and the conventions that keep the suite consistent.

Current totals (all green in CI):

| Tier | Tests | Runtime (local) |
|---|---|---|
| Unit | 452 | ~500 ms |
| Architecture | 25 | ~1 s |
| Integration | 56 | ~45 s |
| **Total** | **533** | ~50 s |

---

## The three tiers

### 1. Unit tests — `tests/EcoNexus.UnitTests`

**Question they answer:** does this class do what it says in isolation?

**Scope:** domain aggregates and value objects, application handlers
(commands and queries), domain services, a small set of infrastructure
adapters with pure logic (AI provider shaping, cache key builders).

**Dependencies:** NSubstitute for repository and abstract-service fakes.
Real domain objects — a fake `CitizenProfile` would hide domain rules.
No database, no HTTP, no DI container.

**Coverage target:** Application layer must stay at or above 60% line
rate (enforced by CI via coverlet.msbuild). Domain is currently ~78% and
has no enforced floor — adding tests is encouraged but not gated.

**When to write one:** any new handler, any new domain method with a
branch, any bug fix that can be reproduced without infrastructure.
**Every bug fix gets a failing test first.**

---

### 2. Architecture tests — `tests/EcoNexus.ArchitectureTests`

**Question they answer:** does the codebase still obey its own rules?

**Scope:** dependency direction between layers, forbidden references
(Domain must not reference EF Core / ASP.NET / MediatR / Serilog /
Microsoft.Extensions.Logging), naming conventions (handler, validator,
controller), placement (entities in `Entities`, VOs in `ValueObjects`,
events in `DomainEvents`, commands/queries under `Features`), and shape
(aggregates sealed).

**Dependencies:** `NetArchTest.Rules` — reflection over compiled
assemblies. No mocks, no runtime.

**Coverage target:** 100% of stated architecture rules. If a rule is not
enforced by a test, it is not a rule.

**When to write one:** any time a new pattern is introduced that the team
wants to enforce. Adding an aggregate root to a new namespace, adding a
new layer, or discovering a violation that slipped past review.

---

### 3. Integration tests — `tests/EcoNexus.IntegrationTests`

**Question they answer:** does the whole thing work end-to-end?

**Scope:** HTTP endpoints via `WebApplicationFactory<Program>`, real
in-process API, real database (SQL Server LocalDB locally, SQL Server
container in CI), real JWT flow, real SignalR hub, real rate limiting,
real caching. No mocks of the framework — the point is the wiring.

**Dependencies:** `Microsoft.AspNetCore.Mvc.Testing`,
`Microsoft.AspNetCore.SignalR.Client`. Each test class gets a fresh
database via `EcoNexusApiFactory`, migrated and role-seeded in
`InitializeAsync`.

**Coverage target:** every HTTP endpoint should have at least one
happy-path test and one authorization/validation test.

**When to write one:** any new controller or endpoint. Any change to
middleware (auth, CORS, rate limiting, exceptions). Any change to the
DI configuration. Any cross-cutting behavior that only shows up when
the whole stack runs.

---

## Directory layout

```
tests/
  EcoNexus.UnitTests/
    Domain/               Per-aggregate, per-VO, per-service tests
    Features/             Mirror of EcoNexus.Application/Features
    Infrastructure/       Unit-level infrastructure adapter tests
  EcoNexus.ArchitectureTests/
    DependencyRulesTests.cs
    ConventionTests.cs
    LayeringAndShapeTests.cs
  EcoNexus.IntegrationTests/
    Auth/  Caching/  Citizen/  Features/  Infrastructure/
    Observability/  Operations/  RateLimiting/  Realtime/  Stations/
```

**Rule:** the folder structure of a test project mirrors the folder
structure of the code under test. Finding the test for a class should
be a directory-tree lookup, not a search.

---


## Naming conventions

Test method names are `Subject_<Condition>_<ExpectedOutcome>` for
handler tests, and `Method_<Condition>_<ExpectedOutcome>` for domain
methods. Examples from the suite:

- `Handle_ValidRequest_CreatesVehicleAndReturnsResponse`
- `Handle_DuplicateRegistrationNumber_ThrowsConflictException`
- `Handle_NegativeCapacity_ThrowsArgumentOutOfRangeException`
- `AdvanceFromLandfilled_ThrowsBackwardsException`

The name is a sentence. If the test fails, the name alone should tell
you what broke.

**Do not** prefix with `Test`, `Should`, or `When`. The `[Fact]` or
`[Theory]` attribute already marks it as a test.

---

## Mocking discipline

**Mock interfaces, not entities.** Repositories (`I*Repository`),
external services (`IUserDirectory`, `IWasteClassificationService`),
and real-time abstractions (`IOperationsNotifier`) are mocked with
`NSubstitute`.

**Never mock a domain aggregate.** Construct a real one via its factory
(`WasteStation.Create`, `RecyclingFacility.Create`). If the aggregate
needs setup, call its real methods (`RecordReading`, `AdvanceIntake`).
Mocking an aggregate hides the rules you are trying to test.

**Never mock `TimeProvider`.** Use a real `TimeProvider` substitute or
`TimeProvider.System` — but prefer explicit time injection over `DateTime.Now`.

**One exception:** `TimeProvider` is mocked in tests that need to
control time (e.g. `ListStationReadingsHandlerTests` verifies `since` is
computed from a fixed instant).

---

## Test isolation

**Unit tests** are isolated by construction — each test creates its own
subject and fakes. No shared state, no ordering.

**Integration tests** each get a fresh database. `EcoNexusApiFactory`
creates a GUID-suffixed database name per factory instance, migrates it
in `InitializeAsync`, and drops it in `DisposeAsync`. Parallel test
classes share a SQL Server instance but not a database.

**Migration gate:** a static `SemaphoreSlim` serializes
database-creation across parallel classes to avoid `sp_getapplock`
contention and concurrent `CREATE DATABASE` races.

**Environment portability:** `EcoNexusApiFactory.BuildConnectionString`
uses LocalDB by default (Windows dev) and honors
`ECONEXUS_TEST_SQL_CONNECTION` when set (CI, macOS, Linux). This makes
the suite runnable on any platform with a reachable SQL Server.

---
## CI enforcement

Every push to `main` runs the full suite on `ubuntu-latest` with a
SQL Server service container. The pipeline fails on:

- **Any failing test** — unit, architecture, or integration.
- **Application line coverage below 60%** — enforced by
  `coverlet.msbuild` with `/p:Threshold=60 /p:ThresholdType=line
  /p:ThresholdStat=Total`, scoped to `[EcoNexus.Application]*` so
  Infrastructure, Domain, and Contracts do not drag the total.
- **Any xUnit analyzer rule violation** treated as an error — for
  example `Assert.Equal(1, collection.Count)` (use `Assert.Single`),
  `Assert.Equal(0, collection.Count)` (use `Assert.Empty`), and
  `Assert.Equal(true, x)` (use `Assert.True`).

Coverage artifacts (Cobertura XML) are uploaded per run for 7 days.

**Assertion library:** new tests may use either raw xUnit `Assert.*` or
the `.Should()` syntax from `AwesomeAssertions` (Apache 2.0 community
fork of FluentAssertions). Existing tests are not being rewritten — the
861 raw assertions remain; new tests may choose either style.

---

## What we do not test

The following are deliberately excluded from the suite:

- **EF Core migrations themselves.** Tested by running them against a
  fresh database in integration tests, not by inspecting the generated
  code.
- **Entity Framework configurations in isolation.** Type conversion,
  table naming, and relationship mappings are exercised through
  integration tests against real SQL Server.
- **Generated DTOs.** Records with no logic are covered by the handler
  tests that construct them.
- **Program.cs composition root.** DI wiring is covered indirectly by
  integration tests; a unit test of `AddInfrastructure()` would mostly
  assert that ASP.NET works.
- **Third-party library behavior.** We test our usage, not `MediatR` or
  `FluentValidation` internals.

---

## Running the suite

```bash
# All tests
dotnet test

# One tier
dotnet test tests/EcoNexus.UnitTests
dotnet test tests/EcoNexus.ArchitectureTests
dotnet test tests/EcoNexus.IntegrationTests

# One handler group
dotnet test tests/EcoNexus.UnitTests --filter "FullyQualifiedName~CreateVehicleHandlerTests"

# With coverage
dotnet test tests/EcoNexus.UnitTests --collect:"XPlat Code Coverage"
```

Coverage output lands in `tests/EcoNexus.UnitTests/TestResults/`.

---

## Summary

| Question | Tier | Speed |
|---|---|---|
| Does this class behave as designed? | Unit | ms |
| Does the codebase still obey its architecture? | Architecture | seconds |
| Does the system work end-to-end? | Integration | tens of seconds |

Every tier is enforced in CI. Breaking any tier fails the build.

