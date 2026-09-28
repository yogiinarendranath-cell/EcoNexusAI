# EcoNexus AI — Architecture Overview

**Last Updated:** 2026-09-28
**Scope:** High-level structural view of the deployed system.

For decision-level detail, see [docs/adr/](../adr/). For product scope, see
[docs/PRD.md](../PRD.md).

---

## 1. Layered Architecture

EcoNexus AI follows **Clean Architecture** with four concentric layers.

```
┌─────────────────────────────────────────────────────────────────┐
│                       Presentation (Api)                        │
│  Controllers · Middleware · Health checks · Swagger/Scalar      │
└───────────────────────────┬─────────────────────────────────────┘
                            │ depends on
┌───────────────────────────▼─────────────────────────────────────┐
│                    Infrastructure (Adapters)                    │
│  EF Core · Identity · SignalR · AI providers · OpenTelemetry   │
└───────────────────────────┬─────────────────────────────────────┘
                            │ implements ports from
┌───────────────────────────▼─────────────────────────────────────┐
│                   Application (Use Cases / CQRS)                │
│  MediatR handlers · Validators · Pipeline behaviours           │
└───────────────────────────┬─────────────────────────────────────┘
                            │ depends on
┌───────────────────────────▼─────────────────────────────────────┐
│                         Domain (Core)                           │
│  Aggregates · Value Objects · Domain Events · Domain Services  │
└─────────────────────────────────────────────────────────────────┘
```

**Contracts** is a sibling project of Domain — pure DTOs, no logic — referenced
by Application, Infrastructure, and Api.

### Dependency Rule (enforced by tests)

```
Domain          ← no dependencies (pure)
   ▲
   │
Application     ← depends on Domain + Contracts
   ▲
   │
Infrastructure  ← depends on Application + Domain (implements Application ports)
   ▲
   │
Api / Worker    ← composition roots
```

`tests/EcoNexus.ArchitectureTests/DependencyRulesTests.cs` asserts that no
reverse dependency exists. Eight rules, all green on every CI run.

---

## 2. C4 Model — Level 2 (Containers)

```
                        ┌───────────────────┐
                        │     Citizen       │
                        │   (mobile web)    │
                        └─────────┬─────────┘
                                  │ HTTPS + SignalR
                                  ▼
       ┌────────────────────────────────────────────┐
       │      Operator Console (React + Vite)       │
       │      Served as static files in prod        │
       └────────────┬──────────────────┬────────────┘
                    │ REST             │ WebSocket
                    ▼                  ▼
       ┌──────────────────────┐  ┌────────────────────┐
       │  EcoNexus.Api        │  │  OperationsHub     │
       │  ASP.NET Core 10     │◄─┤  (SignalR)         │
       └───┬──────────┬───────┘  └────────────────────┘
           │          │
   writes  │          │ reads/writes
           ▼          ▼
       ┌──────────────────┐
       │  SQL Server      │
       │  EcoNexus DB     │
       └────────┬─────────┘
                │
                │ shares
                ▼
       ┌──────────────────────────┐       ┌────────────────────┐
       │  EcoNexus.Worker         │──────▶│  Ollama (optional) │
       │  IoT station simulator   │       │  LLM provider      │
       └──────────────────────────┘       └────────────────────┘
```

### Containers (in C4 terms)

| Container | Technology | Responsibility |
|---|---|---|
| **Operator Console** | React 19 + Vite 8 + TypeScript | Ops UI: stations, jobs, recycling, assistant |
| **Citizen Web App** | React (same build, different routes) | Citizen: classify, points, reports, rewards |
| **EcoNexus.Api** | ASP.NET Core 10 | HTTP API + SignalR hub |
| **EcoNexus.Worker** | .NET 10 BackgroundService | IoT simulator (sensor readings) |
| **SQL Server** | SQL Server 2022 | Relational persistence |
| **Azurite** | Azure Storage emulator | Blob storage emulator (dev) |
| **Ollama** (optional) | Local LLM | Waste classification + assistant LLM |

---

## 3. Data Flow — Recording a Station Reading

This is the flagship flow: from a synthetic sensor reading to a real-time
dashboard update.

```
1. Worker → POST /api/v1/stations/{id}/readings
                │
2. Controller → ISender.Send(RecordStationReadingCommand)
                │
3. MediatR pipeline:
      LoggingBehavior → logs "Handling RecordStationReadingCommand"
                        │
      ValidationBehavior → runs FluentValidation validator
                        │
      Handler  →  loads station via IWasteStationRepository
               →  station.RecordReading(fillLevel, temp, battery, timestamp)
                    │
                    ├─▶ appends StationReading child
                    ├─▶ raises WasteStationFillLevelChangedEvent
                    └─▶ possibly raises WasteStationReachedCriticalFillEvent
               →  repository.SaveChangesAsync()
               →  DbContext.SaveChangesAsync()
                    │
4. DomainEventDispatcher (in DbContext.SaveChangesAsync)
                    │
                    └─▶ publishes each raised event via MediatR as
                        DomainEventNotification<T>
                             │
                             ├─▶ WasteStationFillLevelChangedHandler
                             │      └─▶ IOperationsNotifier → SignalR broadcast
                             │
                             └─▶ WasteStationFillLevelChangedMetricsHandler
                                    └─▶ EcoNexusMeters counter++
                    │
5. HTTP 201 response with new reading ID + fill % + critical flag
                    │
6. SignalR "StationFillLevelChanged" broadcast to subscribed dashboard clients
                    │
7. Prometheus scrape at /metrics exposes the incremented counter
```

