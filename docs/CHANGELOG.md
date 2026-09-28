# EcoNexus AI — Changelog

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