# Project Health Audit — Findings

**Date:** 2026-09-27
**Last Updated:** 2026-09-28
**Status codes:** 🔴 OPEN · 🟡 PARTIAL · ✅ CLOSED · ⚪ SKIPPED

---

## Closed Findings

| ID | Original Finding | Closed In | Notes |
|---|---|---|---|
| FINDING-2.1 | `.gitignore` allegedly missing `bin/` and `obj/` | Session 1 | False alarm — patterns `[Bb]in/` and `[Oo]bj/` exist at lines 33–34 |
| FINDING-3.2 | Coverlet coverage not emitted | Session 1 | False alarm — correct invocation is `--collect:"XPlat Code Coverage"` (my script used wrong syntax) |
| FINDING-4.1 | Phase 10 (Predictive Forecasting) missing | Session 2 | Shipped: `FillLevelForecaster`, CQRS feature, endpoint, 15 unit + 7 integration tests |
| FINDING-4.6 | CollectionVehiclesController endpoints not detected | Session 2 | False alarm — regex missed the `[HttpGet]`/`[HttpPost]` attributes with no path |
| FINDING-5.1 | CORS policy hardcoded to dev origins | Session 4b | Closed via `CorsOptions` + config binding |
| FINDING-5.2 | JWT settings not validated on startup | Session 4b | Closed via DataAnnotations + `.ValidateDataAnnotations()` |
| FINDING-5.3 | No HSTS, no security headers | Session 4b | Closed via `SecurityHeadersMiddleware` + conditional `UseHsts()` |
| FINDING-5.4 | Hardcoded `/api/v1/` route prefixes | Session 4b | Closed via `Asp.Versioning.Mvc` + `[ApiVersion]` on 7 controllers |
| FINDING-5.5 | Dual Swagger config (Swashbuckle + MS OpenAPI) | Session 6 | Closed via Scalar migration; Swashbuckle removed entirely |
| FINDING-6.1 | `OrderBy(s => s.CurrentFill.Percent)` fails EF Core translation | Session 3 | Closed by making `FillLevel` `IComparable` + operators and sorting by `s.CurrentFill` |
| FINDING-2.2 | `docs/backlog.md` empty | Session 7 | Closed — populated with 6 sections |
| FINDING-2.3 | `docs/PRD.md` at v0.1 Draft, out of date | Session 7 | Closed — rewritten as v0.2 Approved |
| FINDING-4.2 | Only 5 ADRs for 30 tags of decisions | Session 7 | Closed — added ADRs 0006–0015 |
| FINDING-4.3 | Architecture doc thin (no diagrams) | Session 7 | Closed — expanded to 221 lines with C4 + data flow + deployment topology |

---

## Partial Findings

### FINDING-3.1 — Application-layer test coverage gap 🟡
**Status:** Domain closed, Application still open.
**Domain:** 41.5% → 75.7% after Session 5 Tier-1 backfill (98 new tests).
**Application:** ~5% — handlers, validators, and assistant tools remain at
0% unit coverage.
**Plan:** Tier-2 backfill (see backlog §1.1) — target 7 high-branch handlers.

### FINDING-4.5 — Portfolio polish 🟡
**Done:** README full rewrite (Session 1), Visual Demo section with screenshot
(Session 6), roadmap corrected.
**Pending:** Demo GIF/video, blog post, richer screenshots.
**Plan:** See backlog §1.3, §6.2.

### FINDING-4.4 — Azure deployment 🔴
**Status:** Blocked on subscription. CI pipeline works; deployment pipeline
does not exist yet.
**Options:** Azure for Students (needs school email), Render (needs Postgres
migration), Railway ($5/mo), Cloudflare Tunnel (free but PC-dependent).
**Plan:** See backlog §2.

---

## Minor / Cosmetic Findings

### FINDING-3.3 — Raw xUnit asserts (no FluentAssertions) 🟡
Cosmetic. Improves test readability. **Caution:** FluentAssertions 8.x has a
paid license for commercial use; use 7.x or Shouldly.

### FINDING-3.4 — Architecture tests only cover dependency direction 🟡
Can be expanded with naming conventions, namespace rules, and forbidden-type
assertions. ~6 new tests.

### OBS-1.1 — Two `v0.15.3-*` tags (naming drift) ⚪
Cosmetic. `v0.15.3-health-checks` and `v0.15.3-otel`. Harmless but worth
avoiding in future.

### FINDING-3.5 — Coverage denominator includes Migrations and DTOs ⚪
Cosmetic. The "6.5% overall coverage" number was misleading; the real number
is Domain 75.7% + Application 5.4%.

---

## Notes for Future Audits

- **Prefer concrete tool invocations.** Wrong syntax (`/p:CollectCoverage`)
  produced a false finding (FINDING-3.2).
- **Confirm false alarms before logging.** Two of the "closed" findings were
  never real problems (FINDING-2.1, FINDING-4.6).
- **Audit docs use `Select-String` markers, not human reading.** Saves
  5–10 minutes per pass.
- **Never assume `.gitignore` is missing.** Use `git check-ignore -v` — the
  definitive proof.