**Key insight:** the domain event dispatcher runs **inside** the EF Core
`SaveChangesAsync()` transaction. Either everything succeeds (DB write + event
dispatch) or nothing does.

---

## 4. Cross-Cutting Concerns

| Concern | Where | Implementation |
|---|---|---|
| **Logging** | Api, Worker | Serilog with trace-ID enrichment |
| **Validation** | Application | FluentValidation via MediatR pipeline behaviour |
| **Exceptions** | Api | `GlobalExceptionHandler` maps domain exceptions to HTTP status codes |
| **AuthN/AuthZ** | Api | JWT Bearer + `[Authorize(Roles=...)]` |
| **Rate limiting** | Api | `RateLimitingExtensions` — 4 named policies |
| **CORS** | Api | `CorsOptions` bound from config |
| **Security headers** | Api | `SecurityHeadersMiddleware` (5 headers + HSTS in prod) |
| **Output caching** | Api | `UseOutputCache` on list endpoints |
| **API versioning** | Api | `Asp.Versioning.Mvc` URL-segment |
| **Tracing / metrics** | Api, Worker | OpenTelemetry (HTTP, EF Core, runtime, business meters) |
| **Health checks** | Api | `/health/live`, `/health/ready` |

---

## 5. Data Model (Entity Overview)

```
AggregateRoot
├── WasteStation ──────── owns StationReading[]
├── CollectionJob ─────── owns RouteStop[]
├── CollectionVehicle
├── RecyclingFacility ─── owns FacilityIntake[]
├── CitizenProfile ────── owns GreenPointTransaction[]
│                         owns WasteClassification[]
├── Reward
├── Alert
├── RefreshToken
└── AssistantInteraction

ValueObjects: FillLevel · Location · StationCode · Weight
              ToolCall · ToolDescriptor · WasteClassificationResult
              ForecastResult

DomainEvents (12): AlertRaised, CitizenReportFiled,
                   CollectionJobCompleted/Started,
                   GreenPointsEarned, RecyclingIntakeAdvanced/Recorded,
                   RewardRedeemed, RouteStopCompleted,
                   WasteStationCollected/FillLevelChanged/ReachedCriticalFill
```

Persistence is via EF Core with value converters for single-scalar value
objects and owned entities for multi-property ones (see ADR-0012).

---

## 6. Deployment Topology (target)

```
       ┌──────────────────────────────────────────────────┐
       │              Azure (or equivalent)               │
       │                                                  │
       │  ┌─────────────────┐    ┌───────────────────┐    │
       │  │  App Service    │    │  Azure SQL DB     │    │
       │  │  (Linux)        │───▶│  EcoNexus         │    │
       │  │  EcoNexus.Api   │    └───────────────────┘    │
       │  └────────┬────────┘                             │
       │           │                                      │
       │  ┌────────▼────────┐   ┌────────────────────┐    │
       │  │ Static Web App  │   │  Container Apps    │    │
       │  │  React frontend │   │  EcoNexus.Worker   │    │
       │  └─────────────────┘   └────────────────────┘    │
       │                                                  │
       │  ┌─────────────────┐   ┌───────────────────┐    │
       │  │ Key Vault       │   │ Application       │    │
       │  │ (secrets)       │   │ Insights (traces) │    │
       │  └─────────────────┘   └───────────────────┘    │
       └──────────────────────────────────────────────────┘
```

**Status:** not yet deployed. CI is green (see `.github/workflows/ci.yml`),
but Azure deployment is blocked on subscription (see [backlog](../backlog.md)).

---

## 7. Local Development Stack

`docker-compose.yml` starts:

| Service | Port | Purpose |
|---|---|---|
| `sqlserver` | 1433 | SQL Server 2022 |
| `azurite` | 10000–10002 | Azure Storage emulator |
| `api` | 5067 | EcoNexus.Api in container |
| `worker` | — | EcoNexus.Worker in container |

For day-to-day development, run the API and Worker via `dotnet run` and only
run SQL Server + Azurite via compose.

---

## 8. Quality Gates

Every commit on `main` must pass:

| Gate | Enforced by |
|---|---|
| **Build** (Release, `TreatWarningsAsErrors`) | CI |
| **Tests** — 356 total | CI |
| **Architecture rules** — 8 dependency-direction assertions | CI |
| **NuGet audit** — fails on high/critical CVE | `Directory.Build.props` |
| **Web build** — TypeScript + Vite | CI |
| **Web lint** — oxlint | CI |

No commit lands on `main` unless all gates are green.