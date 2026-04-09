---
title: Inventory Service
description: Reservas de asientos con locks distribuidos, TTL automático y coreografía de eventos
---

# Inventory Service

## Propósito

El Inventory Service es el guardián del inventario de asientos. Su responsabilidad es garantizar que cada asiento sea reservado por un solo usuario a la vez, gestionar el tiempo de vida de las reservas (15 minutos) y liberar los asientos cuando las reservas expiran o los pagos fallan.

Es el servicio con mayor complejidad técnica del sistema porque combina:
- **Locks distribuidos** para prevenir condiciones de carrera
- **TTL automático** con un worker en background
- **Coreografía de eventos** como productor principal del flujo de compra

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | .NET 9 — Minimal APIs |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL — schema `bc_inventory` |
| Cache / Locks | Redis (distributed locks) |
| Mensajería | Apache Kafka (productor y consumidor) |
| Mediator | MediatR |
| Puerto | `5002` (local), `50002` (Docker) |

---

## Estructura Interna

```
services/inventory/
├── Api/
│   └── Endpoints/
│       └── ReservationEndpoints.cs      ← POST /reservations
├── Application/
│   └── Commands/
│       ├── CreateReservationCommand.cs
│       └── CreateReservationCommandHandler.cs
├── Domain/
│   └── Entities/
│       ├── Seat.cs                       ← id, eventId, seatNumber, section, status
│       └── Reservation.cs                ← id, seatId, customerId, expiresAt, status
└── Infrastructure/
    ├── Persistence/
    │   ├── InventoryDbContext.cs
    │   ├── SeatRepository.cs
    │   └── ReservationRepository.cs
    ├── Locking/
    │   └── RedisLockService.cs           ← Distributed lock por asiento
    ├── Messaging/
    │   ├── ReservationProducer.cs        ← Produce: reservation-created
    │   ├── PaymentFailedConsumer.cs      ← Consume: payment-failed
    │   └── SeatsGeneratedConsumer.cs     ← Consume: seats-generated
    └── BackgroundServices/
        └── ReservationExpiryWorker.cs    ← Escanea y expira reservas vencidas
```

---

## Endpoints

### `POST /reservations`

Reserva un asiento específico para un usuario.

**Request:**
```json
{
  "seatId": "uuid",
  "customerId": "uuid-o-email",
  "eventId": "uuid"
}
```

**Response 201:**
```json
{
  "reservationId": "uuid",
  "seatId": "uuid",
  "customerId": "uuid",
  "expiresAt": "2026-04-06T13:15:00Z",
  "status": "Active"
}
```

**Códigos de respuesta:**
| Código | Situación |
|--------|-----------|
| `201` | Reserva creada exitosamente |
| `404` | El asiento no existe |
| `409` | El asiento ya está reservado |
| `500` | Error interno (ej: fallo del lock) |

---

## Lógica de Reserva (Detalle)

El handler `CreateReservationCommandHandler` ejecuta la siguiente secuencia:

```
1. Adquirir Redis lock en "seat:{seatId}"
   └── Si no puede obtenerlo → retornar 409 (ya reservado)

2. Verificar estado del asiento en DB
   └── Si status != Available → retornar 409

3. Crear Reservation con expiresAt = NOW + 15 minutos
4. Actualizar Seat.Status = Reserved

5. Persistir en bc_inventory (transacción)

6. Publicar evento "reservation-created" en Kafka

7. Liberar Redis lock
```

El lock de Redis protege el paso 2-5 de condiciones de carrera: si dos usuarios intentan reservar el mismo asiento simultáneamente, solo uno obtendrá el lock. El otro recibirá 409.

---

## Worker de Expiración — ReservationExpiryWorker

Un `IHostedService` que corre en background y escanea reservas vencidas periódicamente.

**Proceso:**
```
Cada N segundos:
1. Buscar reservas con status=Active y expiresAt < NOW
2. Para cada reserva expirada:
   a. Actualizar Reservation.Status = Expired
   b. Actualizar Seat.Status = Available
   c. Publicar "reservation-expired" en Kafka
   d. Llamar GET /api/v1/waitlist/has-pending?eventId={id} (ADR-03)
      └── Si hay pendientes: Waitlist inicia asignación automática
```

El timeout de la llamada a Waitlist es de **200ms** (ADR-03). Si Waitlist no responde, el asiento se libera igualmente.

---

## Esquema de Base de Datos

**Schema:** `bc_inventory`

```sql
CREATE TABLE "Seats" (
    "Id"          UUID PRIMARY KEY,
    "EventId"     UUID NOT NULL,
    "SeatNumber"  VARCHAR(20) NOT NULL,
    "Section"     VARCHAR(100),
    "Status"      VARCHAR(50) NOT NULL DEFAULT 'Available',
    "CreatedAt"   TIMESTAMP NOT NULL
);

CREATE TABLE "Reservations" (
    "Id"          UUID PRIMARY KEY,
    "SeatId"      UUID NOT NULL REFERENCES "Seats"("Id"),
    "CustomerId"  VARCHAR(255) NOT NULL,
    "ExpiresAt"   TIMESTAMP NOT NULL,
    "Status"      VARCHAR(50) NOT NULL DEFAULT 'Active',
    "CreatedAt"   TIMESTAMP NOT NULL
);
```

---

## Mensajería Kafka

### Produce: `reservation-created`

Publicado inmediatamente después de crear una reserva exitosa.

```json
{
  "eventId": "uuid",
  "reservationId": "uuid",
  "customerId": "uuid",
  "seatId": "uuid",
  "seatNumber": "A-01",
  "section": "VIP",
  "basePrice": 150.00,
  "createdAt": "2026-04-06T13:00:00Z",
  "expiresAt": "2026-04-06T13:15:00Z",
  "status": "Active"
}
```

**Consumidores:** Ordering (caché de reservas), Payment (validación)

---

### Produce: `reservation-expired`

Publicado por `ReservationExpiryWorker` cuando una reserva vence.

```json
{
  "eventId": "uuid",
  "reservationId": "uuid",
  "customerId": "uuid",
  "seatId": "uuid",
  "expiresAt": "2026-04-06T13:15:00Z",
  "expiredAt": "2026-04-06T13:16:00Z",
  "status": "Expired"
}
```

**Consumidores:** Waitlist

---

### Consume: `payment-failed`

Cuando un pago falla, Inventory libera el asiento reservado.

```json
{
  "reservationId": "uuid",
  "seatId": "uuid",
  "status": "failed"
}
```

**Acción:** Reserva → `Released`, Asiento → `Available`

---

### Consume: `seats-generated`

Cuando Catalog genera asientos para un evento, Inventory sincroniza su copia local.

---

## Configuración Redis

```json
{
  "Redis": {
    "ConnectionString": "localhost:6379",
    "LockTimeout": "00:00:00.200"
  }
}
```

El lock se implementa con el patrón `SET key value NX PX 200` — atómico, con expiración automática de 200ms si el proceso falla.

---

## Notas de Diseño

- El TTL de 15 minutos es un equilibrio entre experiencia de usuario (tiempo suficiente para decidir) y disponibilidad (no bloquear asientos indefinidamente)
- El `ReservationExpiryWorker` es un ejemplo de **process manager** dentro del servicio
- En un sistema productivo, el lock de Redis se reemplazaría por Redlock (algoritmo multi-nodo) para mayor resiliencia
