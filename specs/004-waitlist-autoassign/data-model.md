# Data Model: Waitlist Autoassign

**Feature Branch**: `004-waitlist-autoassign`
**Date**: 2026-04-02
**Status**: Final

---

## Entidades del Dominio

### WaitlistEntry (nuevo — `bc_waitlist`)

Entidad central del Waitlist Service. Representa un email registrado en la cola de espera de un evento.

```
WaitlistEntry
├── Id            : Guid       (PK, generado al crear)
├── Email         : string     (email del visitante, como GuestToken)
├── EventId       : Guid       (ID del concierto — FK lógico a bc_catalog.events)
├── SeatId        : Guid?      (NULL hasta asignación; asiento concreto asignado)
├── OrderId       : Guid?      (NULL hasta asignación; FK lógico a bc_ordering.orders)
├── Status        : string     (pending | assigned | expired | completed)
├── RegisteredAt  : DateTime   (UTC; base del orden FIFO)
├── AssignedAt    : DateTime?  (NULL mientras pending)
└── ExpiresAt     : DateTime?  (AssignedAt + 30 min; NULL mientras pending)
```

#### Ciclo de vida

```
              POST /waitlist/join
                     │
                     ▼
              ┌─────────────┐
              │   Pending   │ ◄── RegisteredAt determina posición FIFO
              └──────┬──────┘
                     │ reservation-expired consumido
                     │ (primer en cola para ese EventId)
                     ▼
              ┌─────────────┐
              │  Assigned   │ ◄── SeatId, OrderId, AssignedAt, ExpiresAt set
              └──────┬──────┘
                     │
            ┌────────┴────────┐
            │                 │
   payment-succeeded      ExpiresAt <= now
   (antes de ExpiresAt)   (WaitlistExpiryWorker)
            │                 │
            ▼                 ▼
     ┌───────────┐     ┌────────────┐
     │ Completed │     │  Expired   │
     └───────────┘     └────────────┘
```

**Transiciones permitidas**:

| Desde → | Hacia | Disparador | Método dominio |
|---|---|---|---|
| `Pending` | `Assigned` | `ReservationExpiredConsumer` / `WaitlistExpiryWorker` | `entry.Assign(seatId, orderId)` |
| `Assigned` | `Completed` | `PaymentSucceededConsumer` | `entry.Complete()` |
| `Assigned` | `Expired` | `WaitlistExpiryWorker` (ExpiresAt <= now) | `entry.Expire()` |
| `Expired` | `Pending` | Nueva llamada `POST /waitlist/join` | `WaitlistEntry.Create(...)` (nueva fila) |

**Transiciones prohibidas** (guard en dominio):
- `Pending → Completed` (sin asignación previa)
- `Pending → Expired`
- `Completed → cualquier estado`
- `Expired → Assigned` (no se reanuda; se crea nueva entrada)

---

### Reservation (modificado — `bc_inventory`)

Se agrega `EventId` para que `ReservationExpiryWorker` incluya el `concertEventId` en el payload del evento.

```
Reservation (ANTES)                 Reservation (DESPUÉS)
├── Id            : Guid            ├── Id            : Guid
├── SeatId        : Guid            ├── SeatId        : Guid
├── CustomerId    : string          ├── CustomerId    : string
├── CreatedAt     : DateTime        ├── CreatedAt     : DateTime
├── ExpiresAt     : DateTime        ├── ExpiresAt     : DateTime
└── Status        : string          ├── Status        : string
                                    └── EventId       : Guid    ← NUEVO
```

**Migración requerida**:

```sql
ALTER TABLE bc_inventory.reservations
    ADD COLUMN event_id UUID NOT NULL DEFAULT '00000000-0000-0000-0000-000000000000';

-- Poblar registros existentes desde bc_catalog.seats
UPDATE bc_inventory.reservations r
SET event_id = cs.event_id
FROM bc_catalog.seats cs
WHERE cs.id = r.seat_id;
```

---

## Schema de Base de Datos

### `bc_waitlist.waitlist_entries`

```sql
CREATE SCHEMA IF NOT EXISTS bc_waitlist;

CREATE TABLE bc_waitlist.waitlist_entries (
    id            UUID        PRIMARY KEY DEFAULT gen_random_uuid(),
    email         TEXT        NOT NULL,
    event_id      UUID        NOT NULL,
    seat_id       UUID,
    order_id      UUID,
    status        TEXT        NOT NULL DEFAULT 'pending'
                              CHECK (status IN ('pending','assigned','expired','completed')),
    registered_at TIMESTAMPTZ NOT NULL DEFAULT NOW(),
    assigned_at   TIMESTAMPTZ,
    expires_at    TIMESTAMPTZ,

    -- Previene registros duplicados activos para el mismo email+evento
    CONSTRAINT uq_active_entry UNIQUE (email, event_id, status)
);
```

**Índices**:

