# ADR-0013: Central Package Management

**Status:** Accepted
**Date:** 2026-09-15
**Decision Makers:** CTO, Lead Engineer

---

## Context

With 9 projects in the solution and 39 NuGet packages, drift in package
versions is a real risk. Two projects on different `Microsoft.EntityFrameworkCore`
patch versions can cause hard-to-debug runtime failures.

.NET's default model puts `<PackageReference Include="X" Version="1.2.3" />` in
every `.csproj`, which is error-prone.

---

## Decision

Adopt **Central Package Management (CPM)** via `Directory.Packages.props`.

- A single `<PackageVersion Include="..." Version="..." />` per package.
- `.csproj` files use `<PackageReference Include="X" />` — no version.
- `<ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>`
  in `Directory.Packages.props`.

We also have a `Directory.Build.props` that centralises:
- `<TargetFramework>net10.0</TargetFramework>`
- `<Nullable>enable</Nullable>`
- `<ImplicitUsings>enable</ImplicitUsings>`
- `<TreatWarningsAsErrors>true</TreatWarningsAsErrors>`
- `<NuGetAudit>true</NuGetAudit>` + `<NuGetAuditLevel>high</NuGetAuditLevel>`
- `InternalsVisibleTo` for the three test projects

---

## Consequences

**Positive:**
- One file to bump versions.
- No drift between projects.
- `NuGetAudit` fails the build on high/critical CVEs.
- `TreatWarningsAsErrors` prevents warning accumulation — builds stay clean.

**Negative:**
- New contributors may be confused by `.csproj` files without versions.
- Some tooling (older IDE versions, CLI helpers) does not understand CPM.
- Transitive-only packages need explicit `<PackageVersion>` entries.

---

## Alternatives Considered

- **Per-project versions** — default, drift-prone.
- **Paket** — powerful but niche; overkill.
- **Custom MSBuild props for versions** — effectively CPM with less tooling.