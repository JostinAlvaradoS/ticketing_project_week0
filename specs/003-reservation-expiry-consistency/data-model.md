# Data Model: Reservation Expiry Consistency

**Phase**: 1 — Design & Contracts
**Date**: 2026-04-02

## Affected Entities

No new entities are introduced. All changes are to existing entities' state transitions and event payloads.

---

### Reservation (Inventory — `bc_inventory.Reservations`)

| Field       | Type     | Notes                                      |
|-------------|----------|--------------------------------------------|
| Id          | Guid     | Primary key                                |
| SeatId      | Guid     | FK → Seats.Id                              |
| CustomerId  | string   | Identifier of the customer who reserved    |
| Status      | string   | `active` / `expired`                       |
| CreatedAt   | DateTime | UTC                                        |
| ExpiresAt   | DateTime | `CreatedAt + TTL` (default 15 min)         |

**State Transitions**:
```
active ──[TTL exceeded]──► expired   (ReservationExpiryWorker)
active ──[payment-failed]─► expired  (PaymentFailedConsumer — NEW)
```

---

### Seat (Inventory — `bc_inventory.Seats`)

| Field    | Type   | Notes                     |
|----------|--------|---------------------------|
| Id       | Guid   | Primary key               |
| Section  | string |                           |
| Row      | string |                           |
| Number   | int    |                           |
| Reserved | bool   | `true` = held; `false` = available |

**State Transitions**:
```
Reserved=true ──[TTL expired]──► Reserved=false   (ReservationExpiryWorker — existing)
Reserved=true ──[payment-failed]─► Reserved=false  (PaymentFailedConsumer — NEW)
```

---

### Order (Ordering — `bc_ordering.Orders`)

| Field       | Type     | Notes                                  |
|-------------|----------|----------------------------------------|
| Id          | Guid     | Primary key                            |
| UserId      | string   |                                        |
| State       | string   | `draft` / `pending` / `paid` / `cancelled` |
| TotalAmount | decimal  |                                        |
| CreatedAt   | DateTime |                                        |

**State Transitions** (affected by this feature):
```
draft   ──[reservation-expired]──► cancelled  (ReservationEventConsumer — UPDATED)
pending ──[payment-failed]───────► cancelled  (PaymentFailedConsumer — NEW in Ordering)
```

---

### Payment (Payment — `bc_payment.Payments`)

| Field         | Type     | Notes                                          |
|---------------|----------|------------------------------------------------|
| Id            | Guid     | Primary key                                    |
| OrderId       | Guid     | FK → Orders.Id (logical, cross-service)        |
| ReservationId | Guid     | FK → Reservations.Id (logical, cross-service)  |
| Amount        | decimal  |                                                |
| Status        | string   | `pending` / `succeeded` / `failed` / `cancelled` |
| CreatedAt     | DateTime |                                                |

**State Transitions** (affected by this feature):
```
pending ──[reservation-expired]──► cancelled  (ReservationEventConsumer — UPDATED)
```

---

## Event Schema Changes

### `reservation-expired` — Enriched Payload (v2)

**Before**:
```json
{
  "eventId": "string (uuid)",
  "reservationId": "string (uuid)",
  "seatId": "string (uuid)"
}
```

**After** (this feature):
```json
{
  "eventId": "string (uuid)",
  "reservationId": "string (uuid)",
  "seatId": "string (uuid)",
  "customerId": "string"
}
```

**Change**: `customerId` field added. All existing consumers that ignore unknown fields are unaffected.

---

### `payment-failed` — No Schema Change Required

Existing payload already includes `orderId` and `reservationId`:
```json
{
  "eventId": "string (uuid)",
  "orderId": "string (uuid)",
  "reservationId": "string (uuid)",
  "reason": "string"
}
```

New consumers in Inventory and Ordering will read `reservationId` and `orderId` respectively.
