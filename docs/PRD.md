# EcoNexus AI — Product Requirements Document

**Version:** 0.2
**Status:** Approved — matches shipped state as of 2026-09-28
**Owner:** CTO
**Last Updated:** 2026-09-28

---

## 1. Vision

EcoNexus AI is a cloud-native, AI-enabled smart waste management platform that connects
IoT-instrumented waste stations, predictive analytics, intelligent collection routing,
citizen engagement, and recycling operations into a single event-driven system.

**One-line pitch:**

> "An AI-powered Smart Waste & Recycling Network that predicts bin overflow, optimizes
> collection routes, classifies waste from photos, and gives city operators a real-time
> command center — built on .NET, React, and an event-driven architecture."

---

## 2. Personas

| Persona | Goals | Pain Points |
|---|---|---|
| Citizen | Dispose of waste correctly, earn rewards, see nearby stations | Doesn't know where to take unusual items; finds overflowing bins |
| Driver | Receive optimized routes, record collections | Wasteful back-and-forth drives, no prioritisation |
| Collection Manager | Plan jobs, manage vehicles | Manual dispatch, no fill-level input |
| Operations Manager | Live oversight of stations, alerts, routes | No real-time view, react to complaints |
| Recycling Manager | Track intakes, processing, sustainability metrics | Opaque paper trail, no per-category metrics |
| City Admin | Policy decisions, budget, compliance | Fragmented data across vendors |
| Super Admin | Platform operations, users, roles | Manual provisioning, no audit trail |

---

## 3. Modules

| Module | Purpose | Status |
|---|---|---|
| Identity | Auth, users, roles, tenants | ✅ Shipped |
| Stations | Waste station lifecycle + sensor readings | ✅ Shipped |
| IoT Simulator | Synthetic sensor event generation (Worker) | ✅ Shipped |
| Realtime | SignalR live dashboard | ✅ Shipped |
| Collection | Vehicles, drivers, jobs, routes | ✅ Shipped |
| AI Classification | Image → waste category | ✅ Shipped |
| Prediction | Fill-level forecasting | ✅ Shipped |
| Operations Assistant | Natural-language ops queries | ✅ Shipped |
| Recycling | Facility intake, processing stages, metrics | ✅ Shipped |
| Citizen | Profile, points, rewards, reports | ✅ Shipped |
| Notifications | Email / SMS | ⬜ Planned |
| Analytics | Dashboards, exports | ⬜ Planned |

---

## 4. Key User Journeys

1. **Citizen classifies waste** — opens app → photographs item → gets category + disposal guidance → optionally visits a station → earns green points → sees balance.
2. **Citizen visits station** — checks nearby stations → picks one → app records visit → awards points → advances streak.
3. **Citizen redeems reward** — browses reward catalog → picks reward → checks balance → redeems → transaction recorded.
4. **Operator monitors stations** — opens operations dashboard → sees stations sorted by fill level → spots critical → investigates via chart.
5. **Operator forecasts overflow** — clicks station → views fill-level chart → reads "predicted overflow in X minutes" → decides to schedule a collection.
6. **Operator previews a route** — opens jobs page → selects candidate stations → picks vehicle → previews optimized route → sees per-stop reasoning and capacity usage.
7. **Operator schedules job** — commits the previewed route → job + stops cascade → driver sees route.
8. **Operator queries the assistant** — asks natural-language question ("which stations are critical?") → assistant picks correct tool → returns answer → interaction audited.
9. **Recycling manager records intake** — opens facility → records material + weight → advances through processing stages → sees per-facility metrics.
10. **Driver completes route** — receives job → marks stops completed → collection events raised → citizen-visible state updates.

---

## 5. Non-Functional Requirements

| Category | Target |
|---|---|
| **Performance** | p95 API response < 300 ms at 100 rps |
| **Availability** | 99.5% target for API and Worker |
| **Scalability** | Horizontal scaling of API behind a load balancer; Worker is single-instance |
| **Security** | JWT access tokens (15 min), refresh tokens (7 days), lockout after 5 failed attempts, HSTS, security headers on every response |
| **Rate limiting** | Per-policy windows on auth, writes, reads, assistant |
| **Observability** | OpenTelemetry traces + metrics, Serilog JSON with trace-ID, Prometheus scrape endpoint, health checks (live + ready) |
| **Data** | SQL Server, EF Core migrations, no data loss on restart |
| **Browser support** | Latest Chrome, Edge, Firefox, Safari (frontend) |

---

## 6. Out of Scope for v1

- Mobile native apps (web only)
- Multi-tenant isolation beyond a shared schema
- Push notifications (email/SMS deferred)
- Payment processing
- Real IoT hardware integration (simulator only)
- Real LLM production deployment (Mock + Ollama supported)

---

## 7. Success Criteria

- ✅ All 17 build-order phases complete (Phase 16 Azure deployment pending)
- ✅ 356 automated tests green (292 unit + 8 architecture + 56 integration)
- ✅ Clean Architecture dependency rules enforced by architecture tests
- ✅ 3 AI capabilities operational (classification, forecasting, assistant)
- ✅ Real-time dashboard working end-to-end
- ⬜ 90% test coverage on the Domain layer (currently 75.7%)
- ⬜ Public deployment to Azure (blocked on subscription)

---

## 8. Architecture (summary)

Clean Architecture, CQRS via MediatR, EF Core + SQL Server, JWT identity, SignalR
realtime, OpenTelemetry observability. Full detail in
[docs/architecture/overview.md](architecture/overview.md) and
[docs/adr/](adr/).

---

## 9. API Surface

Versioned via URL segment (`/api/v1/...`). Full OpenAPI 3.1 spec at
`/openapi/v1.json`, interactive docs at `/scalar/v1`.

| Area | Endpoints |
|---|---|
| Auth | POST register, POST login, POST refresh, POST logout, GET me |
| Stations | GET, GET /{id}, POST, PUT /{id}, DELETE /{id}, POST /{id}/readings, GET /{id}/readings, GET /{id}/forecast |
| Collection Jobs | GET, GET /{id}, POST /preview-route, POST |
| Collection Vehicles | GET, POST |
| Recycling Facilities | GET, GET /{id}, POST, POST /{id}/intakes, POST /{id}/intakes/{intakeId}/advance |
| Citizen | GET /profile, GET /points/history, POST /visits, GET /rewards, POST /rewards/{id}/redeem, POST /reports, GET /reports/mine, POST /classify, GET /classifications |
| Operations Assistant | POST /ask |
| System | GET /api/v1/ping, GET /health/live, GET /health/ready, GET /metrics |
| Realtime | SignalR hub at /hubs/operations |

---

## 10. Approval

| Role | Name | Date |
|---|---|---|
| CTO / Product Owner | Narendra Nath | 2026-09-28 |