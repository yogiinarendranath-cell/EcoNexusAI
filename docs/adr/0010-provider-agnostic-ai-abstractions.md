# ADR-0010: Provider-Agnostic AI Abstractions

**Status:** Accepted
**Date:** 2026-09-23
**Decision Makers:** CTO, Lead Engineer

---

## Context

The platform has three AI capabilities:
1. Waste image classification
2. Fill-level forecasting (deterministic, no LLM)
3. Operations assistant (natural-language Q&A over live data)

For (1) and (3), we need an LLM. Available options:
- OpenAI / Azure OpenAI (paid, requires secrets, requires internet)
- Ollama (local, free, requires a running daemon)
- Mock provider (deterministic, useful for tests and offline dev)

Locking the Application layer to any single provider is a liability. Tests
should not depend on a network call. Demos should work offline.

---

## Decision

Introduce **provider-agnostic abstractions** in the Application layer:

- `IWasteClassificationService`
- `IAssistantLlm`

Each has two implementations in the Infrastructure layer:
- `Mock*` — deterministic, offline, used in tests and local dev.
- `Ollama*` — HTTP client against `http://localhost:11434`.

The provider is chosen by `AI:Provider` in configuration (`Mock` or `Ollama`).

---

## Consequences

**Positive:**
- Tests run offline, fast, and deterministically.
- Development works without an LLM daemon.
- Swapping to Azure OpenAI is a new Infrastructure class + one DI registration.
- The Application layer is honest about what it needs: "a classifier that
  takes an image and returns a category", not "a call to OpenAI".

**Negative:**
- One more layer of indirection.
- Interface must be stable enough that a real provider fits without a rewrite.
- The Mock provider's behavior may diverge from a real LLM over time; we accept
  this because tests assert on structure and invariants, not on specific output.

---

## Alternatives Considered

- **Hard-code OpenAI** — fastest to build, worst to test, ties us to a vendor.
- **Only Ollama** — free but requires a running daemon for every test run.
- **Semantic Kernel** — heavy abstraction; we would still need a provider
  choice and a mock.