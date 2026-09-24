# EcoNexus AI

**AI-Powered Smart Waste & Recycling Network.**

A cloud-native, AI-enabled smart waste management platform that connects IoT-enabled waste stations, predictive analytics, intelligent collection routing, citizen engagement, and recycling operations through an event-driven architecture.

## Status

- .NET 10 · ASP.NET Core · EF Core · SQL Server
- Clean Architecture + CQRS + Domain-Driven Design
- SignalR real-time updates
- JWT auth with refresh tokens + RBAC
- 161 passing tests (unit · architecture · integration)

## Documentation

- [Product Requirements](docs/PRD.md)
- [Architecture Decision Records](docs/adr/)
- [Architecture Overview](docs/architecture/overview.md)

## Repository Layout

| Folder | Purpose |
|--------|---------|
| `src/EcoNexus.Api` | HTTP layer — controllers, middleware |
| `src/EcoNexus.Application` | CQRS, MediatR, validation, orchestration |
| `src/EcoNexus.Contracts` | DTOs shared with clients |
| `src/EcoNexus.Domain` | Entities, value objects, domain events |
| `src/EcoNexus.Infrastructure` | EF Core, Identity, SignalR |
| `src/EcoNexus.Worker` | IoT simulator + background workers |
| `tests/` | Unit, architecture, integration tests |

---

*Work in progress. The full README — with architecture diagram, screenshots, and quickstart — lands in the next session.*
