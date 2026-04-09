---
title: Contratos de Eventos Kafka
description: Schemas JSON de todos los eventos que fluyen por el sistema SpecKit Ticketing
---

# Contratos de Eventos Kafka

## Propósito

Los contratos de eventos son el acuerdo entre servicios productores y consumidores. Definen exactamente qué campos contiene cada evento, cuáles son obligatorios y qué tipos tienen. Sin contratos claros, un cambio en un servicio puede romper silenciosamente a otro.

Los schemas se encuentran en `contracts/kafka/` y sirven como documentación viva del protocolo de mensajería del sistema.

---

## Topics y Flujo

```
[Inventory] ──reservation-created──► [Ordering, Payment]
[Inventory] ──reservation-expired──► [Waitlist]
[Catalog]   ──seats-generated──────► [Inventory]
[Payment]   ──payment-succeeded────► [Fulfillment, Inventory]
[Payment]   ──payment-failed───────► [Ordering, Inventory]
[Fulfillment] ──ticket-issued──────► [Notification]
```

---

## `reservation-created`

**Productor:** Inventory Service
**Consumidores:** Ordering Service, Payment Service

Publicado inmediatamente después de que un asiento es reservado exitosamente.

```json
{
  "$schema": "http://json-schema.org/draft-07/schema",
  "type": "object",
  "required": ["eventId", "reservationId", "customerId", "seatId", "seatNumber", "createdAt", "expiresAt", "status"],
  "properties": {
    "eventId":       { "type": "string", "format": "uuid" },
    "reservationId": { "type": "string", "format": "uuid" },
    "customerId":    { "type": "string" },
    "seatId":        { "type": "string", "format": "uuid" },
    "seatNumber":    { "type": "string", "example": "A-01" },
    "section":       { "type": "string", "example": "VIP" },
    "basePrice":     { "type": "number", "minimum": 0 },
    "createdAt":     { "type": "string", "format": "date-time" },
    "expiresAt":     { "type": "string", "format": "date-time" },
    "status":        { "type": "string", "enum": ["Active"] }
  }
}
```

**Ejemplo:**
```json
{
  "eventId": "a1b2c3d4-...",
  "reservationId": "e5f6g7h8-...",
  "customerId": "user@example.com",
  "seatId": "i9j0k1l2-...",
  "seatNumber": "A-01",
  "section": "VIP",
  "basePrice": 150.00,
  "createdAt": "2026-04-06T13:00:00Z",
  "expiresAt": "2026-04-06T13:15:00Z",
  "status": "Active"
}
```

---

## `reservation-expired`

**Productor:** Inventory Service (ReservationExpiryWorker)
**Consumidores:** Waitlist Service

Publicado cuando el TTL de 15 minutos de una reserva se cumple sin que el usuario complete el pago.

```json
{
  "type": "object",
  "required": ["eventId", "reservationId", "customerId", "seatId", "expiresAt", "expiredAt", "status"],
  "properties": {
    "eventId":       { "type": "string", "format": "uuid" },
    "reservationId": { "type": "string", "format": "uuid" },
    "customerId":    { "type": "string" },
    "seatId":        { "type": "string", "format": "uuid" },
    "expiresAt":     { "type": "string", "format": "date-time" },
    "expiredAt":     { "type": "string", "format": "date-time" },
    "status":        { "type": "string", "enum": ["Expired"] }
  }
}
```

---

## `seats-generated`

**Productor:** Catalog Service (endpoint admin)
**Consumidores:** Inventory Service

Publicado cuando un administrador genera los asientos de un evento por primera vez.

```json
{
  "type": "object",
  "required": ["eventId", "totalSeatsGenerated", "createdAt"],
  "properties": {
    "eventId":             { "type": "string", "format": "uuid" },
    "totalSeatsGenerated": { "type": "integer", "minimum": 1 },
    "sections": {
      "type": "array",
      "items": {
        "type": "object",
        "properties": {
          "name":  { "type": "string" },
          "seats": { "type": "integer" }
        }
      }
    },
    "createdAt": { "type": "string", "format": "date-time" }
  }
}
```

**Ejemplo:**
```json
{
  "eventId": "a1b2c3d4-...",
  "totalSeatsGenerated": 1100,
  "sections": [
    { "name": "VIP",     "seats": 100 },
    { "name": "General", "seats": 1000 }
  ],
  "createdAt": "2026-04-06T10:00:00Z"
}
```

---

## `payment-succeeded`

**Productor:** Payment Service
**Consumidores:** Fulfillment Service, Inventory Service

Publicado cuando el simulador de pagos aprueba la transacción.

