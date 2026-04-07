# 04 — Sequence Diagrams

> **Fase SDLC:** Diseño
> **Audiencia:** Dev, QA
> **Propósito:** Visualizar el flujo de mensajes y responsabilidades entre componentes para cada escenario principal

---

## Flujo 1 — Registro en Lista de Espera (HU-01)

**Trigger:** Usuario hace POST /api/v1/waitlist/join
**Participantes:** Frontend → Controller → Handler → CatalogClient → Repository

```mermaid
sequenceDiagram
    autonumber
    actor Usuario
    participant FE as Frontend
    participant WC as WaitlistController
    participant JH as JoinWaitlistHandler
    participant CAT as CatalogHttpClient
    participant REPO as WaitlistRepository
    participant DB as PostgreSQL (bc_waitlist)

    Usuario->>FE: Click "Unirse a Lista de Espera"
    FE->>WC: POST /api/v1/waitlist/join\n{ email, eventId }

    WC->>WC: FluentValidation\n(email format, eventId not empty)
    alt Request inválido
        WC-->>FE: 400 Bad Request { errors[] }
    end

    WC->>JH: Send(JoinWaitlistCommand)

    JH->>CAT: GetAvailableCountAsync(eventId)
    CAT->>CAT: GET /events/{eventId}/seatmap\n(Catalog Service)
    alt Catalog no disponible
        CAT-->>JH: throws HttpRequestException
        JH-->>WC: throws WaitlistServiceUnavailableException
        WC-->>FE: 503 Service Unavailable
    end
    CAT-->>JH: availableCount (int)

    alt availableCount > 0
        JH-->>WC: throws WaitlistConflictException\n"Hay tickets disponibles"
        WC-->>FE: 409 Conflict
    end

    JH->>REPO: HasActiveEntryAsync(email, eventId)
    REPO->>DB: SELECT WHERE Email=? AND EventId=?\nAND Status IN ('pending','assigned')
    DB-->>REPO: bool
    alt Ya existe entrada activa
        JH-->>WC: throws WaitlistConflictException\n"Ya estás en la lista"
        WC-->>FE: 409 Conflict
    end

    JH->>JH: WaitlistEntry.Create(email, eventId)\nStatus=pending, RegisteredAt=now
    JH->>REPO: AddAsync(entry)
    REPO->>DB: INSERT INTO waitlist_entries
    DB-->>REPO: OK

    JH->>REPO: GetQueuePositionAsync(eventId)
    REPO->>DB: SELECT COUNT(*) WHERE EventId=?\nAND Status='pending'
    DB-->>REPO: position (int)

    JH-->>WC: JoinWaitlistResult { entryId, position }
    WC-->>FE: 201 Created { entryId, position }
    FE-->>Usuario: "Estás en la posición #N de la lista"
```

---

## Flujo 2 — Asignación Automática (HU-02)

**Trigger:** Evento `reservation-expired` llega desde Kafka (publicado por Inventory)
**Participantes:** Kafka → Consumer → Handler → Repository → OrderingClient → EmailService

