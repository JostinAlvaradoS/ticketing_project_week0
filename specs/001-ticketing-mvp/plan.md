
```markdown
# Implementation Plan: Ticketing Platform MVP (Purchase Flow)

**Branch**: `001-ticketing-mvp` | **Date**: 2026-02-22
**Spec**: `specs/001-ticketing-mvp/spec.md`

## Summary

Plan to build a minimal, safe, testable ticket purchase flow across microservices .NET 8 using strict Hexagonal Architecture and a single PostgreSQL instance with schemas per bounded context. Priorities: correctness of seat reservation (no double-sell), reliable TTL reservations (15 min), simulated payment, PDF+QR ticket issuance and email notification.

## Tech Stack (detailed)

- .NET: .NET 8 SDK (target framework: net8.0)
- ORM: EF Core 8 + Npgsql.EntityFrameworkCore.PostgreSQL
- Kafka: Confluent.Kafka (.NET client) for producers/consumers
- Redis: StackExchange.Redis for locks and reservation counters
- Messaging schemas: JSON for MVP (move to Avro later) — store schemas in `/contracts/kafka/`
- DI / CQRS: MediatR for application layer handlers
- Validation: FluentValidation
- Logging: Serilog (structured logs) + Seq optional in dev
- Tracing: OpenTelemetry (.NET) with Jaeger exporter in dev
- PDF/QR: QRCoder + PdfSharpCore (or QuestPDF) for PDF generation
- Tests: xUnit + FluentAssertions; Testcontainers-dotnet for integration tests
- Utilities: Polly for transient retries, Ardalis.Specification optional

NuGet packages (minimum):
- Microsoft.EntityFrameworkCore
- Npgsql.EntityFrameworkCore.PostgreSQL
- Confluent.Kafka
- StackExchange.Redis
- MediatR.Extensions.Microsoft.DependencyInjection
- FluentValidation.AspNetCore
- Serilog.AspNetCore
- OpenTelemetry.Exporter.Jaeger
- QRCoder
- PdfSharpCore or QuestPDF
- Testcontainers

## Project Structure (per microservice — Hexagonal)

General repo layout (single repo mono-repo suggested for MVP):

services/
  ├─ identity/                # bc_identity
  │   ├─ src/
  │   │   ├─ Domain/
  │   │   ├─ Application/
  │   │   ├─ Infrastructure/
  │   │   └─ Api/ (Minimal API)
  │   └─ migrations/
  ├─ catalog/                 # bc_catalog
  ├─ inventory/               # bc_inventory
  ├─ ordering/                # bc_ordering
  ├─ payment/                 # bc_payment
  ├─ fulfillment/             # bc_fulfillment
  └─ notification/            # bc_notification

Example hexagonal layout inside `inventory/src`:

inventory/src/
  ├─ Domain/                   # Entities, ValueObjects, DomainEvents, Domain Services (no infra)
  ├─ Application/              # UseCases (Commands/Queries), DTOs, Validators (FluentValidation), MediatR handlers
  ├─ Infrastructure/           # EF Core DbContext, Repositories (Adapters), Kafka producers/consumers, Redis adapter
  └─ Api/                      # Minimal API endpoints wiring Application ports to HTTP

Notes:
- Keep Domain project free of any infra references. Use interfaces (ports) in Application or Domain.
- Each microservice owns its own `DbContext` configured with `SearchPath` / `.HasDefaultSchema("bc_<service>")`.

## PostgreSQL Schemas & Migrations

- Single Postgres instance; schemas: `bc_identity`, `bc_catalog`, `bc_inventory`, `bc_ordering`, `bc_payment`, `bc_fulfillment`, `bc_notification`.
- Connection string in `docker-compose` points at single host. Each service uses same connection string but configures EF Core `SearchPath`.

EF Core guidance:
- `DbContext` per service (scoped to their schema). In `OnModelCreating` call `modelBuilder.HasDefaultSchema("bc_inventory")` or configure Npgsql search_path in connection string.
- Migrations: keep migrations per service under `migrations/<service>/` and apply them with `dotnet ef database update` during compose startup or CI migration step. Migrations MUST run restricted to the schema — use `HasDefaultSchema` and schema-qualified table names.
- Role separation: recommend creating database roles per schema in ops (document in README ops notes).

## Redis Usage

- Purpose: distributed locks and reservation cache/TTL coordination.
- Pattern: Acquire short Redis lock key `lock:seat:{seatId}` using SET NX with sensible expiry (e.g., 30s) while performing DB checks and creating Reservation row. Use RedLock pattern only if multiple Redis nodes present; for MVP single Redis + SET NX acceptable.
- Reservation TTL: store reservation row `expires_at = now + 15m`. Optionally store reservation id in Redis with TTL 15m for quick lookup: `reservation:{reservationId} -> seatId`.
- Expiry handling: background worker (Inventory) polls or uses Redis keyspace notifications (optional) to trigger expiration tasks that update DB and publish `reservation-expired`.

## Kafka Topics & Initial Schemas

- Topics (JSON for MVP):
  - `reservation-created` { reservationId, seatId, userId?, expiresAt }
  - `reservation-expired` { reservationId, seatId }
  - `payment-succeeded` { paymentId, orderId, amount, currency }
  - `payment-failed` { paymentId, orderId, reason }
  - `ticket-issued` { ticketId, orderId, ticketUrl }

- Store JSON schema files in `/contracts/kafka/` and validate in integration tests. Move to Avro with schema registry in later phases.

## Docker Compose Outline (dev)

- Services: `postgres`, `redis`, `zookeeper`, `kafka`, `identity`, `catalog`, `inventory`, `ordering`, `payment`, `fulfillment`, `notification`
- Example connection strings (env):
  - `POSTGRES_URL=Host=postgres;Port=5432;Database=speckit;Username=postgres;Password=postgres` (use volumes for data)
  - `REDIS_URL=redis:6379`
  - `KAFKA_BOOTSTRAP_SERVERS=kafka:9092`

Volumes:
- Persist Postgres data (`./data/postgres:/var/lib/postgresql/data`) for dev.

Compose tips:
- Start order: postgres -> redis -> zookeeper -> kafka -> identity -> catalog -> inventory -> ordering -> payment -> fulfillment -> notification.
- Services should retry to connect; use healthchecks and wait-for scripts for robust startup.

## Catalog Admin CRUD (TDD & Gherkin Phase)

- Purpose: Implement the administration side for event and seat management in the Catalog service.
- Focus: Use `specs/001-ticketing-mvp/catalog-admin.feature` as the source of truth for implementation.
- TDD Approach: Red-Green-Refactor focusing on domain entities, application handlers (MediatR), and API integration tests using Testcontainers.
- Phase scope: 
  - Admin authentication/authorization check.
  - Event creation, listing, updating, and deactivation.
  - Bulk seat generation for newly created events.

## Concurrency & Locking Strategy

1. Read-mostly operations (seat maps) use simple SELECTs; show `reserved`/`sold` status by joining inventory/projections.
2. Reserving a seat (critical path): combine optimistic locking and a short Redis lock.
   - Steps:
     a. Acquire Redis lock `lock:seat:{seatId}` (SET NX, expiry 30s).
     b. Check seat row version in Postgres (rowversion/timestamp column). Use `SELECT ... FOR UPDATE` is acceptable but prefer optimistic `WHERE id = @id AND version = @v` update pattern.
     c. Insert Reservation row and update seat status to `reserved`, increment rowversion.
     d. Release Redis lock.
3. Payment path: before charging, Payment Service revalidates reservation (exists && not expired) and attempts to atomically mark seat `sold` within its `DbContext` transaction.

Rationale: Redis lock prevents concurrent reservation handlers across instances; optimistic row version prevents lost updates at DB level.

## Domain Events & Choreography

- Choreography model: services publish events on state changes; other services react.

Flow examples:
- Inventory publishes `reservation-created` → Ordering consumes to allow adding to cart (or validate on add).
- Payment publishes `payment-succeeded` → Ordering marks order paid; Inventory marks seat sold (if not yet) and publishes `ticket-issued` after Fulfillment completes.
- Reservation expiry handler publishes `reservation-expired` → Ordering/Inventory react to remove drafts or free seats.

Implementation notes:
- Produce events as part of the same logical operation when possible (after DB commit). Use Outbox pattern if you need stronger guarantees — for MVP rely on simple publish after commit with idempotent consumers; document as a TODO to add Outbox in Phase 2 if needed.

## Observability

- Logging: Serilog structured logs; include correlation id and service name in all logs. Configure sinks to console and Seq (dev).
- Tracing: OpenTelemetry instrumentation for incoming HTTP, outgoing HTTP, DB calls, and Kafka produce/consume. Export to Jaeger in dev.
- Metrics: expose Prometheus metrics endpoint (via OpenTelemetry metrics) for p95 latency and error rates.
- Correlation IDs: propagate `X-Correlation-ID` across HTTP and include in Kafka messages.

## Security Baseline

- Authentication: JWT tokens issued by Identity Service. All services validate JWT and extract `sub` claim for user id.
- Authorization: Order/payment endpoints must validate resource ownership (order.userId == token.sub) or guest token flow.
- Secrets: use `.env` or Docker secrets for dev; user secrets for local secrets in .NET.
- Rate limiting: lightweight rate limiter middleware per API (IP/user) to protect reservation endpoints from abuse.

## Prioritization & Initial Tasks (Phase breakdown)

Phase 0 — Foundation (deliverables: docker-compose, identity skeleton, shared Postgres + schemas)
- T0.1: Create `docker-compose.yml` with `postgres`, `redis`, `zookeeper`, `kafka` and healthchecks. (files: `infra/docker-compose.yml`)
- T0.2: Create DB initialization script to create schemas `bc_*` and service roles (ops note). (`infra/db/init-schemas.sql`)
- T0.3: Identity Service skeleton (Minimal API) with JWT signing for local dev.
- T0.4: CI job to run migrations and Testcontainers smoke tests.

Phase 1 — Core services (deliverables: Catalog read APIs, Inventory reservation + TTL, Ordering cart persistence)
- T1.1: Catalog service: read-only APIs for events/seat map (`GET /events/{id}/seatmap`). Migrations for `bc_catalog`.
- T1.2: Inventory service: implement Reservation creation with Redis lock + DB write; background job to expire reservations and publish `reservation-expired`. Migrations for `bc_inventory`.
- T1.3: Ordering service: implement cart add/remove, create order draft and pending states. Migrations for `bc_ordering`.
- T1.4: Integration tests for reservation→order flows including concurrent reservation test.

Phase 2 — Payment, Fulfillment, Notification
- T2.1: Payment (simulated): validate order and reservation, emit `payment-succeeded`/`payment-failed`.
- T2.2: Fulfillment: consume `payment-succeeded`, create Ticket, generate PDF+QR, store artifact and publish `ticket-issued`.
- T2.3: Notification service: consume `ticket-issued` and send email (dev SMTP adapter).

Phase 3 — Polish & Hardening
- T3.1: Add observability end-to-end, tracing and metrics dashboards.
- T3.2: Add contract tests & consumer-driven tests for key topics.
- T3.3: Add Outbox pattern and idempotency keys if event loss observed.

## Risks & Mitigations

- Schema collisions (multiple services altering shared DB):
  - Mitigation: enforce migrations per schema, CI check that migrations target `bc_<service>`; require at least 2 reviewers for migrations.
- Double-sell under high concurrency:
  - Mitigation: Redis locks + optimistic rowversion checks; integration tests to simulate contention; increase lock granularity if needed.
- Event loss between DB commit and Kafka publish:
  - Mitigation: mark as acceptable risk for MVP; plan Outbox in Phase 3 for stronger guarantees.
- PDF generation failures after payment:
  - Mitigation: implement retry queue for Fulfillment; mark order as `pending-fulfillment` and notify ops on repeated failure.

## Deliverables & Artifacts

- `specs/001-ticketing-mvp/plan.md` (this file)
- `specs/001-ticketing-mvp/tasks.md` (generated by `/speckit.tasks`)
- `infra/docker-compose.yml` and `infra/db/init-schemas.sql`
- `contracts/openapi/*.yaml` and `/contracts/kafka/*.json`

## Commands / Quickstart (dev)

1. Start infra:

```bash
docker compose -f infra/docker-compose.yml up -d
```

2. Apply migrations per service (example):

```bash
cd services/inventory
dotnet ef database update --project src/Infrastructure --context InventoryDbContext
```

3. Run integration tests (from repo root):

```bash
dotnet test tests/Integration --logger:trx
```

## Plan Completion Criteria

- All foundational infra running in `docker-compose` and migrations applied.
- Inventory reservation end-to-end validated by integration tests including concurrent reservation stress test.
- Full purchase flow completes in smoke test (select → reserve → add to cart → simulate payment → ticket issued → email queued).

---

**Notes:** This plan strictly follows constitution v1.1.0: Hexagonal boundaries, single Postgres with schemas, Kafka as event bus (choreography), Redis for locks, Docker Compose for local dev, and prioritizes local transactions + simplicity for MVP.

``` 