# EcoNexus AI — Architecture Overview

## Layered Architecture

```mermaid
flowchart TD
    Api[EcoNexus.Api] --> App[EcoNexus.Application]
    Worker[EcoNexus.Worker] --> App
    App --> Domain[EcoNexus.Domain]
    App --> Contracts[EcoNexus.Contracts]
    Infra[EcoNexus.Infrastructure] --> App
    Infra --> Domain
    Api --> Infra
    Worker --> Infra
```

## Dependency Rules

- `Domain` and `Contracts` depend on nothing.
- `Application` depends on `Domain` + `Contracts`.
- `Infrastructure` depends on `Application` + `Domain`.
- `Api` and `Worker` depend on `Application` + `Infrastructure` (+ `Contracts` for `Api`).

## Decisions

See `docs/adr/` for the list of architectural decisions.

## Target Production Architecture

```mermaid
flowchart TB
    Internet([Internet]) --> FE[React Frontend]
    FE --> API[ASP.NET Core API]
    API --> DB[(SQL Server)]
    API --> SB[Azure Service Bus]
    API --> AI[Azure OpenAI]
    API --> HUB[SignalR]
    SB --> W1[IoT Simulator Worker]
    SB --> W2[Prediction Worker]
    SB --> W3[Alert Worker]
```