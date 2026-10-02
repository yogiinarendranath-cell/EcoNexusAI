# EcoNexus AI — Backlog

**Last Updated:** 2026-09-28
**Owner:** CTO

Prioritised queue of work that is not yet in the current step. Items are
grouped by theme, then ordered by impact / effort ratio within each group.

The goal is that the top of this file is *always* actionable.

---

## 1. Near-term (next 1–2 sessions)

### 1.1 Application-layer handler unit tests (Tier 2) — COMPLETE
**Why:** Integration tests exercise handlers end-to-end but a failure
surfaces as "HTTP 500", not "ScheduleJobHandler threw on capacity overflow".
Focused unit tests give precise failure signals and pin the branches.

**Status as of 2026-10-01 — COMPLETE:**
  - Application line-rate:   29.74% (baseline) -> 62.66% (measured)
  - Application branch-rate: 33.84% (baseline) -> 71.53% (measured)
  - 30 of 30 handlers have dedicated unit-test files
  - 20 new test files this session:
      - `CreateVehicleHandler`           (6 test cases)
      - `UpdateStationHandler`           (9 test cases)
      - `DeleteStationHandler`           (4 test cases)
      - `AdvanceIntakeHandler`          (16 test cases)
      - `GetJobByIdHandler`              (3 test cases)
      - `ListVehiclesHandler`            (3 test cases)
      - `ListJobsHandler`                (4 test cases)
      - `ListFacilitiesHandler`          (4 test cases)
      - `GetFacilityByIdHandler`         (5 test cases)
      - `ListRewardsHandler`             (4 test cases)
      - `GetCitizenProfileHandler`       (5 test cases)
      - `ListClassificationsHandler`     (4 test cases)
      - `ListMyReportsHandler`           (4 test cases)
      - `ListPointTransactionsHandler`   (7 test cases)
      - `GetStationByIdHandler`          (4 test cases)
      - `ListStationReadingsHandler`     (5 test cases)
      - `CreateFacilityHandler`          (8 test cases)
      - `RecordIntakeHandler`            (7 test cases)
      - `ForecastStationFillLevelHandler` (5 test cases)
      - `WasteStationFillLevelChangedHandler` (4 test cases)
  - 111 new test cases; unit suite 341 -> 452

**All 30 handlers have a dedicated unit-test file:**
  AdvanceIntake, AskAssistant, ClassifyWaste, CreateFacility,
  CreateStation, CreateVehicle, DeleteStation, FileReport,
  ForecastStationFillLevel, GetCitizenProfile, GetFacilityById,
  GetJobById, GetStationById, ListClassifications, ListFacilities,
  ListJobs, ListMyReports, ListPointTransactions, ListRewards,
  ListStationReadings, ListStations, ListVehicles, PreviewRoute,
  RecordIntake, RecordStationReading, RecordStationVisit,
  RedeemReward, ScheduleJob, UpdateStation,
  WasteStationFillLevelChanged

**Follow-up work (future, not blocking):**
  - §4.1 FluentAssertions migration — improves failure messages
  - §4.2 Coverlet coverage threshold in CI — prevents regressions
  - Integration tests continue to provide the end-to-end safety net.

**Approach used:** read the handler source -> read the domain it touches
-> read the repository interface -> write tests covering every branch ->
verify filtered then full suite -> commit. One handler per commit.

**Source:** Audit FINDING-3.1.
### 1.2 Architecture test expansion — DONE (2026-10-02)
**Why:** Enforce not just dependency direction but naming, placement, shape,
and forbidden references, so architecture claims stay true as the codebase grows.
**Implemented:**
  - `DependencyRulesTests.cs` (8 tests) — Domain, Application, Contracts
    dependency direction.
  - `ConventionTests.cs` (6 tests) — Domain purity (EF Core, ASP.NET),
    handler/validator/controller naming, entities namespace.
  - `LayeringAndShapeTests.cs` (11 tests, new) — Domain purity (MediatR,
    Microsoft.Extensions.Logging, Serilog), DomainEvents/VO placement,
    Command/Query feature-folder placement, aggregate sealing,
    Application no-EF-Core-direct, Controllers no-persistence-direct.
  - Total: 25 tests. All green on first run against existing code.
**Effort:** done.
**Source:** Audit FINDING-3.4.

### 1.3 Add a "Getting Started" video / GIF
**Why:** The README's Visual Demo section has a screenshot but no motion.
A 20–30 second GIF showing the forecast chart rendering end-to-end is the
single highest-value asset for an interview.
**Effort:** 30 minutes (screen recording + ffmpeg to GIF).
**Source:** Audit FINDING-4.5 (partial).

---

## 2. Deployment (blocked)

### 2.1 Azure deployment — scoped, awaiting subscription

**Why:** A live URL is the difference between "impressive code" and
"impressive product". A public URL is currently served via Cloudflare
Tunnel (see README "Live Demo"); Azure will replace the tunnel with a
permanent hosted deployment.