```sql
-- Consulta FIFO: GetNextPending(eventId)
-- SELECT * FROM waitlist_entries WHERE event_id = X AND status = 'pending'
-- ORDER BY registered_at ASC LIMIT 1
CREATE INDEX idx_waitlist_fifo
    ON bc_waitlist.waitlist_entries (event_id, status, registered_at ASC)
    WHERE status = 'pending';

-- Consulta del worker de expiración:
-- SELECT * FROM waitlist_entries WHERE status = 'assigned' AND expires_at <= NOW()
CREATE INDEX idx_waitlist_expiry
    ON bc_waitlist.waitlist_entries (expires_at)
    WHERE status = 'assigned';

-- Consulta por orderId (PaymentSucceededConsumer):
-- SELECT * FROM waitlist_entries WHERE order_id = X
CREATE INDEX idx_waitlist_order
    ON bc_waitlist.waitlist_entries (order_id)
    WHERE order_id IS NOT NULL;
```

**Nota sobre `UNIQUE(email, event_id, status)`**: PostgreSQL trata múltiples filas con el mismo valor en una columna `UNIQUE` que incluye valores `NULL` de forma especial. Para los estados `pending` y `assigned` la restricción previene duplicados activos. Las filas `expired` y `completed` no colisionan con nuevas `pending` porque difieren en `status`. La validación de negocio (capa de aplicación) verifica adicionalmente la existencia de `pending` o `assigned` antes de insertar.

---

## Comparación de Bounded Contexts

### Modelo de Seat en cada contexto

```
bc_catalog.seats              bc_inventory.seats
┌──────────────────┐          ┌──────────────────┐
│ id       : UUID  │          │ id       : UUID  │
│ event_id : UUID  │ ←── (no FK) ── sin event_id │
│ section  : TEXT  │          │ reserved : BOOL  │
│ row      : TEXT  │          └──────────────────┘
│ number   : TEXT  │
│ price    : DECIMAL│
│ status   : TEXT  │
│  (available/     │
│   reserved/sold) │
└──────────────────┘
```

El Waitlist Service es el único bounded context que necesita el `EventId` para agrupar la cola. Lo obtiene:
- De `bc_waitlist.waitlist_entries.event_id` (pasado por el usuario al registrarse)
- De `reservation-expired.concertEventId` (enriquecido por Inventory al expirar)

---

## Relaciones entre Bounded Contexts (lógicas, no FK físicas)

```
bc_waitlist.waitlist_entries.event_id ──── (referencia lógica) ──→ bc_catalog.events.id
bc_waitlist.waitlist_entries.seat_id  ──── (referencia lógica) ──→ bc_inventory.seats.id
bc_waitlist.waitlist_entries.order_id ──── (referencia lógica) ──→ bc_ordering.orders.id
```

No hay claves foráneas entre schemas. La consistencia eventual se garantiza via eventos Kafka y guards de idempotencia.

---

## Estado de eventos Kafka relevantes

### `reservation-expired` — Payload v2 → v3

```
ANTES (v2):                           DESPUÉS (v3):
{                                     {
  "eventId":       "uuid-msg",          "messageId":      "uuid-msg",    ← renombrado
  "reservationId": "uuid",              "reservationId":  "uuid",
  "seatId":        "uuid",              "seatId":         "uuid",
  "customerId":    "string"             "customerId":     "string",
}                                       "concertEventId": "uuid"         ← NUEVO
                                      }
```

**Consumidores afectados por el cambio**:

| Consumidor | Lee `eventId`/`messageId`? | Lee `concertEventId`? | Impacto |
|---|---|---|---|
| `Ordering.ReservationEventConsumer` | No | No | Ninguno |
| `Payment.ReservationEventConsumer` | No | No | Ninguno |
| `Notification.ReservationExpiredConsumer` | No | No | Ninguno |
| `Waitlist.ReservationExpiredConsumer` | Sí (`messageId` para idempotencia) | Sí (para lookup FIFO) | Requiere el nuevo campo |

### `payment-succeeded` — Sin cambios

```json
{
  "paymentId":     "uuid",
  "orderId":       "uuid",
  "customerId":    "string",
  "reservationId": "uuid",
  "amount":        "decimal"
}
```

El Waitlist Service usa `orderId` para localizar la entrada vía `GetByOrderId`.

---

## EF Core — Configuración del DbContext

### `WaitlistDbContext`

```csharp
// Waitlist.Infrastructure/Persistence/WaitlistDbContext.cs
public class WaitlistDbContext : DbContext
{
    public DbSet<WaitlistEntry> WaitlistEntries => Set<WaitlistEntry>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("bc_waitlist");

        modelBuilder.Entity<WaitlistEntry>(e =>
        {
            e.ToTable("waitlist_entries");
            e.HasKey(x => x.Id);
            e.Property(x => x.Email).IsRequired().HasMaxLength(320);
            e.Property(x => x.Status).IsRequired().HasMaxLength(20);
            e.HasIndex(x => new { x.EventId, x.Status, x.RegisteredAt });
            e.HasIndex(x => x.ExpiresAt).HasFilter("status = 'assigned'");
            e.HasIndex(x => x.OrderId).HasFilter("order_id IS NOT NULL");
        });
    }
}
```

### Migración EF Core requerida en Inventory Service

```csharp
// Inventory.Infrastructure/Migrations/AddEventIdToReservation.cs
public partial class AddEventIdToReservation : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "event_id",
            schema: "bc_inventory",
            table: "reservations",
            nullable: false,
            defaultValue: Guid.Empty);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropColumn(
            name: "event_id",
            schema: "bc_inventory",
            table: "reservations");
    }
}
```
