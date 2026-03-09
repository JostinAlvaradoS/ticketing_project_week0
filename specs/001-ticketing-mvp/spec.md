```markdown
# Feature Specification: Ticketing Platform MVP (Purchase Flow)

**Feature Branch**: `001-ticketing-mvp`  
**Created**: 2026-02-22  
**Status**: Draft  
**Input**: User description: "MVP para plataforma de venta de boletos con microservicios .NET, arquitectura hexagonal y una sola instancia PostgreSQL compartida (schemas por bounded context). Priorizar flujo de compra de boletos." 

## Overview

Objetivo: Entregar un MVP que permita a usuarios comprar boletos para eventos con la experiencia mínima viable: seleccionar asientos, reservar temporalmente, pagar (simulado), emitir boleto con QR y enviar notificación por email.

Arquitectura: Microservicios .NET 8 por bounded context con Hexagonal Architecture (Domain, Application, Infrastructure, API). Una única instancia PostgreSQL compartida; cada servicio usa su propio schema `bc_<name>`. Comunicación síncrona por HTTP/REST (Minimal APIs) y asíncrona por Kafka (Confluent). Redis para reservas temporales/locks.

MVP Scope: Flujo principal de compra (P1). Funcionalidades administrativas y pagos con proveedor real son opcionales (P2/P3).

## Clarifications

### Session 2026-02-22

- Q1: Should the MVP include a Stripe sandbox integration path? → A: No — Payment simulated only for MVP (Stripe sandbox deferred to backlog).


## User Stories & Priorities

### P1 — Compra de boleto (end-to-end)
- As a Customer I want to select a specific seat, reserve it temporarily, add it to my cart, and complete payment so that I receive a valid ticket with QR and email confirmation.

### P2 — Navegación y descubrimiento (browse)
- As a Visitor I want to browse events, venues and seat maps so that I can find events and pick seats to reserve.

### P3 — Gestión básica por organizador
- As an Organizer I want to create events and configure venue seating so that tickets can be sold for my events.

## Acceptance Scenarios

### P1 — Compra de boleto (Critical)
1. Given an event with available seats, When a customer selects a seat and requests a reservation, Then the system marks the seat as `reserved` (TTL 15 minutes) and returns a reservation id.
2. Given a valid reservation, When the customer adds reserved seat to cart and submits payment, Then payment is processed (simulated success) and an order is created with state `paid` and seat state becomes `sold`.
3. Given payment succeeded, When fulfillment runs, Then a ticket PDF with QR is generated, stored (or made retrievable), and `ticket-issued` event is published; email notification is queued.
4. Given a reservation TTL expires before payment, When TTL elapses, Then reservation state becomes `expired`, seat returns to `available`, and `reservation-expired` event is published.

### P2 — Browse events
1. Given events exist, When a visitor queries events or a venue seat map, Then the API returns event metadata and seat availability in real-time (reflecting reservations/sold).

### P3 — Organizer create event
1. Given authenticated organizer, When they submit event + venue + seating config, Then event and seat rows are created in Catalog schema and available for sale.

## Edge Cases

- Concurrent reservations: multiple customers attempt to reserve the same seat simultaneously. (Handled via optimistic locking + Redis locks)
- Payment race: payment processed while reservation expired — payment should fail or result in compensating flow that re-checks seat availability.
- Partial failures: ticket generation fails after payment — implement retry and manual compensation (order->failed/notify ops team).
- DB migration collisions across services: each service must write migrations scoped to its schema only.

## Functional Requirements

- **FR-001**: System MUST allow browsing events and retrieving seat maps with availability per seat.
- **FR-002**: System MUST reserve a seat for a configurable TTL (default 15 minutes) and return a reservation identifier.
- **FR-003**: System MUST allow adding reserved seats to a cart and creating an Order in `draft` then `pending` and `paid` states.
- **FR-004**: System MUST process payments via a simulated Payment Service for MVP and update Order status accordingly.
- **FR-005**: System MUST generate a ticket (PDF) containing a QR code after successful payment and mark the order as `fulfilled`.
- **FR-006**: System MUST publish domain events to Kafka: `reservation-created`, `reservation-expired`, `payment-succeeded`, `payment-failed`, `ticket-issued`.
- **FR-007**: Seat lifecycle MUST follow states: `available`, `reserved`, `sold` and be persisted in `bc_catalog` (seat master) and `bc_inventory` (availability provenance).
- **FR-008**: System MUST implement optimistic locking in Postgres for seat rows and use Redis locks for cross-process reservation coordination.
- **FR-009**: Each microservice MUST own its schema `bc_<name>` and apply migrations only to it.
- **FR-010**: All external integrations MUST be accessed via ports/adapters; domain layer MUST have zero infra dependencies.
- **FR-011**: Fulfillment MUST consume `order-paid` events to trigger ticket generation and QR code creation.
- **FR-012**: Notification MUST consume `ticket-issued` events to send an email confirmation to the customer.

## Key Entities

- `Event` (Catalog - `bc_catalog`)
  - id, name, description, organizer_id, start_time, end_time, venue_id, status

- `Venue` (Catalog)
  - id, name, address, seating_layout_reference

- `Seat` (Catalog / Inventory)
  - id, venue_section, row, number, seat_code, base_price, status (available/reserved/sold), version (rowversion)

- `Reservation` (Inventory - `bc_inventory`)
  - id, seat_id, user_id (optional), expires_at, status (active/expired/cancelled), created_at

- `Cart/Order` (Ordering - `bc_ordering`)
  - id, user_id (or guest_token), items [{seat_id, price}], total_amount, state (draft/pending/paid/fulfilled/cancelled), created_at, paid_at

- `Payment` (Payment - `bc_payment`)
  - id, order_id, amount, currency, status (succeeded/failed), provider_reference

- `Ticket` (Fulfillment - `bc_fulfillment`)
  - id, order_id, ticket_pdf_path (or blob ref), qr_code_payload, issued_at

- `NotificationLog` (Notification - `bc_notification`)
  - id, ticket_id, user_email, status (sent/failed), sent_at, notification_type (email)

## Success Criteria (measurable)

- **SC-001**: 95% of complete purchase flows (select → payment → ticket) succeed end-to-end in < 30 seconds (including PDF generation) in normal conditions.
- **SC-002**: Reservation correctness: under concurrent contention tests, 99% of attempted double-reservations are prevented (measured by integration tests using Testcontainers + Redis).
- **SC-003**: Reservation TTL enforcement: expired reservations free seats within 1 minute of TTL expiration (background job reliability).
- **SC-004**: System must process simulated payment and transition order to `paid` in < 5 seconds in normal conditions.
- **SC-005**: Emit and persist required Kafka events for at least 99% of critical transitions in integration tests.

## Non-Functional Requirements (NFR)

- **NFR-001 (Availability)**: Core purchase path services (Catalog, Inventory, Ordering, Payment, Fulfillment) combined availability target 99% in staging.
- **NFR-002 (Concurrency)**: System must safely handle bursty contention for seats; use optimistic locking + Redis locks to avoid double-sell.
- **NFR-003 (Security)**: JWT-based authentication via `Identity Service`. Payment endpoints must validate order ownership.
- **NFR-004 (Observability)**: Services MUST emit structured logs (Serilog) and traces (OpenTelemetry). Key events: reservation-created, payment-succeeded.
- **NFR-005 (Performance)**: Typical read queries (events list/seat map) should respond < 200ms p95 in local dev with realistic dataset.
- **NFR-006 (Data Integrity)**: All critical state transitions MUST be persisted in Postgres within the owning schema; compensating actions published as events when necessary.

## Key Flows (P1 main flow — step by step)

1. User queries `Catalog Service` for event details and seat map (GET /events/{id}/seatmap).
2. User selects seat S and requests reservation: POST `Inventory Service` /reservations {seatId, userId/guestToken}.
   - Inventory checks seat status (optimistic version) and attempts a Redis-based lock for seatId.
   - If free, create `Reservation` row in `bc_inventory`, set `expires_at = now + 15m`, set seat status `reserved`, publish `reservation-created` to Kafka.
3. User adds reserved seat to cart: POST `Ordering Service` /cart/add {reservationId, seatId} → creates/updates Order in `draft`/`pending`.
4. User submits payment: POST `Payment Service` /payments {orderId, paymentMethod}.
   - Payment Service (simulated) validates order state, re-checks reservation/seat status, charges (simulated) and publishes `payment-succeeded` or `payment-failed`.
5. On `payment-succeeded` event, Ordering transitions order → `paid`; Inventory marks seat → `sold` (persist), Fulfillment creates a Ticket entity and generates PDF + QR, saves artifact (storage or blob) and publishes `ticket-issued`.
6. Notification Service consumes `ticket-issued` and enqueues/sends transactional email with ticket link or attachment.

## Assumptions & Clarifications Needed

- Assumption: Payment is simulated by default for MVP; Stripe integration is OPTIONAL.  
- Assumption: Payment is simulated by default for MVP; Stripe integration is OPTIONAL.  
  - Answered Q1: No — Payment simulated only for MVP (Stripe sandbox deferred to backlog).

- Assumption: Ticket PDFs may be stored on local filesystem in dev or object storage in prod.  
  - [NEEDS CLARIFICATION] Q2: Persist generated PDFs in object storage (S3-compatible) for production, or generate on-demand and store minimal metadata?  
    - Default: Store generated PDF in configured storage (local in dev, S3 in prod) and reference via `ticket_pdf_path`.

- Assumption: Email provider will be configured via environment secrets; for MVP use a local SMTP relay or dev-only adapter.

## Contracts & Artifacts

- OpenAPI specs per service: `/contracts/openapi/catalog.yaml`, `/contracts/openapi/inventory.yaml`, `/contracts/openapi/ordering.yaml`, `/contracts/openapi/payment.yaml`, `/contracts/openapi/fulfillment.yaml`, `/contracts/openapi/identity.yaml`.
- Kafka Avro/JSON schemas: `/contracts/kafka/reservation-created.avsc`, `reservation-expired.avsc`, `payment-succeeded.avsc`, `payment-failed.avsc`, `ticket-issued.avsc`.
- Migrations: each service maintains migrations under `/migrations/<service>/` that target schema `bc_<service>`.
- Event flows and sequence diagrams stored under `/docs/diagrams/checkout-flow.md`.

## Acceptance Test Plan (high level)

- Unit tests: Domain-level tests for each service using pure domain models and mocks for ports.
- Integration tests: Use Testcontainers (single Postgres container with multiple schemas, Redis, Kafka) to validate cross-service flows: reservation→order→payment→ticket. Tests must simulate concurrent reservation attempts to validate locking.
- Contract tests: OpenAPI contract tests for each API (consumer-driven where applicable).
- End-to-end smoke: Deploy docker-compose with 1 Postgres, Redis, Kafka, Zookeeper and run a smoke test that performs the full purchase flow.

## Next Steps (for planning)

1. Create spec directory and commit this `spec.md` to `specs/001-ticketing-mvp/spec.md` (done).  
2. Generate minimal OpenAPI contracts for Catalog, Inventory, Ordering and Payment (Phase 0).  
3. Implement Phase 1 foundation tasks: single Postgres-compose, Redis, Kafka; Identity Service skeleton; skeleton microservices with Hexagonal structure and EF Core `DbContext` per service (migrations scoped to `bc_*`).  
4. Implement Inventory reservation logic and Ordering cart persistence; add integration tests for concurrent reservation.  
5. Implement simulated Payment Service, Fulfillment PDF/QR generation, and Notification consumer.  

----

**Spec ready for** `/speckit.plan` to produce implementation plan, tasks and estimates.

``` 