<!-- Sync Impact Report
Version change: 1.1.0 → 1.2.0 (MINOR: documented exceptions in Principles V and VI — bootstrap permissions and shared catalog tables)
Ratification: 2025-01-01
Last amended: 2026-04-01
Templates updated: ✅ constitution.md
Follow-up TODOs:
  - Move geographic seed data (provincias/cantones) out of migrations into admin UI (violates Principle V)
  - Add CREATE POLICY statements to core tables that have ENABLE ROW LEVEL SECURITY (violates Principle VI)
  - Fix Unwrap() calls in production application/api layers (violates Principle II)
  - Expose JWT middleware via shared/port to eliminate cross-module imports (violates Principle I)
-->

# GPI Project Constitution

**Version**: 1.2.0 | **Ratified**: 2025-01-01 | **Last Amended**: 2026-04-01

**Status**: Active

---

## Project Overview

GPI is a multi-tenant SaaS ERP for Ecuadorian public sector institutions.
Each institution is a tenant with its own subdomain (`municipiodeloja.gpi.ec`)
and its own Keycloak realm.

**Module path**: `github.com/tuusuario/gpi`
**Language**: Go 1.22
**Database**: PostgreSQL 16

---

## Core Principles

### I. Hexagonal Architecture with DDD

Every business module MUST follow exactly 4 layers with no exceptions:

```
modules/{name}/
├── domain/         # pure business logic — zero external imports
├── application/    # CQRS orchestration only
├── infrastructure/ # concrete implementations (postgres, keycloak)
└── api/            # HTTP handlers
```

**Dependency rule** (MUST never be inverted):
```
api → application → domain ← infrastructure
shared ← all layers
```

Modules MUST NOT import each other directly.
Cross-module communication MUST use interfaces in `domain/port/` or
`shared/port/`.

**Rationale**: Enforces separation of concerns, enables independent testing
of business logic, and prevents accidental coupling between modules.

---

### II. Explicit Error Handling with Result[T]

Every function that can fail MUST return `result.Result[T]`. The `(T, error)`
pattern is PROHIBITED in domain and application layers.

```go
// MUST — correct
func NewPresupuestoReferencial(monto decimal.Decimal) result.Result[PresupuestoReferencial] {
    if monto.LessThanOrEqual(decimal.Zero) {
        return result.Err[PresupuestoReferencial](
            errors.New("INVALID_MONTO", "debe ser mayor a cero"))
    }
    return result.Ok(PresupuestoReferencial{monto: monto})
}

// PROHIBITED — never do this in domain or application
func Calculate(input Input) (Result, error) { ... }
```

`panic` is PROHIBITED in production code. Use `Result.Err()` always.
`Unwrap()` (which panics) is allowed ONLY in test files.

**Rationale**: Makes failure paths explicit, prevents silent error swallowing,
and produces consistent error structures for the API layer.

---

### III. Immutable Value Objects with Guard Clauses

Value Objects MUST have:
- All fields private (unexported)
- Only getter methods (no setters)
- Guard Clauses as the first statements in constructors
- Return `Result[T]` from constructors, never plain structs

```go
type PresupuestoReferencial struct {
    monto decimal.Decimal // private — never exported
}

func NewPresupuestoReferencial(monto decimal.Decimal) result.Result[PresupuestoReferencial] {
    if monto.LessThanOrEqual(decimal.Zero) {
        return result.Err[PresupuestoReferencial](
            errors.New("INVALID_MONTO", "debe ser > 0"))
    }
    return result.Ok(PresupuestoReferencial{monto: monto})
}

func (p PresupuestoReferencial) Monto() decimal.Decimal { return p.monto }
```

**Rationale**: Guarantees domain invariants are enforced at construction time,
making invalid states unrepresentable in the type system.

---

### IV. Decimal Precision for Money and Percentages

`float64` is PROHIBITED for any monetary value or percentage.
All money and percentage values MUST use `github.com/shopspring/decimal`.

