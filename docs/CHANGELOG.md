# EcoNexus AI — Changelog
All notable changes to this project are documented in this file.

The format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/)
with these category headings, in this order:

  ### Added      — new features, files, or public surfaces
  ### Changed    — changes to existing behaviour or content
  ### Fixed      — bug fixes
  ### Removed    — removed features or files

Each entry is grouped under a version-and-tag heading, newest first:

  ## [vX.Y.Z-tag-name] - YYYY-MM-DD

Unreleased work sits at the top under `## [Unreleased] - YYYY-MM-DD`
until the next tag is cut, at which point it moves under its own
version heading.


## [Unreleased] - 2026-10-01

### Added
- 20 new Application-layer handler test files covering every handler (see backlog §1.1):
  CreateVehicle, UpdateStation, DeleteStation, AdvanceIntake, GetJobById,
  ListVehicles, ListJobs, ListFacilities, GetFacilityById, ListRewards,
  GetCitizenProfile, ListClassifications, ListMyReports, ListPointTransactions,
  GetStationById, ListStationReadings, CreateFacility, RecordIntake,
  ForecastStationFillLevel, WasteStationFillLevelChanged
- Footer component (Tailwind, no external UI kit) rendered on every route
- Full favicon set (favicon.ico, 16/32 PNG, apple-touch-icon, android-chrome 192/512)
- `docs/branding/logo.png` — official EcoNexus AI wordmark (512x279, 415 KB)
- `AwesomeAssertions 9.4.0` adopted for future test files (Apache-2.0 community fork)
- CI: coverlet.msbuild threshold of 60% on Application line coverage

### Changed
- README: test badge 356 -> 452; author hero image; Scalar references
- `web/index.html`: real title, description, theme-color, favicon links
- `web/src/App.tsx`: wraps `<Routes>` in a fragment to render `<Footer />`
- Landing page: removed "Step 9.5c" dev-note from the public view

### Fixed
- `web/vite.config.ts`: allow Cloudflare tunnel Host headers; correct `/api` proxy target

### Removed
- Legacy `web/public/favicon.svg` (Vite placeholder)

---

## [v0.15.17-step16-scope] - 2026-09-29

### Changed
- `docs/backlog.md` §2.1: Azure deployment fully scoped — service list, cost estimate, effort estimate, "awaiting subscription"
- README Roadmap: Azure entry clarified; notes Cloudflare Tunnel as the current public-URL substitute

---

## [v0.15.16-live-demo] - 2026-09-29

### Added
- README "Live Demo" section with a Cloudflare quick-tunnel URL

### Fixed
- `web/vite.config.ts`: `host: true` + `allowedHosts: ['.trycloudflare.com']` (Vite 403'd tunnel Host headers)

---

## [v0.15.15-readme-refresh] - 2026-09-29

### Changed
- README: test badge and counts corrected (356 -> 411; arch 8 -> 14; unit 194 -> 341)
- README: Swagger references replaced with Scalar (`/scalar/v1`)

---

## [v0.15.14-visit-date-fix] - 2026-09-29

### Fixed
- `RecordStationVisitHandler` used `DateTimeOffset.UtcNow` for the transaction's `OccurredAt`, while the domain dedup compared `OccurredAt.Date` to the client-supplied `VisitDate`. Once the calendar day rolled, the "one visit per station per calendar day" rule silently failed. Fix: derive `occurredAt` from `VisitDate`.

---

## [v0.15.13-arch-gif] - 2026-09-28

### Added
- 6 new architecture tests (8 -> 14 total)
- README Visual Demo section: `docs/demos/app-demo.mp4`

---

## [v0.15.12-app-tests] - 2026-09-28

### Added
- Tier-2 Application handler tests: 8 new test files (~100 tests)

### Changed
- Application line coverage 5.4% -> 29.6%

---

## [v0.15.11-docs] - 2026-09-28

### Added
- PRD v0.2 (Approved)
- 10 new ADRs (0006-0015)
- Expanded `docs/architecture/overview.md`
- `docs/backlog.md`, `docs/audit/2026-09-27/`

### Changed
- PRD v0.1 Draft -> v0.2 Approved

---
## [v0.15.10-docs-polish] - 2026-09-28

### Added
- README Visual Demo section with embedded forecast-chart screenshot

### Changed
- README test badge updated from tests-258 to tests-356
- README roadmap: Predictive Fill-Level Forecasting moved from "In Progress" to "Done"
- API documentation migrated from Swashbuckle to Scalar + native OpenAPI 3.1

### Removed
- Swashbuckle.AspNetCore package, config block, and unused using (FINDING-5.5 closed)

---

## [v0.15.9-test-coverage] - 2026-09-27

### Added
- 98 new unit tests across 8 new test files (Tier 1 backfill):
  - GreenPointLedgerTests (14), RecyclingMetricsCalculatorTests (12)
  - CitizenProfileTests (27), AssistantInteractionTests (15)
  - CollectionVehicleTests (8), RewardTests (10)
  - ToolCallTests (6), ToolDescriptorTests (6)

### Changed
- Domain layer line coverage: 41.5% -> 75.7%
- Total test count: 258 -> 356

---

## [v0.15.8-hardening] - 2026-09-27

### Added
- Config-driven CORS via CorsOptions with DataAnnotations (FINDING-5.1 closed)
- SecurityHeadersMiddleware: X-Content-Type-Options, X-Frame-Options, Referrer-Policy, Permissions-Policy, X-XSS-Protection (FINDING-5.3 closed)
- HSTS enabled outside Development
- JwtSettings DataAnnotations validation with ValidateOnStart (FINDING-5.2 closed)
- API versioning via Asp.Versioning.Mvc 8.1.1 (FINDING-5.4 closed)

### Changed
- All 7 API controllers now use [ApiVersion("1.0")] + api/v{version:apiVersion} routes
- HealthController remains unversioned (system endpoint)

---

## [v0.15.7-forecast-ui] - 2026-09-27

### Added
- GET /api/v1/stations/{id}/readings endpoint (windowed reading history)
- ForecastChart React component using Recharts (past readings + forecast trajectory + overflow marker)
- StationDetailPage with parallel data loading
- /stations/:id route + clickable rows on the stations list

### Fixed
- FillLevel now implements IComparable<FillLevel> + operators + Equals/GetHashCode
- WasteStationRepository sorts by s.CurrentFill instead of s.CurrentFill.Percent (latent EF Core translation bug)

---

## [v0.15.6-forecasting] - 2026-09-27

### Added
- FillLevelForecaster domain service (OLS linear regression on (hours, fillPercent))
- ForecastResult value object
- ForecastStationFillLevelQuery + Handler + Validator (MediatR CQRS)
- GET /api/v1/stations/{id}/forecast?windowHours=24 endpoint
- 15 unit tests for the forecaster + 7 integration tests

---

## [v0.15.5-readme-ci] - 2026-09-27

### Added
- Full project README with hero, badges, Mermaid architecture diagram, quickstart, tech stack, and API surface
- GitHub Actions CI workflow (`.github/workflows/ci.yml`):
  - Build + test all 232 tests on every push and PR
  - NuGet and npm dependency caching
  - Coverage and web build artifacts uploaded
- Issue templates (bug report, feature request)
- Pull request template with pre-merge checklist

### Fixed
- README test count corrected from 161 to 232

---

## [v0.15.4-observability] - 2026-09-26

### Added
- OpenTelemetry tracing + metrics pipeline
- Custom business meters from domain events
- Serilog trace-ID enrichment
- /health/live, /health/ready, /metrics endpoints
- 4 new integration tests (232 total)