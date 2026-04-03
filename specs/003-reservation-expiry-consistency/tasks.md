# Tasks: Reservation Expiry Consistency

**Input**: Design documents from `/specs/003-reservation-expiry-consistency/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Tests**: Unit test tasks included per service affected (existing test projects reused).

**Organization**: Tasks grouped by user story to enable independent implementation and testing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1..US4)

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm existing port interfaces needed by new consumers are available; no new scaffolding required.

- [x] T001 Verify `IOrderRepository.GetByIdAsync` and `UpdateAsync` exist in `services/ordering/src/Ordering.Application/Ports/IOrderRepository.cs` — also added `GetActiveOrderBySeatIdAsync`
- [x] T002 [P] Verify `IPaymentRepository.GetByReservationIdAsync` (or equivalent) exists in `services/payment/src/Payment.Application/Ports/IPaymentRepository.cs` — added `GetByReservationIdAsync`; also added `Payment.Cancel()` + `StatusCancelled`
- [x] T003 [P] Verify `ISeatRepository.GetByIdAsync` and `UpdateAsync` exist in `services/inventory/src/Inventory.Application/Ports/ISeatRepository.cs` — confirmed present
- [x] T004 [P] Verify `IReservationRepository.GetByIdAsync` and `UpdateAsync` exist in `services/inventory/src/Inventory.Application/Ports/IReservationRepository.cs` — added both methods

**Checkpoint**: All required port interfaces confirmed — consumer implementation can begin.

---

## Phase 2: Foundational (Blocking Prerequisite)

**Purpose**: Enrich the `reservation-expired` Kafka event with `customerId` — all downstream consumer changes (US1, US2, US4) depend on this field being present.

- [x] T005 Read `services/inventory/src/Inventory.Infrastructure/Workers/ReservationExpiryWorker.cs` in full
- [x] T006 Add `customerId` field to the anonymous/DTO object serialized by `ReservationExpiryWorker` when publishing to Kafka topic `reservation-expired` (file: `services/inventory/src/Inventory.Infrastructure/Workers/ReservationExpiryWorker.cs`)

**Checkpoint**: `reservation-expired` events now carry `customerId`. US1, US2, US4 consumers can be implemented.

---

## Phase 3: User Story 1 — Cancelled Order on Seat Expiry (Priority: P1) 🎯 MVP

**Goal**: When a reservation expires, the associated draft order is cancelled in `bc_ordering.Orders`.

**Independent Test**: Publish a `reservation-expired` event for a reservation linked to a draft order. Verify the order row transitions to `cancelled` state.

### Implementation for User Story 1

- [x] T007 [US1] Read `services/ordering/src/Ordering.Infrastructure/Events/ReservationEventConsumer.cs` in full
- [x] T008 [US1] Read `services/ordering/src/Ordering.Infrastructure/Events/ReservationStore.cs` in full — confirmed no orderId; used `GetActiveOrderBySeatIdAsync` instead
- [x] T009 [US1] In `ReservationEventConsumer.cs` (Ordering), add handling for `reservation-expired` case: look up order by seatId via `GetActiveOrderBySeatIdAsync`, call `order.Cancel()`, then `UpdateAsync` (file: `services/ordering/src/Ordering.Infrastructure/Events/ReservationEventConsumer.cs`)
- [ ] T010 [US1] Add unit test `Handle_ReservationExpired_CancelsOrder_WhenOrderIsDraft` in `services/ordering/tests/unit/Ordering.Application.UnitTests/` (or integration test in `Ordering.IntegrationTests`)

**Checkpoint**: US1 fully testable — draft order is cancelled when its reservation expires.

---

## Phase 4: User Story 2 — Cancelled Payment Record on Seat Expiry (Priority: P1)

**Goal**: When a reservation expires, the associated pending payment record in `bc_payment.Payments` is cancelled.

**Independent Test**: Publish a `reservation-expired` event for a reservation with a pending payment. Verify the payment row transitions to `cancelled` state.

### Implementation for User Story 2

- [x] T011 [US2] Read `services/payment/src/Payment.Infrastructure/EventConsumers/ReservationEventConsumer.cs` in full
- [x] T012 [US2] Read `services/payment/src/Payment.Domain/Entities/Payment.cs` — confirmed no `Cancel()`; added `Cancel()` + `StatusCancelled` to domain entity
- [x] T013 [US2] In `Payment.Infrastructure/EventConsumers/ReservationEventConsumer.cs`, add DB cancellation on `reservation-expired`: query payment by `reservationId` via `IPaymentRepository`, call `payment.Cancel()` if status is `pending`, then `UpdateAsync` (file: `services/payment/src/Payment.Infrastructure/EventConsumers/ReservationEventConsumer.cs`)
- [ ] T014 [US2] Add unit test `Handle_ReservationExpired_CancelsPayment_WhenPaymentIsPending` in `services/payment/tests/unit/`

**Checkpoint**: US2 fully testable — pending payment is cancelled when its reservation expires.

---

## Phase 5: User Story 3 — Seat Release After Failed Payment (Priority: P2)

**Goal**: When a payment fails, the seat is released and the reservation marked expired in Inventory; the associated order is cancelled in Ordering.

**Independent Test**: Publish a `payment-failed` event. Verify seat `Reserved = false`, reservation `Status = expired` in Inventory and order `State = cancelled` in Ordering.

### Implementation for User Story 3 — Inventory side

- [x] T015 [P] [US3] Create `services/inventory/src/Inventory.Infrastructure/Consumers/PaymentFailedConsumer.cs` — subscribe to Kafka topic `payment-failed`, load reservation by `reservationId`, call `seat.Release()` and set reservation status to expired, persist via `ISeatRepository.UpdateAsync` and `IReservationRepository.UpdateAsync`
- [x] T016 [P] [US3] Register `PaymentFailedConsumer` in the Inventory service DI container (`ServiceCollectionExtensions.cs`)

### Implementation for User Story 3 — Ordering side

- [x] T017 [P] [US3] Read `services/ordering/src/Ordering.Infrastructure/Events/` directory — no existing `PaymentFailedConsumer`; created new file
- [x] T018 [P] [US3] Created `services/ordering/src/Ordering.Infrastructure/Events/PaymentFailedConsumer.cs` — handles `payment-failed` event: load order by `orderId`, call `order.Cancel()` if not terminal, persist via `IOrderRepository.UpdateAsync`; registered in DI
- [ ] T019 [US3] Add unit tests: `PaymentFailed_ReleasesSeat` in Inventory unit tests; `PaymentFailed_CancelsOrder` in Ordering unit tests

**Checkpoint**: US3 fully testable — failed payment releases seat and cancels order.

---

## Phase 6: User Story 4 — Customer Notification on Reservation Expiry (Priority: P2)

**Goal**: When a reservation expires, a notification is dispatched to the customer identified by `customerId` in the enriched event.

**Independent Test**: Publish a `reservation-expired` event with a `customerId`. Verify the Notification service consumer processes it and dispatches a notification event/message for that customer.

### Implementation for User Story 4

- [x] T020 [US4] Read `services/notification/src/` directory — confirmed `IEmailService` pattern with `TicketIssuedEventConsumer` as reference
- [x] T021 [US4] Created `services/notification/src/Notification.Infrastructure/Events/ReservationExpiredConsumer.cs` — subscribes to `reservation-expired`, reads `customerId`, dispatches via `IEmailService`
- [x] T022 [US4] Registered `ReservationExpiredConsumer` in `ServiceCollectionExtensions.cs`
- [ ] T023 [US4] Add unit test `Handle_ReservationExpired_DispatchesNotification_WhenCustomerIdPresent` in `services/notification/tests/unit/`

**Checkpoint**: US4 fully testable — notification dispatched when reservation expires.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Idempotency hardening and validation across all new consumers.

- [x] T024 [P] Verify all new consumers skip processing if the target entity is already in a terminal state (idempotency guard) — confirmed in all 4 consumers
- [x] T025 [P] Verify `PaymentFailedConsumer` (Inventory) handles `null` reservation gracefully — null check + early return implemented
- [x] T026 [P] Verify Ordering consumer for `payment-failed` handles missing/already-cancelled order gracefully — null check + state check implemented
- [x] T027 Run all service test suites — Inventory: 12/12 ✅, Ordering unit: 22/22 ✅, Identity: 7/7 ✅, Ordering integration: 2/2 ✅

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 — BLOCKS US1, US2, US4 (those read `customerId` from event)
- **US1 (Phase 3)**: Depends on Phase 2 (enriched event) + Phase 1 (ports verified)
- **US2 (Phase 4)**: Depends on Phase 2 + Phase 1 — can run in parallel with US1
- **US3 (Phase 5)**: Depends only on Phase 1 — `payment-failed` event already has `reservationId`/`orderId`; **can start immediately after Phase 1**
- **US4 (Phase 6)**: Depends on Phase 2 (needs `customerId` in payload)
- **Polish (Phase 7)**: Depends on all user story phases

### Parallel Opportunities

- T001..T004 (Phase 1) can all run in parallel
- US1 (Phase 3) + US2 (Phase 4) can run in parallel after Phase 2
- US3 (Phase 5) can start in parallel with Phase 2 (different event topic, no dependency)
- US4 (Phase 6) can start in parallel with US1 + US2 after Phase 2
- T015 + T017 (Inventory and Ordering sides of US3) are fully parallel

---

## Implementation Strategy

### MVP First (US1 + Foundational)

1. Phase 1: Verify ports
2. Phase 2: Enrich event with `customerId`
3. Phase 3: Cancel order on `reservation-expired`
4. **STOP and VALIDATE**: Expired reservation → cancelled order works end-to-end
5. Continue with US2, US3, US4

### Full Delivery Order

Phase 1 → Phase 2 → (Phase 3 ∥ Phase 4 ∥ Phase 5) → Phase 6 → Phase 7

---

## Task Summary

| Phase | User Story | Tasks | Parallelizable |
|-------|------------|-------|----------------|
| 1     | Setup      | T001–T004 | All 4 |
| 2     | Foundational | T005–T006 | 0 (sequential) |
| 3     | US1 (P1)   | T007–T010 | 0 (sequential chain) |
| 4     | US2 (P1)   | T011–T014 | 0 (sequential chain) |
| 5     | US3 (P2)   | T015–T019 | T015∥T016∥T017∥T018 |
| 6     | US4 (P2)   | T020–T023 | 0 (sequential chain) |
| 7     | Polish     | T024–T027 | T024∥T025∥T026 |

**Total tasks**: 27
**Suggested MVP scope**: Phases 1–3 (T001–T010)