```json
{
  "type": "object",
  "required": ["paymentId", "orderId", "customerId", "amount", "currency", "paymentMethod", "processedAt", "status"],
  "properties": {
    "paymentId":     { "type": "string", "format": "uuid" },
    "orderId":       { "type": "string", "format": "uuid" },
    "customerId":    { "type": "string" },
    "reservationId": { "type": "string", "format": "uuid" },
    "amount":        { "type": "number", "minimum": 0 },
    "currency":      { "type": "string", "example": "USD" },
    "paymentMethod": { "type": "string", "enum": ["credit_card", "debit_card", "wallet", "bank_transfer"] },
    "transactionId": { "type": "string" },
    "processedAt":   { "type": "string", "format": "date-time" },
    "status":        { "type": "string", "enum": ["succeeded"] }
  }
}
```

---

## `payment-failed`

**Productor:** Payment Service
**Consumidores:** Ordering Service, Inventory Service

Publicado cuando el simulador de pagos rechaza la transacción.

```json
{
  "type": "object",
  "required": ["paymentId", "orderId", "customerId", "amount", "currency", "failedAt", "status"],
  "properties": {
    "paymentId":     { "type": "string", "format": "uuid" },
    "orderId":       { "type": "string", "format": "uuid" },
    "customerId":    { "type": "string" },
    "reservationId": { "type": "string", "format": "uuid" },
    "amount":        { "type": "number" },
    "currency":      { "type": "string" },
    "failureReason": {
      "type": "string",
      "enum": ["insufficient_funds", "card_declined", "expired_card", "network_error"]
    },
    "failedAt":      { "type": "string", "format": "date-time" },
    "status":        { "type": "string", "enum": ["failed"] }
  }
}
```

---

## `ticket-issued`

**Productor:** Fulfillment Service
**Consumidores:** Notification Service

Publicado cuando el boleto digital ha sido generado exitosamente.

```json
{
  "type": "object",
  "required": ["ticketId", "ticketNumber", "orderId", "paymentId", "customerId", "eventId", "seatId", "seatNumber", "qrCode", "issuedAt", "status"],
  "properties": {
    "ticketId":     { "type": "string", "format": "uuid" },
    "ticketNumber": { "type": "string", "example": "TKT-20260406-0001" },
    "orderId":      { "type": "string", "format": "uuid" },
    "paymentId":    { "type": "string", "format": "uuid" },
    "customerId":   { "type": "string" },
    "eventId":      { "type": "string", "format": "uuid" },
    "seatId":       { "type": "string", "format": "uuid" },
    "seatNumber":   { "type": "string" },
    "section":      { "type": "string" },
    "pdfPath":      { "type": "string" },
    "qrCode":       { "type": "string", "description": "Base64-encoded QR code data" },
    "issuedAt":     { "type": "string", "format": "date-time" },
    "status":       { "type": "string", "enum": ["generated", "delivered", "used", "cancelled"] }
  }
}
```

**Ejemplo:**
```json
{
  "ticketId":     "x1y2z3-...",
  "ticketNumber": "TKT-20260406-0001",
  "orderId":      "e5f6g7-...",
  "paymentId":    "p1q2r3-...",
  "customerId":   "user@example.com",
  "eventId":      "a1b2c3-...",
  "seatId":       "i9j0k1-...",
  "seatNumber":   "A-01",
  "section":      "VIP",
  "pdfPath":      "/app/data/tickets/x1y2z3.pdf",
  "qrCode":       "iVBORw0KGgoAAAANSUhEUgAA...",
  "issuedAt":     "2026-04-06T13:05:30Z",
  "status":       "generated"
}
```

---

## Semántica de Entrega

Kafka está configurado con semántica **at-least-once**: un evento puede ser entregado más de una vez en casos de fallo. Por eso cada consumidor implementa **idempotencia**:

| Servicio | Mecanismo de Idempotencia |
|---------|--------------------------|
| Ordering | `ReservationStore` ignora reservas ya registradas |
| Payment | Verifica existencia de Payment por `orderId` antes de crear uno nuevo |
| Fulfillment | `UNIQUE` constraint en `Tickets.OrderId` |
| Notification | `UNIQUE` constraint en `EmailNotifications.OrderId` |

---

## Evolución de Contratos

Para agregar un campo a un evento:
1. Hacerlo opcional en el schema (`required` solo para campos existentes)
2. Los consumidores ignorarán el nuevo campo hasta actualizarse
3. Actualizar consumidores gradualmente (compatibilidad hacia atrás)

Para eliminar un campo:
1. Deprecarlo primero (marcar en schema, notificar a consumidores)
2. Verificar que ningún consumidor lo use
3. Eliminar del schema y del productor
