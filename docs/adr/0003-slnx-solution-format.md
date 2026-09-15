# ADR-0003: Use .slnx Solution Format

- Status: Accepted
- Date: 2026-09-15

## Context
.NET 9+ defaults to the XML-based .slnx format. Classic .sln is still supported.

## Decision
Use `EcoNexus.slnx`. All solution-level commands reference it explicitly.

## Consequences
**Positive:** modern, readable XML, merges cleanly in git.
**Negative:** some older tooling assumes .sln; VS 2022 17.10+ supports it.
