# Research: Reservation Expiry Consistency

**Phase**: 0 — Outline & Research
**Date**: 2026-04-02

## R-001: Current State of `reservation-expired` Event Payload

**Decision**: The `ReservationExpiryWorker` publishes a JSON payload containing `{eventId, reservationId, seatId}`. The `customerId` field is absent because `Reservation.CustomerId` was not serialized.

**Finding**: `Reservation` entity stores `CustomerId` (string). The worker has access to the full `Reservation` object from the database before publishing. Adding `customerId` to the payload requires only a one-line change in the serialization object.

**Rationale**: Enriching the event at the source (Inventory worker) is the correct approach under event-driven best practices — consumers should not need to re-query the source to reconstruct context. This aligns with the "enriched event" pattern.

**Alternatives Considered**:
- Consumers re-query Inventory for `customerId` → rejected: adds inter-service HTTP coupling, breaks Hexagonal isolation.
- Add `customerId` via a Kafka header → rejected: non-standard; harder for consumers to process uniformly.

---

## R-002: Order Cancellation on `reservation-expired`

**Decision**: `Ordering.Infrastructure.Events.ReservationEventConsumer` already handles `reservation-expired` to remove from in-memory `ReservationStore`. It must also call `order.Cancel()` on the `bc_ordering.Orders` row linked to the expired reservation.

**Finding**: The Ordering service has no index from `reservationId → orderId`. The `ReservationStore` (in-memory) holds the `orderId` per `reservationId` (type `ActiveReservation`). The consumer can read the in-memory store first, then load the order from the DB.

**Risk**: In-memory store is volatile. On restart, the mapping is lost and orders may stay in draft permanently.

**Decision for this scope**: Use the in-memory store for the mapping (already present). Out-of-scope for this feature: persisting the store to DB (documented as known limitation). Idempotency: if the order is already cancelled or paid, skip.

**Rationale**: Minimal change; no new DB column required. Known limitation is documented.

---

## R-003: Payment Cancellation on `reservation-expired`

**Decision**: `Payment.Infrastructure.EventConsumers.ReservationEventConsumer` already calls `ExpireReservation()` on the in-memory `ReservationStateStore`. It must also cancel the `bc_payment.Payments` row with `status = 'pending'` for that reservation.

**Finding**: `Payment` entity in `bc_payment.Payments` has a `ReservationId` foreign key. Querying `IPaymentRepository.GetByReservationIdAsync(reservationId)` returns the relevant payment. If status is already terminal (succeeded, cancelled, failed), no action is needed.

**Rationale**: Local ACID transaction in the Payment service's own schema — fully constitution-compliant.

---

## R-004: Seat Release on `payment-failed`

**Decision**: A new Kafka consumer `PaymentFailedConsumer` in `Inventory.Infrastructure` must subscribe to topic `payment-failed`. On receiving a failure event it must: (1) load the reservation, (2) call `seat.Release()` and `reservation.Expire()`, (3) persist both via repositories.

**Finding**: The `payment-failed` event payload (from `Payment.Infrastructure`) currently contains `{eventId, orderId, reservationId}`. The `reservationId` is sufficient to look up both the reservation and the seat.

**Rationale**: Only the Inventory service owns seat availability — correct bounded context to handle this.

---

## R-005: Order Revert on `payment-failed`

**Decision**: A new handler in `Ordering.Infrastructure.Events` must consume `payment-failed` and call `order.Cancel()` on the matching draft/pending order.

**Finding**: The `payment-failed` event contains `orderId` directly — no lookup needed.

**Rationale**: Ordering service owns order lifecycle; it is the correct consumer for this event.

---

## R-006: Notification on `reservation-expired`

**Decision**: A new consumer `ReservationExpiredConsumer` in `Notification.Infrastructure` subscribes to `reservation-expired`. With the enriched payload (now including `customerId`), it dispatches a notification.

**Finding**: Notification service already exists with a Kafka consumer infrastructure. Adding a new consumer follows the same pattern as existing consumers.

**Rationale**: Notification is a cross-cutting concern handled by its own bounded context.

---

## R-007: Idempotency Strategy

**Decision**: All new consumers will check terminal state before applying changes.
- Order: if already `cancelled` or `paid`, skip.
- Payment: if already `cancelled`, `succeeded`, or `failed`, skip.
- Seat: if already not reserved, skip.
- Notification: no idempotency guard needed (duplicate notification is acceptable; at most once additional message).

**Rationale**: Domain entities already enforce valid state transitions (guard clauses). Attempting to cancel an already-cancelled order will either no-op or throw a safe domain exception that the consumer catches and ignores.

---

## R-008: No New Ports Required

**Decision**: All required repository interfaces already exist:
- `IOrderRepository` (Ordering.Application.Ports)
- `IPaymentRepository` (Payment.Application.Ports)
- `ISeatRepository` (Inventory.Application.Ports)
- `IReservationRepository` (Inventory.Application.Ports)

**Finding**: Consumers directly call existing port methods `GetByIdAsync`, `UpdateAsync`. No new port interface definitions needed.

**Rationale**: Reusing existing ports keeps the change surface minimal and avoids unnecessary abstractions.