**Blocked on:** Azure subscription + `az login`. Check Azure for Students
eligibility (requires school email) or save for a paid tier.

**Scope when unblocked:**

| Piece | Azure service | Notes |
|---|---|---|
| API | Container Apps or App Service (Linux) | Reuse `docker/Dockerfile.api` |
| Worker | Container App (background) | Reuse `docker/Dockerfile.worker` |
| Database | Azure SQL (Basic tier) | EF Core migrations already exist |
| Blob storage | Storage Account | Replaces Azurite |
| Secrets | Key Vault | Connection strings + JWT signing key |
| Frontend | Static Web Apps | Build `web/` -> SWA |
| Observability | Application Insights | Wire OpenTelemetry exporter |
| Registry | Azure Container Registry | Host API/Worker images |
| CI/CD | GitHub Actions + OIDC | Federated creds, no stored secrets |
| IaC | `infrastructure/main.bicep` | `infrastructure/` folder reserved for this |

**Cost (demo):** ~$10-25/mo, or $0 if torn down after screenshots.
Container Apps scale-to-zero; SQL Basic ~$5/mo; Static Web Apps free tier.

**Effort:** 6-11 hours total (first manual deploy, then Bicep codification,
then CI deploy job).

**Source:** Audit FINDING-4.4.

### 2.2 Alternative: Render / Railway deployment
**Why:** Free/cheap alternatives if Azure is unavailable.
**Cost:** Time (Postgres migration) or money ($5/mo Railway).
**Effort:** 1–2 sessions.
**Source:** Session 4 conversation.

---

## 3. Documentation improvements

### 3.1 ADR for event-driven architecture — DONE (2026-10-02)
**Why:** We have 12 domain events, a dispatcher, and pipeline behaviours —
but no ADR explaining *why* we chose domain events over service-to-service
calls.
**Delivered:** [ADR-0016](../adr/0016-event-driven-architecture.md) records
the decision, alternatives considered, and consequences (post-commit dispatch,
`DomainEventNotification<T>` wrapper, no retry/dead-letter today).
**Effort:** done.
**Source:** Self-identified.

### 3.2 Test strategy document — DONE (2026-10-02)
**Why:** A short doc explaining the three test projects, what each covers,
and when to add which kind of test.
**Delivered:** [docs/testing/strategy.md](../testing/strategy.md) — three-tier
breakdown, directory layout, naming conventions, mocking discipline, test
isolation, CI enforcement, and what is deliberately not tested.
**Effort:** done.
**Source:** Self-identified.

