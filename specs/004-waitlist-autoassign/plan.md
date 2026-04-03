# Implementation Plan: Waitlist Autoassign

**Branch**: `004-waitlist-autoassign` | **Date**: 2026-04-02 | **Spec**: [spec.md](./spec.md)

---

## Índice

1. [Resumen Ejecutivo](#resumen-ejecutivo)
2. [Technical Context](#technical-context)
3. [Constitution Check](#constitution-check)
4. [Análisis del Dominio Corregido](#análisis-del-dominio-corregido)
5. [Decisiones Arquitectónicas (ADRs)](#decisiones-arquitectónicas-adrs)
6. [Diseño de Datos](#diseño-de-datos)
7. [Diseño de Eventos Kafka](#diseño-de-eventos-kafka)
8. [Flujos Técnicos Detallados](#flujos-técnicos-detallados)
9. [Cambios a Servicios Existentes](#cambios-a-servicios-existentes)
10. [Estructura del Nuevo Microservicio](#estructura-del-nuevo-microservicio)
11. [Plan de Pruebas Técnico](#plan-de-pruebas-técnico)
12. [Riesgos e Impactos](#riesgos-e-impactos)
13. [Estimación de Esfuerzo](#estimación-de-esfuerzo)
14. [Project Structure](#project-structure)

---

## Resumen Ejecutivo

La feature agrega un **Waitlist Service** independiente que gestiona una cola FIFO de emails por evento. Cuando una reserva expira y hay personas en cola, el Waitlist Service:

1. Intercepta el evento `reservation-expired` antes de que el inventario libere el asiento.
2. Crea automáticamente una orden de compra de invitado en el Ordering Service (usando el email como `GuestToken`).
3. Notifica al usuario con enlace de pago válido 30 minutos.
4. Si el usuario no paga en 30 min, rota al siguiente en cola sin liberar el asiento al pool general.

**Decisiones MVP resueltas en este plan:**

| Bloqueante original | Decisión tomada |
|---|---|
| BLOCKER 1: `EventId` ausente en `reservation-expired` | Agregar `EventId` a `Inventory.Reservation` + payload del evento |
| BLOCKER 2: `order-payment-timeout` sin mecanismo | **Opción A**: Waitlist Service gestiona el timer de 30 min internamente |
| BLOCKER 3: Email vs UserId | El email se usa como `GuestToken` — sin cambios a Ordering existente |
| BLOCKER 4: Race condition al liberar asiento | `ReservationExpiryWorker` consulta la cola antes de llamar `seat.Release()` |

---

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: MediatR 12.2.0, EF Core 8 + Npgsql, Confluent.Kafka 2.5.0, FluentValidation, xUnit + FluentAssertions + Moq
**Storage**: PostgreSQL schema `bc_waitlist` (nuevo); lectura de `bc_catalog` para validar stock
**Testing**: xUnit + FluentAssertions + Moq (unit); Testcontainers + WebApplicationFactory (integration)
**Target Platform**: Linux (Docker Compose), macOS dev
**Project Type**: Microservicio .NET 8 (Minimal API)
**Performance Goals**: P95 < 5s desde `reservation-expired` hasta correo enviado (SC-001)
**Constraints**: Sin autenticación; email como GuestToken; idempotencia obligatoria en todos los consumers
**Scale/Scope**: Una cola por `EventId`; MVP con un asiento por asignación

---

## Constitution Check

| Principio | Estado | Notas |
|---|---|---|
| Hexagonal (Ports & Adapters) | PASS | Puertos en `Waitlist.Application`, adaptadores en `Waitlist.Infrastructure` |
| PostgreSQL compartida + schema `bc_waitlist` | PASS | Nuevo schema; migration ownership en Waitlist Service |
| DbContext/Migrations | PASS | `WaitlistDbContext` dedicado; migrations solo para `bc_waitlist` |
| Comunicación: Kafka async | PASS | Consume `reservation-expired`, `payment-succeeded`; no HTTP saliente excepto a Catalog |
| Transacciones ACID locales | PASS | Todas las operaciones sobre `bc_waitlist` son transacciones locales |
| Local Dev Topology | PASS | Docker Compose entry requerido; Kafka y PostgreSQL ya están declarados |
| Seguridad | PASS | Sin JWT; email validado por FluentValidation; no se almacenan contraseñas |
| Testing | PASS | Unit tests para handlers; integration tests con Testcontainers |

**No hay violaciones al Constitution.**

---

## Análisis del Dominio Corregido

### Bounded Contexts y sus responsabilidades

```
┌────────────────────────────────────────────────────────────────┐
│  bc_catalog (Catalog Service)                                  │
│  Seat.EventId, Seat.Status (available/reserved/sold)           │
│  → Fuente de verdad para disponibilidad de eventos             │
└─────────────────────────────┬──────────────────────────────────┘
                              │  HTTP GET /events/{eventId}/availability
                              ▼
┌────────────────────────────────────────────────────────────────┐
│  bc_waitlist (Waitlist Service) ← NUEVO                        │
│  WaitlistEntry: email, eventId, seatId*, status, timestamps    │
│  Cola FIFO por (eventId, status=Pending, registeredAt ASC)     │
│  Gestiona TTL de 30 min para asignaciones                      │
└─────────────────────────────┬──────────────────────────────────┘
                              │  HTTP POST /orders (GuestToken=email)
                              ▼
┌────────────────────────────────────────────────────────────────┐
│  bc_ordering (Ordering Service) — MODIFICADO MÍNIMO            │
│  Order.GuestToken = email del waitlist                         │
│  HTTP POST /orders/waitlist (nuevo endpoint interno)           │
└────────────────────────────────────────────────────────────────┘

┌────────────────────────────────────────────────────────────────┐
│  bc_inventory (Inventory Service) — MODIFICADO                 │
│  Reservation.EventId ← NUEVO CAMPO                             │
│  reservation-expired payload incluye eventId ← ENRIQUECIDO    │
│  ReservationExpiryWorker: NO libera asiento si hay cola activa │
└────────────────────────────────────────────────────────────────┘
```

### Entidad WaitlistEntry

```csharp
// Waitlist.Domain.Entities.WaitlistEntry

public class WaitlistEntry
{
    public const string StatusPending   = "pending";
    public const string StatusAssigned  = "assigned";
    public const string StatusExpired   = "expired";
    public const string StatusCompleted = "completed";

    public Guid     Id           { get; private set; }
    public string   Email        { get; private set; }
    public Guid     EventId      { get; private set; }
    public Guid?    SeatId       { get; private set; }  // asignado al crear orden
    public Guid?    OrderId      { get; private set; }  // FK lógico a bc_ordering.Orders
    public string   Status       { get; private set; } = StatusPending;
    public DateTime RegisteredAt { get; private set; }
    public DateTime? AssignedAt  { get; private set; }
    public DateTime? ExpiresAt   { get; private set; }  // AssignedAt + 30 min

    private WaitlistEntry() { }

    public static WaitlistEntry Create(string email, Guid eventId)
    {
        // validaciones guard
        return new WaitlistEntry
        {
            Id           = Guid.NewGuid(),
            Email        = email,
            EventId      = eventId,
            Status       = StatusPending,
            RegisteredAt = DateTime.UtcNow
        };
    }

    public void Assign(Guid seatId, Guid orderId)
    {
        if (Status != StatusPending)
            throw new InvalidOperationException($"Cannot assign entry in status '{Status}'.");
        SeatId     = seatId;
        OrderId    = orderId;
        Status     = StatusAssigned;
        AssignedAt = DateTime.UtcNow;
        ExpiresAt  = AssignedAt.Value.AddMinutes(30);
    }

    public void Complete()
    {
        if (Status != StatusAssigned)
            throw new InvalidOperationException($"Cannot complete entry in status '{Status}'.");
        Status = StatusCompleted;
    }

    public void Expire()
    {
        if (Status != StatusAssigned)
            throw new InvalidOperationException($"Cannot expire entry in status '{Status}'.");
        Status = StatusExpired;
    }

    public bool IsAssignmentExpired() =>
        Status == StatusAssigned && DateTime.UtcNow > ExpiresAt;
}
```

---

## Decisiones Arquitectónicas (ADRs)

### ADR-01: Email como GuestToken (sin Identity Service)

| Campo | Detalle |
|---|---|
| **Contexto** | La compra de tickets no requiere cuenta registrada. El waitlist registra un email de visitante. El Ordering Service necesita un `UserId` o `GuestToken` para crear órdenes. |
| **Decisión** | El Waitlist Service pasa `guestToken = email` al crear la orden. No se llama al Identity Service. |
| **Alternativas descartadas** | Lookup al Identity Service para mapear email → userId: agrega dependencia HTTP frágil y no agrega valor en flujo de invitado. |
| **Consecuencias** | Las órdenes de waitlist son órdenes de invitado. El frontend puede usar el `OrderId` como referencia directa sin sesión. |

---

### ADR-02: Waitlist Service gestiona el timer de 30 minutos (no Ordering)

| Campo | Detalle |
|---|---|
| **Contexto** | Los turnos asignados necesitan un TTL de 30 min. Se evaluó agregar `PaymentDueAt` a `Order` en el Ordering Service vs que el Waitlist lleve el timer. |
| **Decisión** | El Waitlist Service tiene un `WaitlistExpiryWorker` (análogo a `ReservationExpiryWorker`) que ejecuta cada minuto, consulta entradas `Assigned` con `ExpiresAt <= now`, y dispara el flujo de rotación. |
| **Alternativas descartadas** | Agregar `PaymentDueAt` al Ordering Service: requiere migración + nuevo worker + lógica de diferenciación entre órdenes de waitlist y regulares (TR-01). Más blast radius sin más valor. |
| **Consecuencias** | El Ordering Service no necesita cambios de dominio. El Waitlist Service es autónomo en la gestión del TTL. Ordering cancela la orden cuando el Waitlist lo instruye vía HTTP. |

---

### ADR-03: ReservationExpiryWorker consulta cola antes de liberar asiento

| Campo | Detalle |
|---|---|
| **Contexto** | Cuando `ReservationExpiryWorker` detecta una reserva expirada, actualmente libera el asiento (`seat.Release()`) y luego publica `reservation-expired`. Esto crea una ventana donde el asiento queda disponible antes de que el Waitlist Service lo reasigne. |
| **Decisión** | El worker llama al Waitlist Service vía HTTP (`GET /waitlist/has-pending?eventId=X`) antes de ejecutar `seat.Release()`. Si hay cola activa, publica `reservation-expired` SIN hacer `seat.Release()`. El Waitlist Service es responsable de liberar el asiento si la cola queda vacía. |
| **Alternativas descartadas** | Saga orchestrada para transferencia atómica: complejidad excesiva para MVP. Compartir DB entre Inventory y Waitlist: viola bounded context. |
| **Consecuencias** | Introduce una dependencia HTTP síncrona de Inventory → Waitlist. Si el Waitlist Service no responde, el worker usa fallback: libera el asiento (comportamiento previo, sin degradar el sistema). La latencia del worker aumenta en ~50-100ms por la consulta HTTP. |

---

### ADR-04: PostgreSQL para la cola FIFO (no Redis)

| Campo | Detalle |
|---|---|
| **Contexto** | La cola necesita durabilidad, cambios de estado transaccionales, y capacidad de auditoría. |
| **Decisión** | PostgreSQL con índice compuesto `(event_id, status, registered_at ASC)`. La query FIFO es `WHERE status = 'pending' ORDER BY registered_at ASC LIMIT 1`. |
| **Alternativas descartadas** | Redis Sorted Sets: sin garantía ACID, dificulta auditoría, requiere configuración AOF para durabilidad. |
| **Consecuencias** | Latencia de consulta FIFO ~2-5ms en condiciones normales. Soporte completo de transacciones para el cambio de estado `Pending → Assigned`. |

---

### ADR-05: Nuevo endpoint HTTP en Ordering para creación de orden de waitlist

| Campo | Detalle |
|---|---|
| **Contexto** | El Waitlist Service necesita crear una orden de compra en el Ordering Service. El endpoint existente `POST /Orders` requiere flujo de múltiples pasos (AddToCart + Checkout). |
| **Decisión** | Agregar `POST /Orders/waitlist` que recibe `{seatId, price, guestToken, eventId}` y en una sola transacción crea la orden, agrega el ítem y la lleva a estado `pending`. Retorna el `OrderId`. |
| **Alternativas descartadas** | Reutilizar `AddToCart` + `Checkout` en dos llamadas HTTP: más frágil, expone a inconsistencias si la segunda llamada falla. Evento Kafka para crear orden: dificulta obtener el `OrderId` de respuesta síncrona. |
| **Consecuencias** | Mínimo cambio en Ordering: un handler y un endpoint. El endpoint es interno (no expuesto al frontend directamente). |

---

## Diseño de Datos

### Schema `bc_waitlist`

```sql
CREATE TABLE bc_waitlist.waitlist_entries (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email         TEXT        NOT NULL,
    event_id      UUID        NOT NULL,
    seat_id       UUID,                          -- NULL hasta asignación
    order_id      UUID,                          -- NULL hasta asignación
    status        TEXT        NOT NULL DEFAULT 'pending',
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    assigned_at   TIMESTAMPTZ,
    expires_at    TIMESTAMPTZ,                   -- assigned_at + 30 min

    CONSTRAINT uq_active_entry UNIQUE (email, event_id, status)
        -- permite re-registro después de Expired
);

-- Índice FIFO para GetNextInQueue
CREATE INDEX idx_waitlist_fifo
    ON bc_waitlist.waitlist_entries (event_id, status, registered_at ASC)
    WHERE status = 'pending';

-- Índice para el worker de expiración
CREATE INDEX idx_waitlist_expiry
    ON bc_waitlist.waitlist_entries (expires_at)
    WHERE status = 'assigned';
```

> **Nota sobre UNIQUE constraint**: `UNIQUE(email, event_id, status)` permite que un email tenga múltiples filas para el mismo evento siempre que difieran en status (e.g., una `expired` y una nueva `pending`). La regla RN-03 (no duplicados activos) se refuerza en la capa de aplicación verificando que no exista una fila `pending` o `assigned` para el mismo `email + event_id`.

### Cambio a `bc_inventory.Reservations`

```sql
-- Migración EF Core requerida en Inventory Service
ALTER TABLE bc_inventory.reservations
    ADD COLUMN event_id UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

-- Poblar datos existentes desde bc_catalog.seats (one-time migration)
UPDATE bc_inventory.reservations r
SET event_id = cs.event_id
FROM bc_catalog.seats cs
WHERE cs.id = r.seat_id;
```

> **Nota**: El `DEFAULT '00000000-...'` es solo para la migración. Los nuevos registros siempre tendrán un `EventId` real desde `CreateReservationCommand`.

---

## Diseño de Eventos Kafka

### `reservation-expired` — Payload v2 (enriquecido con `eventId` del concierto)

```json
{
  "messageId":     "string (uuid) — ID del mensaje Kafka para idempotencia",
  "reservationId": "string (uuid)",
  "seatId":        "string (uuid)",
  "customerId":    "string",
  "concertEventId":"string (uuid) — ID del evento/concierto al que pertenece el asiento"
}
```

> **Cambio de naming**: El campo anterior `eventId` era el UUID de idempotencia del mensaje. Se renombra a `messageId` y se agrega `concertEventId` para el ID del concierto. Todos los consumidores existentes (`Ordering.ReservationEventConsumer`, `Payment.ReservationEventConsumer`) solo leen `reservationId` y `seatId` — no son afectados por los campos nuevos.

### `payment-succeeded` — Sin cambios

El Waitlist Service consume este evento para detectar pagos exitosos y marcar la entrada como `Completed`.

```json
{
  "paymentId":     "string (uuid)",
  "orderId":       "string (uuid)",
  "customerId":    "string",
  "reservationId": "string (uuid)",
  ...
}
```

### Nuevos tópicos Kafka

Ninguno. El Waitlist Service es solo consumidor de topics existentes. El timer de 30 min se gestiona internamente sin Kafka.

---

## Flujos Técnicos Detallados

### Flujo A: Registro en Lista de Espera

```
POST /api/v1/waitlist/join
  { email: "jostin@example.com", eventId: "uuid" }
          │
          ▼
[JoinWaitlistHandler]
  1. Validar formato email (FluentValidation)
  2. HTTP GET bc_catalog → GET /events/{eventId}/availability
     ├── stock > 0 → 409 "Hay tickets disponibles"
     └── stock = 0 → continuar
  3. Consultar bc_waitlist:
     EXISTS (email, eventId) WHERE status IN ('pending','assigned')
     ├── EXISTS → 409 "Ya estás en la lista"
     └── NOT EXISTS → continuar
  4. WaitlistEntry.Create(email, eventId)
  5. IWaitlistRepository.AddAsync(entry)
  6. Notification → correo "Estás en posición X"
  7. Return 201 { position: X, entryId: ... }
```

### Flujo B: Asignación Automática (consumer de `reservation-expired`)

```
Kafka: reservation-expired
  { messageId, reservationId, seatId, customerId, concertEventId }
          │
          ▼
[WaitlistReservationExpiredConsumer]
  1. Idempotencia: ¿ya existe entrada Assigned para este seatId? → skip
  2. IWaitlistRepository.GetNextPending(concertEventId)
     ├── NULL → no hay cola → NO ACCIÓN (Inventory ya liberó o liberará el asiento)
     └── WaitlistEntry (pending) → continuar
  3. HTTP POST bc_ordering → POST /orders/waitlist
     { seatId, price, guestToken: entry.Email, eventId: concertEventId }
     ← { orderId }
  4. entry.Assign(seatId, orderId)
  5. IWaitlistRepository.UpdateAsync(entry)
  6. Notification → correo "Tienes 30 min para pagar" + link orderId
  7. Log: asiento {seatId} asignado a {email}

[ReservationExpiryWorker — Inventory Service] (MODIFICADO)
  ANTES de seat.Release():
    HTTP GET bc_waitlist → GET /waitlist/has-pending?eventId={concertEventId}
    ├── has_pending = true  → publicar reservation-expired SIN seat.Release()
    │                         (Waitlist gestiona el asiento)
    └── has_pending = false → seat.Release() + publicar reservation-expired
                              (comportamiento original)
```

### Flujo C: Rotación por Inacción (WaitlistExpiryWorker)

```
[WaitlistExpiryWorker] — ejecuta cada 60 segundos
  1. Consultar bc_waitlist:
     SELECT * FROM waitlist_entries
     WHERE status = 'assigned' AND expires_at <= NOW()
  2. Para cada entrada expirada:
     a. entry.Expire()
     b. IWaitlistRepository.UpdateAsync(entry)
     c. Notification → correo "Tu turno expiró"
     d. IWaitlistRepository.GetNextPending(entry.EventId)
        ├── SIGUIENTE EXISTE:
        │     i.  HTTP POST bc_ordering → POST /orders/waitlist
        │         { seatId: entry.SeatId, price, guestToken: siguiente.Email }
        │         ← { orderId }
        │    ii.  siguiente.Assign(entry.SeatId.Value, orderId)
        │   iii.  IWaitlistRepository.UpdateAsync(siguiente)
        │    iv.  Notification → correo nuevo usuario "Tienes 30 min"
        └── COLA VACÍA:
              i.  HTTP PUT bc_inventory → PUT /seats/{seatId}/release
              ii. HTTP DELETE/CANCEL bc_ordering → PATCH /orders/{orderId}/cancel
             iii. Log: asiento liberado al pool general
```

### Flujo D: Pago exitoso (consumer de `payment-succeeded`)

```
Kafka: payment-succeeded
  { paymentId, orderId, reservationId, ... }
          │
          ▼
[WaitlistPaymentSucceededConsumer]
  1. IWaitlistRepository.GetByOrderId(orderId)
     ├── NULL → no es una orden de waitlist → skip
     └── WaitlistEntry → continuar
  2. entry.Complete()
  3. IWaitlistRepository.UpdateAsync(entry)
  4. Log: waitlist entry {entryId} completada
```

---

## Cambios a Servicios Existentes

### Inventory Service (cambios requeridos)

| Cambio | Archivo | Descripción |
|---|---|---|
| Agregar `EventId` a `Reservation` entity | `Inventory.Domain/Entities/Reservation.cs` | Campo `public Guid EventId { get; private set; }` |
| Actualizar `CreateReservationCommand` | `Inventory.Application/UseCases/CreateReservation/` | Agregar `EventId` al command + handler |
| Enriquecer payload `reservation-expired` | `Inventory.Infrastructure/Workers/ReservationExpiryWorker.cs` | Renombrar `eventId` → `messageId`; agregar `concertEventId = res.EventId` |
| Consultar cola antes de liberar | `ReservationExpiryWorker.cs` | HTTP GET `bc_waitlist/waitlist/has-pending?eventId=X` con fallback |
| Nuevo endpoint `GET /waitlist/has-pending` | No en Inventory — es un endpoint **del Waitlist Service** | |
| Migración EF Core | `Inventory.Infrastructure/Migrations/` | `AddColumn EventId` en `bc_inventory.reservations` |

### Ordering Service (cambios requeridos)

| Cambio | Archivo | Descripción |
|---|---|---|
| Nuevo command `CreateWaitlistOrderCommand` | `Ordering.Application/UseCases/CreateWaitlistOrder/` | `{ SeatId, Price, GuestToken(email), EventId }` → crea orden + AddItem + Checkout en una transacción |
| Nuevo endpoint `POST /orders/waitlist` | `Ordering.Api/Controllers/OrdersController.cs` | Endpoint interno para el Waitlist Service; retorna `{ orderId }` |
| Endpoint `PATCH /orders/{id}/cancel` | Verificar si ya existe | Si no existe, agregar para el flujo de cola vacía |

### Notification Service

Sin cambios estructurales. Los nuevos correos usan el patrón existente (`IEmailService.SendAsync`). Se agrega configuración de templates de correo en `appsettings.json`.

### Catalog Service

Sin cambios de código. El Waitlist Service llama `GET /events/{eventId}/availability` — este endpoint ya existe en `EventsController`.

---

## Estructura del Nuevo Microservicio

```
services/waitlist/
├── src/
│   ├── Waitlist.Domain/
│   │   ├── Entities/
│   │   │   └── WaitlistEntry.cs
│   │   └── Waitlist.Domain.csproj
│   │
│   ├── Waitlist.Application/
│   │   ├── Ports/
│   │   │   ├── IWaitlistRepository.cs
│   │   │   ├── IOrderingClient.cs        ← puerto HTTP para Ordering
│   │   │   ├── IInventoryClient.cs       ← puerto HTTP para liberar asiento
│   │   │   └── ICatalogClient.cs         ← puerto HTTP para verificar stock
│   │   ├── UseCases/
│   │   │   ├── JoinWaitlist/
│   │   │   │   ├── JoinWaitlistCommand.cs
│   │   │   │   └── JoinWaitlistHandler.cs
│   │   │   ├── AssignNextInQueue/
│   │   │   │   ├── AssignNextCommand.cs
│   │   │   │   └── AssignNextHandler.cs
│   │   │   └── CompleteAssignment/
│   │   │       ├── CompleteAssignmentCommand.cs
│   │   │       └── CompleteAssignmentHandler.cs
│   │   └── Waitlist.Application.csproj
│   │
│   ├── Waitlist.Infrastructure/
│   │   ├── Consumers/
│   │   │   ├── ReservationExpiredConsumer.cs  ← Kafka consumer
│   │   │   └── PaymentSucceededConsumer.cs    ← Kafka consumer
│   │   ├── Clients/
│   │   │   ├── OrderingHttpClient.cs          ← IOrderingClient impl
│   │   │   ├── InventoryHttpClient.cs         ← IInventoryClient impl
│   │   │   └── CatalogHttpClient.cs           ← ICatalogClient impl
│   │   ├── Persistence/
│   │   │   ├── WaitlistDbContext.cs
│   │   │   ├── WaitlistRepository.cs
│   │   │   └── Migrations/
│   │   ├── Workers/
│   │   │   └── WaitlistExpiryWorker.cs        ← Timer de 30 min
│   │   ├── ServiceCollectionExtensions.cs
│   │   └── Waitlist.Infrastructure.csproj
│   │
│   └── Waitlist.Api/
│       ├── Controllers/
│       │   └── WaitlistController.cs
│       │       ├── POST /join
│       │       └── GET  /has-pending?eventId={id}   ← consultado por Inventory
│       ├── Program.cs
│       └── Waitlist.Api.csproj
│
└── tests/
    ├── unit/Waitlist.UnitTests/
    └── integration/Waitlist.IntegrationTests/
```

### Docker Compose entry

```yaml
# docker-compose.yml — agregar:
waitlist-service:
  build:
    context: ./services/waitlist/src/Waitlist.Api
  ports:
    - "5006:5006"
  environment:
    - ConnectionStrings__Default=Host=postgres;Database=ticketing;Username=admin;Password=admin
    - Kafka__BootstrapServers=kafka:9092
    - Services__OrderingUrl=http://ordering-service:5002
    - Services__InventoryUrl=http://inventory-service:5003
    - Services__CatalogUrl=http://catalog-service:5001
  depends_on:
    - postgres
    - kafka
```

---

## Plan de Pruebas Técnico

| ID | Tipo | Handler/Consumer | Escenario | Resultado esperado |
|---|---|---|---|---|
| TU-01 | Unit | `JoinWaitlistHandler` | Email válido + stock = 0 | Entry `Pending` creada, retorna posición |
| TU-02 | Unit | `JoinWaitlistHandler` | Stock > 0 (mock `ICatalogClient`) | Lanza excepción de negocio 409 |
| TU-03 | Unit | `JoinWaitlistHandler` | Email duplicado activo | Lanza excepción 409 |
| TU-04 | Unit | `WaitlistEntry.Assign()` | Entry en `Pending` | Status = `Assigned`, `AssignedAt` y `ExpiresAt` set |
| TU-05 | Unit | `WaitlistEntry.Expire()` | Entry en `Assigned` | Status = `Expired` |
| TU-06 | Unit | `WaitlistEntry.Complete()` | Entry en `Assigned` | Status = `Completed` |
| TU-07 | Unit | `WaitlistEntry.Assign()` | Entry NO en `Pending` | `InvalidOperationException` |
| TU-08 | Unit | `AssignNextHandler` | Cola con `Pending`; mock `IOrderingClient` retorna `orderId` | Entry pasa a `Assigned`, correo enviado |
| TU-09 | Unit | `AssignNextHandler` | Cola vacía | No acción, retorna sin error |
| TU-10 | Unit | `WaitlistExpiryWorker` | Entry con `ExpiresAt <= now`, siguiente en cola | Entry `Expired`, siguiente `Assigned` |
| TU-11 | Unit | `WaitlistExpiryWorker` | Entry expirada, cola vacía | Entry `Expired`, asiento liberado via `IInventoryClient` |
| TI-01 | Integration | `POST /waitlist/join` | Email válido + stock = 0 (DB real) | `201`, entry en `bc_waitlist` |
| TI-02 | Integration | Consumer `reservation-expired` | Evento Kafka + cola con `Pending` | Orden creada en Ordering, entry `Assigned` |
| TI-03 | Integration | Consumer `reservation-expired` | Cola vacía | Sin cambios en waitlist |
| TI-04 | Integration | `WaitlistExpiryWorker` | Entry expirada + siguiente en cola | Rotación correcta |
| TR-01 | Regresión | Ordering Service | Creación de orden directa (no-waitlist) | No afectada; `PATCH /orders/waitlist` no se dispara |
| TR-02 | Regresión | Inventory `ReservationExpiryWorker` | Expiración sin cola activa | Comportamiento previo inalterado; asiento liberado |

---

## Riesgos e Impactos

| Riesgo | Probabilidad | Impacto | Mitigación |
|---|---|---|---|
| Waitlist Service caído cuando Inventory consulta `has-pending` | Media | Alto | Fallback en `ReservationExpiryWorker`: si HTTP falla, libera asiento (comportamiento anterior) |
| Email de bots llena la cola | Alta | Medio | Rate limiting por IP en `POST /waitlist/join`; validación de formato + `unique(email, eventId, pending)` |
| `reservation-expired` llega dos veces (at-least-once) | Alta | Bajo | Guard de idempotencia: verificar si ya existe `Assigned` para `seatId` antes de asignar |
| HTTP a Ordering falla al crear orden de waitlist | Media | Alto | Si falla, no se hace `entry.Assign()` (transacción local). El worker reintentará en el próximo ciclo si la entry sigue `Pending`. |
| `payment-succeeded` llega antes que la entry esté `Assigned` | Baja | Bajo | Si `GetByOrderId` retorna null, se ignora el evento (idempotente) |

### Impacto en servicios existentes

| Servicio | Cambios | Riesgo de regresión |
|---|---|---|
| Inventory | `EventId` en `Reservation` + consulta HTTP antes de `seat.Release()` | Bajo — fallback protege el comportamiento original |
| Ordering | Nuevo endpoint `POST /orders/waitlist` + nuevo handler | Bajo — aditivo, no modifica lógica existente |
| Catalog | Sin cambios de código | Ninguno |
| Notification | Sin cambios estructurales | Ninguno |
| Payment | Sin cambios | Ninguno |

---

## Estimación de Esfuerzo

| Historia | Componentes | Talla | Puntos Fibonacci |
|---|---|---|---|
| HU-01: Registro | WaitlistEntry domain, JoinWaitlistHandler, API endpoint, migration | M | 5 |
| HU-02: Asignación automática | ReservationExpiredConsumer, AssignNextHandler, IOrderingClient, cambios Inventory | L | 13 |
| HU-03: Rotación | WaitlistExpiryWorker, PaymentSucceededConsumer, IInventoryClient | XL | 21 |
| Infraestructura transversal | Docker Compose, CI/CD, proyecto .NET, DI wiring | M | 5 |
| **Total** | | | **44** |

---

## Project Structure (this feature)

```
specs/004-waitlist-autoassign/
├── plan.md         ← Este archivo
├── spec.md         ← Especificación de negocio
├── research.md     ← Investigación de decisiones
├── data-model.md   ← Modelo de datos detallado
├── contracts/      ← Schemas de eventos y endpoints HTTP
│   ├── reservation-expired-v3.json
│   ├── waitlist-order-request.json
│   └── has-pending-response.json
└── tasks.md        ← Tareas de implementación (generado por /speckit.tasks)
```