**Rationale**: Floating-point arithmetic produces rounding errors that are
illegal in financial calculations governed by Ecuadorian public sector law.

---

### V. SQL as Versioned Artifacts

SQL inline in Go is PROHIBITED. All database queries MUST be defined in
`.sql` files and generated via `sqlc`. Query files live in `db/sqlc/`.

Migrations MUST contain DDL only (CREATE TABLE, CREATE INDEX, ALTER TABLE).
INSERT/UPDATE/DELETE in migration files is PROHIBITED.
Reference data is loaded by the admin through the application UI.

**Rationale**: Queries are contracts with the database. Versioning them as
files enables review, audit, and regeneration of type-safe Go code.

**Exception — Bootstrap Permissions**: Migration files that seed
`core.permissions` and `core.role_permissions` with the base permission set
ARE allowed to contain INSERT/DELETE statements. This exception exists because
the RBAC system requires a known set of permissions to exist before any admin
can log in and operate the platform. Without this seed data, the system cannot
be administered after initial deployment. These migrations MUST be idempotent
(`INSERT ... ON CONFLICT DO NOTHING` / `DELETE ... WHERE name = '...'`).

All other reference data (including geographic data such as `provincias` and
`cantones`) MUST be loaded through the admin UI, not via migrations.

---

### VI. Multi-Tenant Data Isolation

Every table containing tenant business data MUST have:
- `tenant_id UUID NOT NULL` column
- Row Level Security (RLS) enabled
- An RLS policy filtering by `current_setting('app.current_tenant_id')::uuid`

Every query on tenant data MUST filter by `tenant_id`.
Data between tenants MUST never be mixed.

```sql
-- REQUIRED for every business table
ALTER TABLE presupuesto.presupuestos ENABLE ROW LEVEL SECURITY;
CREATE POLICY presupuestos_tenant_isolation
    ON presupuesto.presupuestos
    USING (tenant_id = current_setting('app.current_tenant_id')::uuid);
```

**Rationale**: Architectural enforcement of data isolation prevents accidental
data leakage between institutions, even in the presence of application bugs.

**Exception — Shared Normative Catalogs**: Tables in the `catalogs` schema
(`sectoriales`, `sectoriales_versiones`, `fuentes`, `fuentes_versiones`,
`geografico_versiones`, `provincias`, `cantones`, `clasificador_versiones`,
`clasificador_naturalezas`, `clasificador_grupos`, `clasificador_subgrupos`,
`clasificador_items`, `cootad_reglas_inversion`) are global normative data
published by the Ecuadorian Ministry of Finance and shared identically across
all tenants. These tables MUST NOT have `tenant_id` and MUST NOT enable RLS.
They are read-only reference data managed by `gpi_admin`, not by individual
institutions. Any future catalog table that IS tenant-specific MUST still
follow the full tenant isolation rule.

---

### VII. Versioned Reference Data

Normative data (LOSNCP ranges, catalog items, geographic codes) MUST live
in the database and be versioned. Hardcoding normative values is PROHIBITED.

Each catalog has a `*_versiones` table with `vigente_hasta IS NULL` marking
the current version. A UNIQUE partial index enforces exactly one active
version.

When a business process is created, it MUST record the UUID of each catalog
version active at that moment. This reference is permanent and immutable.

**Rationale**: Ecuadorian public institutions update normative yearly via
ministerial agreements. Historical records must reference the exact regulation
that was in force when the process was created, not the current one.

---

### VIII. Mandatory Pattern Catalog

Every component MUST use the assigned pattern. No exceptions without
explicit architecture review.

| Layer | Component | Pattern | Location |
|-------|-----------|---------|----------|
| Domain | Business rules | Value Object | `domain/vo/` |
| Domain | Entity with identity | Aggregate Root | `domain/entity/` |
| Domain | Post-commit notifications | Domain Events | `domain/event/` |
| Domain | Combinable rules | Specification | `domain/spec/` |
| Application | State changes | Command Handler | `application/command/` |
| Application | Read operations | Query Handler | `application/query/` |
| Application | External services | Anti-Corruption Layer | `application/port/` + `infrastructure/` |
| Application | Multi-repo transactions | Unit of Work | `shared/port/UnitOfWork` |
| Infrastructure | Cross-cutting concerns | Decorator | `infrastructure/decorator/` |

