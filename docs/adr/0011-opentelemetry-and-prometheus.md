# ADR-0011: OpenTelemetry + Prometheus for Observability

**Status:** Accepted
**Date:** 2026-09-26
**Decision Makers:** CTO, Lead Engineer

---

## Context

Once the platform is deployed, we need to answer questions like:
- Why was this request slow?
- Which station raised the overflow event?
- How many classification requests are we handling per minute?

Options:
- Logging only (Serilog to console / file)
- Application Performance Monitoring (APM) SaaS (Datadog, New Relic)
- OpenTelemetry (vendor-neutral) + Prometheus + Grafana
- Azure Monitor / Application Insights

---

## Decision

Adopt **OpenTelemetry** for traces and metrics, **Prometheus** for metric
scraping, and keep **Serilog** for structured logs.

- Traces: ASP.NET Core inbound, HttpClient outbound, EF Core queries.
- Metrics: runtime (GC, thread pool), HTTP server, custom business meters.
- Custom business meters are registered in `EcoNexusMeters` and emitted from
  domain event handlers (e.g. `econexus_station_reading_recorded`).
- Every Serilog log line carries the current trace ID (`Enrich.WithSpan()`).
- The Prometheus scrape endpoint is exposed at `/metrics` and must be
  firewalled in production (internal ingress only).

---

## Consequences

**Positive:**
- Vendor-neutral: no lock-in. We can ship traces to Tempo, Jaeger, or
  Azure Monitor by changing the exporter only.
- Business metrics sit next to framework metrics — same dashboard.
- Trace ID in every log line makes log↔trace correlation trivial.
- Free with the OSS stack.

**Negative:**
- Two more moving parts in the runtime (OTel SDK + Prometheus exporter).
- The Prometheus endpoint is a scrape target — needs firewall discipline.
- No built-in UI. Grafana or similar must be added later for dashboards.
- Instrumentation is not free — some CPU overhead.

---

## Alternatives Considered

- **Application Insights** — turnkey but Azure-bound; requires an Azure
  subscription.
- **Datadog** — excellent but expensive at any real scale.
- **Logging only** — cannot answer "why slow" or "how many per minute".
- **Custom metrics library** — reinventing OpenTelemetry.