### 3.3 API versioning policy — DONE (2026-10-02)
**Why:** `ADR-0014` explains the *mechanism* but not the *policy* (e.g. "v1
supported until at least X, deprecations announced Y months ahead").
**Delivered:** [docs/api/versioning.md](../api/versioning.md) — current state,
breaking-change list, version lifecycle, deprecation policy, client contract.
**Effort:** done.
**Source:** Self-identified.

---

## 4. Test quality

### 4.1 Adopt AwesomeAssertions for new tests (scope revised)
**Why:** Assertion failure messages from raw xUnit `Assert.*` calls are
terse ("Expected 5, Actual 3"). FluentAssertions-style `.Should()`
assertions include the expression and more context.

**Scope decision (2026-10-01):**
  The original backlog entry proposed a full migration to
  FluentAssertions. Two things changed:

  1. FluentAssertions v8+ relicensed to a paid Community License for
     commercial use. The Apache-2.0 community fork **AwesomeAssertions**
     is the drop-in replacement (same `.Should()` API, same syntax).
  2. The test suite now contains **861** raw `Assert.*` calls across
     30 handler test files. A mechanical rewrite of 861 passing
     assertions risks introducing silent test bugs (a mistranslated
     assertion that still passes) with zero functional gain.

**Revised scope:**
  - Add AwesomeAssertions to the test project so new tests can use it.
  - Do **not** rewrite existing tests. Existing `Assert.*` calls stay
    as-is; they are accurate, readable, and green.
  - New test files should use `.Should()` where it aids clarity.

**Status:**
  - `AwesomeAssertions 9.4.0` added to `Directory.Packages.props`.
  - Referenced from `tests/EcoNexus.UnitTests/EcoNexus.UnitTests.csproj`.
  - Build clean, 452/452 tests green.

**Effort:** done (10 minutes). No further work required.

**Source:** Audit FINDING-3.3 (revised).
### 4.2 Add `coverlet` threshold to CI — DONE (2026-10-01)
**Why:** Prevent regressions in Application handler coverage as the
project grows.
**Implemented:**
  - `coverlet.msbuild 10.0.1` added to `Directory.Packages.props`.
  - `tests/EcoNexus.UnitTests.csproj` uses `coverlet.msbuild` (the
    collector does not support thresholds; only the MSBuild
    integration does).
  - `.github/workflows/ci.yml`: unit test step runs
    `/p:Threshold=60 /p:ThresholdType=line /p:ThresholdStat=Total`,
    scoped to `[EcoNexus.Application]*` so Infrastructure / Domain /
    Contracts (covered by their own tests) don't drag the total
    below the threshold.
  - Current measured Application line-rate: 61.59% (buffer: 1.59 pts).
**Effort:** done.
**Source:** Self-identified.

---

## 5. Feature enhancements

### 5.1 Predictive forecasting: exponential smoothing algorithm
**Why:** The current linear regression assumes constant fill rate. Real
stations have daily patterns (fuller in the evening).
**Scope:** Add `ExponentialSmoothingForecaster` alongside `FillLevelForecaster`;
choose via strategy pattern in the handler.
**Effort:** 1 session.
**Source:** Deferred during Session 2.

### 5.2 Notification service
**Why:** Citizens want email/SMS when a station is empty again or when points
are earned.
**Scope:** `INotificationSender` abstraction; SendGrid + Twilio adapters.
**Effort:** 2 sessions.
**Source:** PRD §6 (out of scope for v1 but on the long-term roadmap).

### 5.3 Multi-tenant support
**Why:** Cities using the platform should not see each other's data.
**Scope:** Tenant column on every aggregate + query filter + tenant resolution
from JWT claim.
**Effort:** 3–4 sessions.
**Source:** PRD §6.

### 5.4 Mobile native app
**Why:** Citizens are mobile-first.
**Scope:** React Native or .NET MAUI client consuming the same API.
**Effort:** Multi-session project.
**Source:** PRD §6.

---

## 6. Tech debt / cleanup

### 6.1 Consolidate audit files — DONE (2026-10-01)
**Why:** `docs/audit-20260918-083247.txt` and `docs/audit-20260918-083338.txt`
are from a prior audit. The current audit lives under
`docs/audit/2026-09-27/`. Consolidate or archive.
**Effort:** 15 minutes.
**Source:** Self-identified.

### 6.2 Fill the `docs/screenshots/` folder
**Why:** One chart screenshot exists. Screenshots for stations list, jobs
preview, and assistant Q&A would strengthen the README.
**Effort:** 30 minutes.
**Source:** Audit FINDING-4.5 (partial).

### 6.3 Update CHANGELOG format note — DONE (2026-10-01)
**Why:** Ensure future contributors use the same structure (Keep a Changelog
style with Added / Changed / Fixed / Removed).
**Effort:** 10 minutes.
**Source:** Self-identified.

---

### 6.4 FacilityIntake.AdvanceTo — unreachable terminal-stage branch — DONE (documented 2026-10-01)

`AdvanceTo` checks the backward transition before the terminal-stage guard.
Because `Landfilled(4)` is the highest enum value, any other target from
`Landfilled` fails the backward check first. The `Stage == Landfilled`
clause of the terminal-stage guard is therefore unreachable.

Two clean options:
  a. Reorder — check the terminal guard before the backward guard.
  b. Remove the unreachable `Landfilled` case from the terminal guard.

Not a functional bug: `Landfilled` is a dead-end either way; the exception
message just says "backwards" instead of "terminal" in that case. Cosmetic.

Source: `AdvanceIntakeHandlerTests.Handle_AdvanceFromLandfilled_ThrowsBackwardsException`.

---

### 6.5 Track GitHub Actions runner deprecations
**Why:** The CI workflow's actions (`actions/checkout@v4`,
`actions/setup-dotnet@v4`, `actions/setup-node@v4`,
`actions/upload-artifact@v4`, `actions/cache@v4`) all target Node.js 20,
which is deprecated on GitHub runners. GitHub is currently forcing them
onto Node.js 24 automatically, but future action versions will require
`@v5+`.
**Scope:**
  - Bump each action to its next major version that ships Node-24-native.
  - Verify the workflow stays green after the bump.
**Effort:** 15 minutes.
**Source:** CI run #63 (2026-10-01) — Node 20 deprecation notice.

### 6.6 Ubuntu 26 migration window
**Why:** GitHub's `ubuntu-latest` label migrates to Ubuntu 26 on
**2026-10-19**. Behavior may shift (glibc, OpenSSL, default package
versions, SQL Server container compatibility).
**Scope:**
  - Once Ubuntu 26 is available, run the workflow against it.
  - Address compatibility issues that surface.
  - Alternatively pin to `ubuntu-24.04` to defer — but the migration is
    unavoidable long-term.
**Effort:** 30 minutes.
**Source:** CI run #63 (2026-10-01) — ubuntu-latest deprecation notice.

---

## Notes

- **Won't do (for now):** microservices split. Modular monolith remains the
  right shape; the seams are clean if we ever need to extract.
- **Won't do (ever):** rewriting to a different stack. The .NET + React +
  SQL Server combination is intentional and documented in the ADRs.