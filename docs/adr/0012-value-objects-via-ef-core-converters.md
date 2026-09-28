# ADR-0012: Value Objects via EF Core Value Converters

**Status:** Accepted
**Date:** 2026-09-27
**Decision Makers:** CTO, Lead Engineer

---

## Context

The Domain layer uses value objects (`FillLevel`, `Weight`, `StationCode`,
`Location`) to enforce invariants at construction time and to make the
ubiquitous language explicit.

EF Core can map value objects in three ways:
1. As owned entities (`.OwnsOne(...)`)
2. As value-converted scalars (`.HasConversion(...)`)
3. As complex types (EF Core 8+ `.ComplexProperty(...)`)

Each approach has different trade-offs for query translation and schema.

---

## Decision

Use **value converters** for single-scalar value objects, and **owned entities**
for multi-property ones.

- `Weight` → `double` column, converter from/to `Weight.Kilograms`.
- `FillLevel` → `double` column, converter from/to `FillLevel.Percent`.
- `StationCode` → `string` column, converter from/to `StationCode.Value`.
- `Location` → owned entity with `Latitude` / `Longitude` columns.

For value objects that must be **comparable** in LINQ (e.g. `FillLevel` in
`OrderBy` or `Where`), the type **must implement `IComparable<T>` and the full
set of comparison operators** (`<`, `>`, `<=`, `>=`, `==`, `!=`) plus
`Equals` / `GetHashCode`. This is required because EF Core needs to translate
comparisons against the *converted* scalar, and the compiler needs the
operators to type-check.

**Related bug fixed:** `WasteStationRepository.ListAsync` originally sorted by
`s.CurrentFill.Percent`. EF Core could not translate this through the value
converter. The fix was:
1. Make `FillLevel` implement `IComparable<FillLevel>` + operators.
2. Sort by `s.CurrentFill` (the whole value object), letting EF Core use the
   converter.

---

## Consequences

**Positive:**
- Clean domain — no leaking of EF Core concepts into Domain.
- Simple, flat schema — one column per scalar value object.
- Query translation works for comparisons and ordering once the value object
  implements `IComparable`.
- No shadow tables.

**Negative:**
- Value objects must repeat some boilerplate (`IComparable`, operators,
  `Equals`, `GetHashCode`) — code generation would help.
- Some LINQ expressions fail to translate until the type is properly
  comparable. Debuggable but non-obvious.
- Multi-property value objects must be owned entities, mixing two mapping
  styles.

---

## Alternatives Considered

- **Complex types** (EF Core 8+) — cleaner but newer; some LINQ translation
  edges still rough.
- **Owned entities for everything** — verbose for single-scalar VOs; forces
  `OwnsOne` config everywhere.
- **String columns with JSON** — opaque; kills query support.