# Feature Specification: Reservation Expiry Consistency

**Feature Branch**: `003-reservation-expiry-consistency`
**Created**: 2026-04-02
**Status**: Draft
**Input**: Corregir consistencia del flujo de expiración de reservas: cancelar órdenes y pagos huérfanos, consumir payment-failed, incluir customerId en evento, notificar al usuario

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Cancelled Order on Seat Expiry (Priority: P1)

A customer selects a seat and adds it to their cart but fails to complete payment within the allowed time window. When the reservation expires, the system must automatically cancel the associated order so the seat becomes available for other buyers and the customer's cart no longer shows the expired item.

**Why this priority**: Without this, expired reservations leave orphaned orders in "draft" state. Customers may attempt to re-enter checkout on a cancelled reservation, and operators cannot trust order counts or inventory availability.

**Independent Test**: Trigger seat expiry for an order in draft state and verify the order transitions to cancelled state with the abandoned seat no longer associated.

**Acceptance Scenarios**:

1. **Given** a customer has a draft order containing a reserved seat, **When** the reservation expires (TTL exceeded), **Then** the order is automatically cancelled and the seat is no longer held for that customer.
2. **Given** a customer has a draft order with multiple seats, **When** one reservation expires, **Then** the entire order is cancelled and all associated seats are released.
3. **Given** an order that was already paid, **When** an expiry event is received for an old reservation, **Then** the paid order is not affected.

---

### User Story 2 - Cancelled Payment Record on Seat Expiry (Priority: P1)

When a reservation expires, any pending payment attempt associated with that reservation must be automatically cancelled. This ensures the payment system does not attempt to charge the customer for a seat that is no longer reserved, and financial records remain accurate.

**Why this priority**: A pending payment with no valid reservation could result in erroneous charges or permanent locks on the payment processor side, creating customer disputes and support burden.

**Independent Test**: Create a pending payment for a reservation, expire the reservation, and verify the payment record transitions to a cancelled state.

**Acceptance Scenarios**:

1. **Given** a pending payment exists for a reservation, **When** the reservation expires, **Then** the payment record is cancelled and no charge is processed.
2. **Given** no payment record exists for an expired reservation, **When** the expiry event is received, **Then** the system handles the case gracefully without error.

---

### User Story 3 - Seat Release After Failed Payment (Priority: P2)

When a customer's payment fails for a reserved seat, the seat must be automatically released so it becomes available for other buyers. Without this, a failed payment permanently blocks a seat from being sold.

**Why this priority**: Payment failures are common (insufficient funds, card declined). Without releasing the seat, inventory is permanently corrupted — seats appear unavailable even though they were never sold.

**Independent Test**: Process a failed payment event for a reserved seat and confirm the seat returns to available status and the reservation is marked expired.

**Acceptance Scenarios**:

1. **Given** a seat is reserved and its associated payment fails, **When** the payment failure event is processed, **Then** the seat is released and made available for reservation by other customers.
2. **Given** a payment failure event references a reservation that no longer exists, **When** the event is processed, **Then** the system handles the case gracefully without error.

---

### User Story 4 - Customer Notification on Reservation Expiry (Priority: P2)

When a customer's seat reservation expires because they did not complete payment in time, the customer must receive a notification informing them that their reservation has been released. This allows the customer to try again if they still want the ticket.

**Why this priority**: Without notification, customers have no awareness that their seat was released. They may attempt to return to checkout and receive confusing errors. Proactive notification improves customer experience and reduces support contacts.

**Independent Test**: Expire a reservation linked to a customer and verify a notification is dispatched to that customer's contact details.

**Acceptance Scenarios**:

1. **Given** a customer has an active reservation, **When** the reservation expires, **Then** the customer receives a notification that their reservation has expired and the seat is available again.
2. **Given** a reservation expires with no customer identifier, **When** the expiry is processed, **Then** the notification is skipped without system error.

---

### Edge Cases

- What happens when an expiry event arrives for a reservation that was already manually cancelled?
- What happens when the order service is temporarily unavailable when an expiry event is published?
- What happens when the same expiry event is delivered more than once (at-least-once delivery)?
- What happens when a payment failure event arrives after the reservation was already expired by TTL?
- What happens when a customer identifier is missing from the expiry event?

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST cancel an order in draft state when its associated reservation expires.
- **FR-002**: System MUST cancel a pending payment record when its associated reservation expires.
- **FR-003**: System MUST release a reserved seat and mark its reservation as expired when a payment fails.
- **FR-004**: System MUST cancel the associated order when a payment fails for that order's reservation.
- **FR-005**: System MUST include the customer identifier in every reservation-expiry notification so downstream consumers can contact the affected customer.
- **FR-006**: System MUST dispatch a notification to the customer when their reservation expires.
- **FR-007**: All expiry and failure handlers MUST be idempotent: processing the same event twice must not produce duplicate cancellations, double notifications, or errors.
- **FR-008**: System MUST NOT affect orders or payments in a terminal state (paid, cancelled, fulfilled) when expiry events arrive.

### Key Entities

- **Reservation**: Represents a time-limited hold on a seat for a specific customer. Has a TTL, a status (active/expired), and a customer identifier.
- **Order**: Represents a customer's intent to purchase one or more reserved seats. Has a lifecycle state (draft → pending → paid / cancelled).
- **Payment**: Represents a payment attempt for an order. Has a status (pending → succeeded / failed / cancelled).
- **Seat**: Represents a physical seat in a venue. Is either available or reserved.
- **Notification**: A message sent to a customer regarding a change in their reservation status.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: When a reservation expires, the associated draft order is cancelled within 30 seconds of the expiry event being published.
- **SC-002**: When a reservation expires, the associated pending payment record is cancelled within 30 seconds of the expiry event being published.
- **SC-003**: When a payment fails, the reserved seat becomes available for other customers within 30 seconds of the failure event being published.
- **SC-004**: 100% of expiry events that include a customer identifier result in a notification being dispatched to that customer.
- **SC-005**: Re-delivering the same expiry event produces no duplicate state changes or duplicate notifications (idempotency rate: 100%).
- **SC-006**: Zero orders remain in draft state with a corresponding expired reservation after the expiry processing window.

## Assumptions

- The notification delivery mechanism (email, push, SMS) is outside the scope of this feature; only the dispatch of the notification event is required.
- The TTL for reservations is 15 minutes (already established by the domain).
- Events are delivered at-least-once; idempotency is a hard requirement.
- Orders with a single seat item are the primary case; multi-seat orders follow the same cancellation rule (cancel the whole order).
