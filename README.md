<p align="center">
  <img src="docs/branding/logo.png" alt="EcoNexus AI" width="260" />
</p>

# EcoNexus AI

> **AI-Powered Smart Waste & Recycling Network** â€” a cloud-native platform that connects IoT-instrumented waste stations, predictive analytics, intelligent collection routing, citizen engagement, and recycling operations through an event-driven architecture.

[![CI](https://github.com/yogiinarendranath-cell/EcoNexusAI/actions/workflows/ci.yml/badge.svg)](https://github.com/yogiinarendranath-cell/EcoNexusAI/actions/workflows/ci.yml)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com)
[![Tests](https://img.shields.io/badge/tests-533%20passing-brightgreen)](#testing)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](#license)

## Live Demo

_No public demo is currently hosted._ The application runs locally;
see [Quickstart](#quickstart) for full setup. A permanent hosted
demo is planned; see [docs/backlog.md](docs/backlog.md) section 2.1.

---

## Visual Demo

### App Demo

<video src="docs/demos/app-demo.mp4" controls muted playsinline width="800"></video>

*2-minute walkthrough: stations list → station detail with the forecast
chart → back to the list. Recorded on 2026-09-28.*

### Fill-Level Forecast Chart

![Fill-level forecast chart for station ST-001](docs/screenshots/station-detail-forecast.png)

**What you are looking at:** the station-detail page for `ST-001`. The
green line shows actual fill-level readings from the IoT sensors; the
blue dashed line is the linear-regression forecast projected forward;
the red `Overflow` marker shows the predicted overflow time; and the
amber line at 90% is the critical-fill threshold. The header reports
`Predicted overflow in X min` along with a confidence score and the
number of samples used.

This feature is served by `GET /api/v1/stations/{id}/forecast` and backed
by the pure domain service `FillLevelForecaster` (Ordinary Least Squares
on `(hours, fillPercent)`, confidence from R² and sample size).

### Application Screenshots

![EcoNexus AI landing page](docs/screenshots/landing-page.png)

*Landing page — hero, tagline, and live network stats.*

![Stations list](docs/screenshots/stations-list.png)

*Stations list — sorted by fill level, critical stations first.*

![Vehicles list](docs/screenshots/vehicles-list.png)

*Collection vehicles — fleet registry.*

---

## Table of Contents

- [What It Does](#what-it-does)
- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Quickstart](#quickstart)
- [Project Structure](#project-structure)
- [Testing](#testing)
- [Observability](#observability)
- [API Surface](#api-surface)
- [Documentation](#documentation)
- [Roadmap](#roadmap)

---

## What It Does

EcoNexus AI is a full-stack waste-management platform for smart cities. It provides:

- **Smart Waste Stations** â€” IoT-instrumented bins that report fill levels in real time
- **AI Waste Classification** â€” Citizens upload a photo; the platform classifies the waste type and provides disposal guidance
- **Predictive Fill-Level Forecasting** â€” Forecasts when a station will overflow based on recent sensor readings
- **Route Optimization** â€” Prioritized collection routes based on urgency, distance, and vehicle capacity
- **Recycling Facility Workflow** â€” Intake recording, processing stage tracking, and sustainability metrics
- **Citizen Engagement** â€” Green points ledger, reward redemption, station visits, and report filing
- **AI Operations Assistant** â€” Natural-language queries against live operational data
- **Real-Time Dashboard** â€” SignalR pushes station fill-level changes to connected operators

---

## Architecture

EcoNexus AI follows **Clean Architecture** with strict dependency rules enforced by architecture tests.

```mermaid
graph TB
    subgraph Clients
        React["React + Vite Frontend"]
        Browser["Browser"]
    end

    subgraph Api["EcoNexus.Api"]
        Controllers["Controllers"]
        Middleware["Middleware: CORS / Auth / RateLimit / Exceptions"]
    end

    subgraph Application["EcoNexus.Application"]
        MediatR["MediatR Pipeline - CQRS"]
        Handlers["Command and Query Handlers"]
        Validators["FluentValidation"]
    end

    subgraph Domain["EcoNexus.Domain"]
        Aggregates["Entities and Aggregates"]
        VOs["Value Objects"]
        DomainEvents["Domain Events"]
        Services["Domain Services"]
    end

    subgraph Infrastructure["EcoNexus.Infrastructure"]
        EFCore["EF Core - SQL Server"]
        Identity["JWT Identity - RBAC"]
        SignalRHub["SignalR Hub"]
        AI["AI Providers - Ollama / Mock"]
        Observability["OpenTelemetry - Serilog - Prometheus"]
    end

    subgraph Worker["EcoNexus.Worker"]
        IoTSim["IoT Station Simulator"]
    end

    React -->|HTTPS| Controllers
    Browser -->|SignalR| SignalRHub
    Controllers --> MediatR
    MediatR --> Handlers
    Handlers --> Validators
    Handlers --> Aggregates
    Handlers --> Services
    Handlers --> EFCore
    Handlers --> AI
    IoTSim --> EFCore
    EFCore --> SQL[("SQL Server")]
```

### Dependency Rule (enforced by tests)

```
Domain          <- no dependencies (pure)
   ^
   |
Application     <- depends on Domain
   ^
   |
Infrastructure  <- depends on Application, Domain
   ^
   |
Api / Worker    <- composition roots
```

**Zero violations.** See `tests/EcoNexus.ArchitectureTests/DependencyRulesTests.cs`.

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 10, C# 14 |
| Web | ASP.NET Core, Controllers + Minimal APIs |
| CQRS | MediatR 12 |
| Validation | FluentValidation 12 |
| Data | EF Core 10, SQL Server |
| Identity | ASP.NET Core Identity, JWT Bearer, RBAC |
| Real-Time | SignalR |
| AI | Provider-agnostic (Ollama / Mock) |
| Observability | OpenTelemetry, Serilog, Prometheus |
| Frontend | React 19, Vite 8, TypeScript 6, Zustand, TanStack Query, Tailwind 4 |
| Testing | xUnit, NSubstitute, NetArchTest, WebApplicationFactory, coverlet |
| Dev Environment | Docker Compose (SQL Server + Azurite + API + Worker) |
| Package Management | Directory.Packages.props (central versioning) |

---

## Quickstart

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop) (for SQL Server + Azurite)
- [Node.js 20+](https://nodejs.org/)

### 1. Clone and restore

```bash
git clone https://github.com/yogiinarendranath-cell/EcoNexusAI.git
cd EcoNexusAI
dotnet restore
```

### 2. Start backing services

```bash
docker compose up -d
```

Starts SQL Server on `localhost:1433` and Azurite (Azure Storage emulator) on `localhost:10000-10002`.

### 3. Apply migrations

```bash
dotnet ef database update --project src/EcoNexus.Infrastructure --startup-project src/EcoNexus.Api
```

### 4. Run the API

```bash
dotnet run --project src/EcoNexus.Api
```

- API: `https://localhost:7001`
- Scalar API reference: `https://localhost:7001/scalar/v1`
- Health: `https://localhost:7001/health/live`
- Metrics: `https://localhost:7001/metrics`

### 5. Run the IoT simulator (separate terminal)

```bash
dotnet run --project src/EcoNexus.Worker
```

### 6. Run the frontend (separate terminal)

```bash
cd web
npm install
npm run dev
```

Frontend at `http://localhost:5173`.

---

## Project Structure

```
EcoNexusAI/
â”œâ”€â”€ src/
â”‚   â”œâ”€â”€ EcoNexus.Api/            HTTP layer, controllers, middleware, composition root
â”‚   â”œâ”€â”€ EcoNexus.Application/    CQRS features, MediatR handlers, validators
â”‚   â”œâ”€â”€ EcoNexus.Contracts/      DTOs shared with clients
â”‚   â”œâ”€â”€ EcoNexus.Domain/         Entities, value objects, events, domain services
â”‚   â”œâ”€â”€ EcoNexus.Infrastructure/ EF Core, Identity, SignalR, AI providers, observability
â”‚   â””â”€â”€ EcoNexus.Worker/         IoT simulator + background workers
â”œâ”€â”€ tests/
â”‚   â”œâ”€â”€ EcoNexus.ArchitectureTests/  Dependency rule enforcement (NetArchTest)
â”‚   â”œâ”€â”€ EcoNexus.IntegrationTests/   End-to-end HTTP tests (WebApplicationFactory)
â”‚   â””â”€â”€ EcoNexus.UnitTests/          Domain + application unit tests
â”œâ”€â”€ web/                         React + Vite + TypeScript frontend
â”œâ”€â”€ docs/                        PRD, ADRs, architecture, changelog, backlog
â”œâ”€â”€ docker/                      Dockerfiles for API and Worker
â”œâ”€â”€ docker-compose.yml           Local dev stack
â”œâ”€â”€ Directory.Build.props        Shared MSBuild settings
â””â”€â”€ Directory.Packages.props     Central NuGet version management
```

---

## Testing

**533 tests, all green:**

| Project | Count | Scope |
|---|---|---|
| EcoNexus.ArchitectureTests | 25 | Layer dependencies, naming, placement, shape, forbidden refs |
| EcoNexus.UnitTests | 452 | Domain aggregates, value objects, handlers, services |
| EcoNexus.IntegrationTests | 56 | HTTP endpoints, auth flows, real-time, caching, rate limiting |

Run all tests:

```bash
dotnet test
```

Run with coverage:

```bash
dotnet test tests/EcoNexus.UnitTests --collect:"XPlat Code Coverage"
```

Coverage output is a Cobertura XML under `tests/EcoNexus.UnitTests/TestResults/`.

**Architecture rules enforced in CI** â€” if you break the dependency direction, the build fails.

---

## Observability

- **Structured logging** â€” Serilog with trace-ID enrichment
- **Distributed tracing** â€” OpenTelemetry (HTTP, EF Core, outbound HTTP)
- **Metrics** â€” exposed at `/metrics` in Prometheus format
- **Custom business meters** â€” `econexus_station_reading_recorded`, etc.
- **Health checks** â€” `/health/live` (liveness), `/health/ready` (readiness)
- **Correlation** â€” every log line includes `trace:{TraceId} span:{SpanId}`

> **Production note:** The `/metrics` endpoint must be firewalled to internal traffic only â€” see `Program.cs`.

---

## API Surface

Versioned via route prefix (`/api/v1/...`):

| Area | Endpoints |
|---|---|
| Auth | `POST /register`, `POST /login`, `POST /refresh`, `POST /logout`, `GET /me` |
| Stations | `GET`, `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}`, `POST /{id}/readings`, `GET /{id}/readings`, `GET /{id}/forecast` |
| Collection Jobs | `GET`, `GET /{id}`, `POST /preview-route`, `POST` |
| Collection Vehicles | `GET`, `POST` |
| Recycling Facilities | `GET`, `GET /{id}`, `POST`, `POST /{id}/intakes`, `POST /{id}/intakes/{intakeId}/advance` |
| Citizen | `GET /profile`, `GET /points/history`, `POST /visits`, `GET /rewards`, `POST /rewards/{id}/redeem`, `POST /reports`, `GET /reports/mine`, `POST /classify`, `GET /classifications` |
| Operations Assistant | `POST /ask` (RBAC: SuperAdmin, CityAdmin, OperationsManager) |
| System | `GET /api/v1/ping`, `GET /health/live`, `GET /health/ready`, `GET /metrics` |
| Real-Time | SignalR hub at `/hubs/operations` |

Full OpenAPI spec at `/scalar/v1` when running locally.

---

## Documentation

- [Product Requirements Document](docs/PRD.md)
- [Architecture Overview](docs/architecture/overview.md)
- [Architecture Decision Records](docs/adr/)
- [Changelog](docs/CHANGELOG.md)
- [Backlog](docs/backlog.md)

---

## Roadmap

**Done:**
- Clean Architecture + CQRS foundation
- Identity (JWT + refresh tokens + RBAC)
- Waste Stations, Collection Jobs, Vehicles, Recycling Facilities
- AI Waste Classification (provider-agnostic)
- AI Operations Assistant (8 tools)
- Route Optimization
- Citizen app (web)
- Real-time updates (SignalR)
- IoT Station Simulator
- Observability (OpenTelemetry + Serilog + Prometheus)
- Predictive Fill-Level Forecasting (linear regression + UI chart)
- Application-layer test coverage (30 of 30 handlers, 62.7% line / 71.5% branch)

**In Progress:**
- Azure deployment — scoped in [docs/backlog.md](docs/backlog.md) §2.1;
  awaiting subscription. Public URL currently via Cloudflare Tunnel.

**Planned:**
- Notification service (email/SMS)
- Mobile app
- Multi-tenant support

See [docs/backlog.md](docs/backlog.md) for the current prioritized queue.

---

## Author

**Narendra N** â€” [@yogiinarendranath-cell](https://github.com/yogiinarendranath-cell)

Built as a portfolio project demonstrating production-grade .NET architecture.