```mermaid
sequenceDiagram
    autonumber
    participant INV as Inventory Service
    participant K as Kafka\n(reservation-expired)
    participant REC as ReservationExpiredConsumer
    participant ANH as AssignNextHandler
    participant REPO as WaitlistRepository
    participant DB as PostgreSQL (bc_waitlist)
    participant ORD as OrderingHttpClient
    participant EMAIL as EmailService

    INV->>K: Publish reservation-expired\n{ seatId, concertEventId, ... }

    K->>REC: Deliver message (at-least-once)
    REC->>REC: Deserialize ReservationExpiredEventV3
    REC->>REC: Guard: concertEventId != empty?\nGuard: seatId != empty?

    alt Payload inválido (v2 sin concertEventId)
        REC->>REC: Log warning, skip message
        REC->>K: Manual commit (skip)
    end

    REC->>ANH: Send(AssignNextCommand\n{ seatId, concertEventId })

    ANH->>REPO: HasAssignedEntryForSeatAsync(seatId)
    REPO->>DB: SELECT WHERE SeatId=? AND Status='assigned'
    DB-->>REPO: bool

    alt Asiento ya asignado (idempotencia)
        ANH->>ANH: Log "seat already assigned, skip"
        ANH-->>REC: return (no-op)
    end

    ANH->>REPO: GetNextPendingAsync(eventId)
    REPO->>DB: SELECT TOP 1 WHERE EventId=? AND Status='pending'\nORDER BY RegisteredAt ASC
    DB-->>REPO: WaitlistEntry? (next)

    alt Cola vacía
        ANH->>ANH: Log "no pending entries, skip"
        ANH-->>REC: return (no-op)
    end

    ANH->>ORD: CreateWaitlistOrderAsync\n(seatId, price=0, guestToken=email, eventId)
    ORD->>ORD: POST /orders/waitlist (Ordering Service)
    ORD-->>ANH: orderId (Guid)

    ANH->>ANH: next.Assign(seatId, orderId)\nStatus=assigned\nExpiresAt=now+30min

    ANH->>REPO: UpdateAsync(next)
    REPO->>DB: UPDATE waitlist_entries SET Status='assigned',\nSeatId=?, OrderId=?, AssignedAt=?, ExpiresAt=?

    ANH->>EMAIL: SendAsync(next.Email,\n"Tienes un asiento disponible",\n"Tienes 30 minutos para completar el pago")
    EMAIL-->>ANH: true (sent or logged in dev mode)

    ANH-->>REC: return
    REC->>K: Manual commit (message processed)
```

---

## Flujo 3 — Rotación de Asignación (HU-03)

**Trigger:** `WaitlistExpiryWorker` corre cada 10 segundos y encuentra entradas con `ExpiresAt < NOW()`
**Dos caminos:** A) Hay siguiente en cola → rotación directa | B) Cola vacía → liberar asiento

```mermaid
sequenceDiagram
    autonumber
    participant WEW as WaitlistExpiryWorker\n(cada 10 segundos)
    participant REPO as WaitlistRepository
    participant DB as PostgreSQL
    participant ORD as OrderingHttpClient
    participant INV as InventoryHttpClient
    participant EMAIL as EmailService

    loop Cada 10 segundos
        WEW->>REPO: GetExpiredAssignedAsync()
        REPO->>DB: SELECT WHERE Status='assigned'\nAND ExpiresAt <= NOW()\n[usa idx_waitlist_expiry]
        DB-->>REPO: List<WaitlistEntry> expired

        alt Sin entradas expiradas
            WEW->>WEW: await Task.Delay(10s), continuar
        end

        loop Para cada entrada expirada
            WEW->>WEW: entry.Expire() → Status='expired'
            WEW->>REPO: UpdateAsync(entry)
            REPO->>DB: UPDATE SET Status='expired'

            WEW->>EMAIL: SendAsync(entry.Email,\n"Tu turno ha expirado",\n"...")

            WEW->>REPO: GetNextPendingAsync(entry.EventId)
            REPO->>DB: SELECT TOP 1 WHERE EventId=?\nAND Status='pending'\nORDER BY RegisteredAt ASC
            DB-->>REPO: WaitlistEntry? next

            alt CASO A — Hay siguiente en cola (Rotación directa)
                WEW->>ORD: CancelOrderAsync(entry.OrderId)
                ORD->>ORD: PATCH /orders/{id}/cancel

                WEW->>ORD: CreateWaitlistOrderAsync\n(entry.SeatId, 0, next.Email, eventId)
                ORD-->>WEW: newOrderId

                WEW->>WEW: next.Assign(entry.SeatId, newOrderId)\nStatus='assigned'\nExpiresAt=now+30min

                WEW->>REPO: UpdateAsync(next)
                REPO->>DB: UPDATE waitlist_entries

                WEW->>EMAIL: SendAsync(next.Email,\n"Tienes un asiento disponible",\n"30 minutos para pagar")

                note over WEW,INV: El asiento NUNCA pasa por Status='Available'\nSe transfiere directo entre usuarios
            else CASO B — Cola vacía (Liberar al inventario)
                WEW->>INV: ReleaseSeatAsync(entry.SeatId)
                INV->>INV: PUT /api/v1/seats/{id}/release\n(Inventory Service)

                WEW->>ORD: CancelOrderAsync(entry.OrderId)
                ORD->>ORD: PATCH /orders/{id}/cancel

                note over WEW,INV: El asiento vuelve a Status='Available'\nen el inventario general
            end
        end

        WEW->>WEW: await Task.Delay(10s)
    end
```