Decorator MUST wrap the interface, never the concrete implementation.

---

### IX. Quality Standards

- Every domain function MUST have unit tests
- Every infrastructure implementation MUST have integration tests using
  testcontainers
- Specifications MUST have tests for `And`, `Or`, `Not` combinations
- All logging MUST be structured: `slog.With("tenant_id", ..., "op", ...)`
- `go build ./...`, `go vet ./...` MUST pass before any commit

---

### X. Test File Location

Test files MUST NOT live inside module layers (`modules/*/domain/`,
`modules/*/application/`, `modules/*/infrastructure/`, `modules/*/api/`).
All tests MUST be placed under the top-level `tests/` directory:

```
tests/
├── unit/{module}/          # domain unit tests (package {module}_test)
└── integration/{module}/   # infrastructure integration tests (package {module}_test)
```

**Package naming**: test files MUST use an external test package named
`{module}_test` (e.g. `package presupuesto_test`, `package catalogs_test`).
This enforces black-box testing and prevents accidental access to unexported
symbols from the production package.

**Build tags**: integration tests MUST carry `//go:build integration` as the
first line so they are excluded from `go test ./...` runs without the tag.

**PROHIBITED**: `*_test.go` files anywhere inside `modules/`.
Any test file found inside a module layer is a constitution violation and
MUST be moved to `tests/` before the PR can be merged.

**Rationale**: Keeping tests in a dedicated top-level directory makes the
test surface immediately visible, prevents IDE confusion between production
and test code, and enforces the black-box boundary between callers and the
domain layer.

---

## Tech Stack Reference

| Concern | Library | Notes |
|---------|---------|-------|
| HTTP router | `github.com/go-chi/chi/v5` | |
| Database driver | `github.com/jackc/pgx/v5` | |
| Query generation | `sqlc` | All queries in `.sql` files |
| Migrations | `goose` | DDL only |
| Decimal math | `github.com/shopspring/decimal` | All money/percentages |
| JWT validation | `github.com/lestrrat-go/jwx/v2` | JWKS cache, 1h TTL |
| Dependency injection | `github.com/google/wire` | |
| Config | `github.com/spf13/viper` | |
| Testing | `github.com/stretchr/testify` + `testcontainers-go` | |
| Observability | `go.opentelemetry.io/otel` | |

---

## DB Schemas

| Schema | Purpose |
|--------|---------|
| `core` | Tenants, users, modules, RBAC |
| `catalogs` | 4 Ministry of Finance catalogs, versioned per table |
| `presupuesto` | Annual budget management — COOTAD Ecuador |

---

## Roles

| Role | Realm | Has tenant |
|------|-------|-----------|
| `gpi_superadmin` | `gpi` | No |
| `gpi_admin` | `gpi` | No — manages catalogs and normative |
| `gpi_user` | `{slug}` | Yes — belongs to one institution |

---

## Governance

**Amendment procedure**: Any change to this constitution requires:
1. Explicit rationale documenting why the principle needs to change
2. Impact assessment on existing modules
3. Version bump following semantic versioning
4. Update of `LAST_AMENDED_DATE`

**Versioning policy**:
- MAJOR: Removal or redefinition of a principle (backward incompatible)
- MINOR: New principle or material expansion
- PATCH: Clarifications, wording, non-semantic refinements

**Compliance review**: Every `/speckit.plan` execution MUST include a
Constitution Check section validating all 9 principles against the feature
design. Any violation blocks the plan unless explicitly justified and
approved.

**Violation protocol**: If implementation conflicts with this constitution,
execution MUST stop. Report the exact principle violated, describe the
conflict, propose a compliant alternative, and wait for explicit approval
before continuing.
