# EcoNexus AI — Backlog

**Last Updated:** 2026-09-28
**Owner:** CTO

Prioritised queue of work that is not yet in the current step. Items are
grouped by theme, then ordered by impact / effort ratio within each group.

The goal is that the top of this file is *always* actionable.

---

## 1. Near-term (next 1–2 sessions)

### 1.1 Application-layer handler unit tests (Tier 2) — IN PROGRESS
**Why:** Integration tests exercise handlers end-to-end but a failure
surfaces as "HTTP 500", not "ScheduleJobHandler threw on capacity overflow".
Focused unit tests give precise failure signals and pin the branches.

**Status as of 2026-10-01:**
  - Application line-rate:   29.74% (baseline) -> 59.07% (measured)
  - Application branch-rate: 33.84% (baseline) -> 67.69% (measured)
  - 28 of 30 handlers now have dedicated unit-test files
  - 18 new test files this session:
      - `CreateVehicleHandler`          (6 test cases)
      - `UpdateStationHandler`          (9 test cases)
      - `DeleteStationHandler`          (4 test cases)
      - `AdvanceIntakeHandler`         (16 test cases)
      - `GetJobByIdHandler`             (3 test cases)
      - `ListVehiclesHandler`           (3 test cases)
      - `ListJobsHandler`               (4 test cases)
      - `ListFacilitiesHandler`         (4 test cases)
      - `GetFacilityByIdHandler`        (5 test cases)
      - `ListRewardsHandler`            (4 test cases)
      - `GetCitizenProfileHandler`      (5 test cases)
      - `ListClassificationsHandler`    (4 test cases)
      - `ListMyReportsHandler`          (4 test cases)
      - `ListPointTransactionsHandler`  (7 test cases)
      - `GetStationByIdHandler`         (4 test cases)
      - `ListStationReadingsHandler`    (5 test cases)
      - `CreateFacilityHandler`         (8 test cases)
      - `RecordIntakeHandler`           (7 test cases)
  - 102 new test cases; unit suite 341 -> 443

**Handlers with a dedicated unit-test file (28):**
  AdvanceIntake, AskAssistant, ClassifyWaste, CreateFacility,
  CreateStation, CreateVehicle, DeleteStation, FileReport,
  GetCitizenProfile, GetFacilityById, GetJobById, GetStationById,
  ListClassifications, ListFacilities, ListJobs, ListMyReports,
  ListPointTransactions, ListRewards, ListStationReadings,
  ListStations, ListVehicles, PreviewRoute, RecordIntake,
  RecordStationReading, RecordStationVisit, RedeemReward,
  ScheduleJob, UpdateStation

**Handlers still without a dedicated unit-test file (2):**
  Stations:  Events/WasteStationFillLevelChanged, ForecastStationFillLevel

**Approach:** read the handler source -> read the domain it touches ->
read the repository interface -> write tests covering every branch ->
verify filtered then full suite -> commit. One handler per commit.

**Effort:** ~10 minutes per simple handler (3-6 tests); ~30 minutes
for state machines (10-16 tests). 2 handlers remain.

**Source:** Audit FINDING-3.1.
### 1.2 Architecture test expansion
**Why:** The current 8 architecture tests only enforce dependency direction.
We can assert naming conventions, namespace placement, and forbidden types
(e.g. Domain must not reference `Microsoft.EntityFrameworkCore`).
**Scope:** ~6 new tests using `NetArchTest.Rules`.
**Effort:** 30 minutes.
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

### 3.1 ADR for event-driven architecture
**Why:** We have 12 domain events, a dispatcher, and pipeline behaviours —
but no ADR explaining *why* we chose domain events over service-to-service
calls.
**Effort:** 30 minutes.
**Source:** Self-identified.

### 3.2 Test strategy document
**Why:** A short doc explaining the three test projects, what each covers,
and when to add which kind of test.
**Effort:** 30 minutes.
**Source:** Self-identified.

### 3.3 API versioning policy
**Why:** `ADR-0014` explains the *mechanism* but not the *policy* (e.g. "v1
supported until at least X, deprecations announced Y months ahead").
**Effort:** 20 minutes.
**Source:** Self-identified.

---

## 4. Test quality

### 4.1 FluentAssertions migration
**Why:** Raw `Assert.Equal(x, y)` reads worse than `y.Should().Be(x)` and
fails with less helpful messages.
**Caution:** FluentAssertions 8.x requires a paid license for commercial use.
Use 7.x or Shouldly instead.
**Effort:** 1 session.
**Source:** Audit FINDING-3.3.

### 4.2 Add `coverlet` threshold to CI
**Why:** Prevent regressions in Domain coverage (currently 75.7%).
**Scope:** `Directory.Build.props` `<Threshold>70</Threshold>`, CI fails if
below.
**Effort:** 30 minutes.
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

### 6.1 Consolidate audit files
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

### 6.3 Update CHANGELOG format note
**Why:** Ensure future contributors use the same structure (Keep a Changelog
style with Added / Changed / Fixed / Removed).
**Effort:** 10 minutes.
**Source:** Self-identified.

---

### 6.4 FacilityIntake.AdvanceTo — unreachable terminal-stage branch

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

## Notes

- **Won't do (for now):** microservices split. Modular monolith remains the
  right shape; the seams are clean if we ever need to extract.
- **Won't do (ever):** rewriting to a different stack. The .NET + React +
  SQL Server combination is intentional and documented in the ADRs.