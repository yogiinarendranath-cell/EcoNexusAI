# EcoNexus AI — Product Requirements Document

**Version:** 0.1 (trimmed — will expand as we build)
**Status:** Draft
**Owner:** CTO
**Last Updated:** 2026-09-15

---

## 1. Vision

EcoNexus AI is a cloud-native, AI-enabled smart waste management platform that connects
IoT-instrumented waste stations, predictive analytics, intelligent collection routing,
citizen engagement, and recycling operations into a single event-driven system.

**One-line pitch:**
> "An AI-powered Smart Waste & Recycling Network that predicts bin overflow, optimizes
> collection routes, classifies waste from photos, and gives city operators a real-time
> command center — built on .NET, React, Azure, and an event-driven architecture."

---

## 2. Personas

Citizen, Driver, Collection Manager, Operations Manager, Recycling Manager, City Admin,
Super Admin.

---

## 3. Modules (high level)

| Module | Purpose |
|--------|---------|
| Identity | Auth, users, roles, tenants |
| Stations | Waste station lifecycle + sensor readings |
| IoT Simulator | Synthetic sensor event generation |
| Realtime | SignalR live dashboard |
| Collection | Vehicles, drivers, jobs, routes |
| AI Classification | Image to waste category |
| Prediction | Fill-level forecasting (+4h, +8h) |
| Routing | Route optimization |
| Recycling | Facility intake + metrics |
| Citizen | Nearby stations, reports, green points |
| Notifications | Alerts + email |
| AI Assistant | Natural-language ops queries |
| Analytics | Reports and dashboards |
| Audit | Compliance logging |

---

## 4. Key User Journeys

- Citizen uploads waste photo -> gets disposal guidance -> earns points
- IoT sensor emits fill level -> event on bus -> dashboard updates in real time
- Prediction service forecasts overflow -> ops dashboard shows recommendation
- Collection Manager requests optimized route -> system returns ordered stops
- Facility records intake -> recycling metrics update on City Ops
- Ops Manager asks AI assistant "which stations will overflow tomorrow?"
- City Admin invites Ops Manager -> RBAC enforced on all endpoints

---

## 5. Non-Functional Requirements (targets)

| Area | Target |
|------|--------|
| API latency (p95) | < 300 ms read, < 800 ms write |
| AI classification (p95) | < 3 s |
| Dashboard update | < 1 s from event |
| Availability (design) | 99.9% |
| Security | OWASP Top 10, JWT 15m + refresh 7d |
| Observability | Serilog, metrics, traces |
| Testability | >= 70% Domain + Application |
| Portability | Full local via Docker Compose |

---

## 6. Out of Scope for v1

Real IoT hardware, payments, native mobile, i18n UI, ML training pipelines,
full microservices, multi-region, real-time driver navigation.

---

## 7. Success Criteria

- All modules above implemented and demonstrable
- docker compose up brings up API + DB + workers + frontend
- 15+ stations simulated live on dashboard
- AI classification works end-to-end with a real image upload
- Deployed to Azure with green CI/CD
- README, architecture diagrams, ADRs, and demo published

---

## 8. Approval

This is v0.1. Full PRD will be produced as we complete Step 2 and Step 3.
Any scope change requires a new version + an ADR.