---

## Flujo 4 — Completar Asignación por Pago (HU-02 / HU-03)

**Trigger:** Evento `payment-succeeded` llega desde Kafka (publicado por Payment Service)
**Propósito:** Cerrar el ciclo de vida de la entrada cuando el usuario de la waitlist completa el pago

```mermaid
sequenceDiagram
    autonumber
    participant PAY as Payment Service
    participant K as Kafka\n(payment-succeeded)
    participant PSC as PaymentSucceededConsumer
    participant CAH as CompleteAssignmentHandler
    participant REPO as WaitlistRepository
    participant DB as PostgreSQL

    PAY->>K: Publish payment-succeeded\n{ orderId, customerId, ... }

    K->>PSC: Deliver message
    PSC->>PSC: Deserialize PaymentSucceededEvent
    PSC->>PSC: Guard: orderId != empty

    PSC->>CAH: Send(CompleteAssignmentCommand { orderId })

    CAH->>REPO: GetByOrderIdAsync(orderId)
    REPO->>DB: SELECT WHERE OrderId=?\n[usa idx_waitlist_order]
    DB-->>REPO: WaitlistEntry?

    alt entry == null (orden no pertenece a waitlist)
        CAH->>CAH: Log "not a waitlist order, skip"
        CAH-->>PSC: return (no-op, idempotente)
    end

    CAH->>CAH: entry.Complete()\nStatus='completed'

    CAH->>REPO: UpdateAsync(entry)
    REPO->>DB: UPDATE SET Status='completed'

    CAH-->>PSC: return
    PSC->>K: Manual commit

    note over CAH,DB: El ciclo de vida de la entrada\nqueda cerrado permanentemente
```

---

## Flujo 5 — ADR-03: Inventory consulta Waitlist antes de liberar asiento

**Trigger:** `ReservationExpiryWorker` en Inventory detecta reserva expirada
**Propósito:** Decidir si retener el asiento para Waitlist o liberarlo al inventario disponible

```mermaid
sequenceDiagram
    autonumber
    participant IREW as Inventory\nReservationExpiryWorker
    participant WC as WaitlistController\nGET /has-pending
    participant REPO as WaitlistRepository
    participant K as Kafka\n(reservation-expired)

    IREW->>IREW: Detecta Reservation con ExpiresAt < NOW()

    IREW->>WC: GET /api/v1/waitlist/has-pending?eventId={id}\n[timeout: 200ms]

    alt Waitlist responde a tiempo
        WC->>REPO: GetNextPendingAsync(eventId)\n(o GetQueuePositionAsync)
        REPO-->>WC: count
        WC-->>IREW: 200 { hasPending: true/false, pendingCount: N }

        alt hasPending == true
            IREW->>IREW: No liberar al inventario disponible\nWaitlist tomará control via Kafka
        else hasPending == false
            IREW->>IREW: Liberar asiento al inventario disponible\nSeat.Status = Available
        end
    else Timeout (> 200ms) o error
        IREW->>IREW: Liberar asiento al inventario disponible\n(degradación controlada)
    end

    IREW->>K: Publish reservation-expired\n{ seatId, concertEventId, ... }
    note over IREW,K: El evento se publica SIEMPRE\nWaitlist decide si hay pending o no
```

**Nota importante:** Inventory publica `reservation-expired` independientemente del resultado de `has-pending`. Waitlist recibe el evento y si no hay pendientes, simplemente no hace nada. La consulta HTTP es una optimización de UX (retener el asiento), no un control de flujo crítico.
