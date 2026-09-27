
## [v0.15.4-observability] - 2026-09-26
- OpenTelemetry tracing + metrics pipeline
- Custom business meters from domain events
- Serilog trace-ID enrichment
- /health/live, /health/ready, /metrics endpoints
- 4 new integration tests (232 total)
- Canonical close of phase 15.3

## [v0.15.5-readme-ci] - 2026-09-27

### Added
- Full project README with hero, badges, Mermaid architecture diagram, quickstart, tech stack, and API surface
- GitHub Actions CI workflow (`.github/workflows/ci.yml`):
  - Build + test all 232 tests on every push and PR
  - NuGet and npm dependency caching
  - Coverage and web build artifacts uploaded
- Issue templates (bug report, feature request)
- Pull request template with pre-merge checklist
- Project health audit (docs/audit/2026-09-27/) covering 6 passes

### Fixed
- README test count corrected from 161 to 232
