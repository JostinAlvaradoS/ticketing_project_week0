# Implementation Plan: Reservation Expiry Consistency

**Branch**: `003-reservation-expiry-consistency` | **Date**: 2026-04-02 | **Spec**: [spec.md](./spec.md)
**Input**: Feature specification from `/specs/003-reservation-expiry-consistency/spec.md`

## Summary

When a seat reservation expires (TTL = 15 min), the system must consistently cancel related orders and payments, release the seat on failed payment, and notify the affected customer. Five gaps exist: (1) draft orders never cancelled, (2) pending payments never cancelled, (3) `payment-failed` has no consumers to release seat or revert order, (4) `customerId` missing from `reservation-expired` event, (5) Notification service not notified. All fixes are new Kafka consumers + enriched event payload. No new DB tables required.

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: MediatR 12.2.0, EF Core 8, Confluent.Kafka 2.5.0, FluentAssertions, xUnit, Moq
**Storage**: PostgreSQL (shared instance) — schemas `bc_ordering`, `bc_payment`, `bc_inventory`
**Testing**: xUnit + FluentAssertions + Moq (unit); Testcontainers (integration)
**Target Platform**: Linux (Docker Compose), macOS dev
**Project Type**: Microservices / event-driven web services
**Performance Goals**: Expiry-to-cancellation latency < 30 seconds (per SC-001..SC-003)
**Constraints**: Events must be idempotent; no saga orchestrator; no new DB tables
**Scale/Scope**: Single-seat orders primary case; per spec assumptions

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-checked after Phase 1 design.*

| Check | Status | Notes |
|-------|--------|-------|
| Architecture: Hexagonal | PASS | New consumers go in Infrastructure; ports stay in Application |
| Database: shared PostgreSQL + `bc_` schemas | PASS | Existing schemas used; no new schemas needed |
| DbContext/Migrations | PASS | No new tables; only new read/write paths via existing EF Contexts |
| Communication: Kafka topics | PASS | Consuming `reservation-expired`, `payment-failed`; enriching `reservation-expired` payload |
| Transactions: local ACID | PASS | Each consumer updates only its own schema — no distributed transactions |
| Local Dev Topology | PASS | Kafka, Redis, PostgreSQL already in docker-compose.yml |
| Security | PASS | No new endpoints; JWT not required for consumers |
| Testing | PASS | Unit tests (mock ports) + integration tests (Testcontainers) per service affected |

No violations. Complexity Tracking table omitted.

## Project Structure

### Documentation (this feature)

```text
specs/003-reservation-expiry-consistency/
├── plan.md              ← This file
├── research.md          ← Phase 0 output
├── data-model.md        ← Phase 1 output
├── contracts/
│   └── reservation-expired-v2.json   ← Enriched event schema
└── tasks.md             ← Phase 2 output (created by /speckit.tasks)
```

### Source Code affected

```text
services/
├── inventory/
│   └── src/Inventory.Infrastructure/
│       ├── Workers/ReservationExpiryWorker.cs        ← Enrich event with customerId
│       └── EventConsumers/PaymentFailedConsumer.cs   ← NEW: release seat on payment-failed
│
├── ordering/
│   └── src/Ordering.Infrastructure/
│       └── Events/ReservationEventConsumer.cs        ← Add reservation-expired → Order.Cancel()
│
├── payment/
│   └── src/Payment.Infrastructure/
│       └── EventConsumers/ReservationEventConsumer.cs ← Add reservation-expired → Payment.Cancel()
│
└── notification/
    └── src/Notification.Infrastructure/
        └── Consumers/ReservationExpiredConsumer.cs   ← NEW: dispatch notification to customer
```

**Structure Decision**: All changes are in Infrastructure adapters (Kafka consumers). Domain entities already have the necessary state-transition methods (`Order.Cancel()`, `Seat.Release()`). No new ports needed — existing `IOrderRepository`, `IPaymentRepository`, `ISeatRepository`, `IReservationRepository` cover all read/write needs.

## Phase 0: Research

*See [research.md](./research.md)*

## Phase 1: Design & Contracts

*See [data-model.md](./data-model.md) and [contracts/](./contracts/)*
