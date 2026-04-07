# Feature: Sistema de Lista de Espera Inteligente

**Autor:** Jostin Enrique Alvarado Sarmiento
**Feature branch:** Semana 7 — Vuelo Manual
**Servicio:** `Waitlist Service` (puerto 5006)

---

## Índice

1. [Contexto de negocio](#1-contexto-de-negocio)
2. [Dominio del problema](#2-dominio-del-problema)
3. [Historias de Usuario](#3-historias-de-usuario)
4. [Criterios de Aceptación — Gherkin](#4-criterios-de-aceptación--gherkin)
5. [Vista QA — Estrategia de pruebas](#5-vista-qa--estrategia-de-pruebas)
6. [Vista DEV — Implementación técnica](#6-vista-dev--implementación-técnica)
7. [Diferencias entre diseño e implementación final](#7-diferencias-entre-diseño-e-implementación-final)

---

## 1. Contexto de Negocio

### El problema real

En cualquier plataforma de ticketing existe un momento crítico: la reserva expira. Un usuario reservó un asiento, tuvo 15 minutos para pagar y no lo hizo. El asiento queda libre.

Lo que ocurría antes de esta feature: **ese asiento volvía al inventario disponible sin ningún criterio de equidad.** El primer usuario que recargaba la página y hacía clic lo obtenía. Esto se conoce como *F5 warfare* — una carrera injusta donde gana quien tiene mejor conexión o mejor reflejos, no quien llegó primero.

El resultado para el negocio era doble: pérdida de ventas por usuarios frustrados que abandonaron el proceso, y una experiencia percibida como injusta por quienes intentaron comprar y no pudieron.

### La solución

La **Lista de Espera Inteligente** captura la demanda insatisfecha y la administra con equidad. Cuando un evento se agota, los usuarios pueden registrarse con su correo. El sistema mantiene un orden estricto de llegada y, cada vez que un asiento queda disponible, se lo ofrece automáticamente al primero en la cola — sin competencia, sin carreras, con un tiempo razonable para completar el pago.

### Impacto esperado

| Antes | Después |
|-------|---------|
| Asiento libre → carrera de clics | Asiento libre → primer usuario en cola recibe oferta |
| Demanda insatisfecha sin capturar | Cada usuario que quiso comprar, queda registrado |
| Usuarios frustrados abandonan el sistema | Usuario en cola recibe notificación y link de pago |
| "Stock fantasma" (asientos que nadie paga) | Rotación automática garantiza que el asiento se ofrezca hasta agotarse la cola |

### Motivación de negocio en tres líneas

> Un asiento que expira no es un asiento perdido — es una oportunidad que le pertenece a quien esperó más tiempo. La feature garantiza que esa oportunidad siempre llegue a alguien, en orden, de forma automática.

---

## 2. Dominio del Problema

### Glosario del dominio

| Término | Definición |
|---------|-----------|
| **Lista de Espera** | Cola FIFO de usuarios interesados en un evento agotado |
| **Entrada en Lista de Espera** | Registro individual de un usuario en la cola de un evento específico |
| **Cola FIFO** | First In, First Out — el primero en registrarse es el primero en recibir asignación |
| **Asignación Automática** | El sistema selecciona al primero de la cola y le reserva un asiento sin intervención humana |
| **Rotación de Asignación** | Cuando el usuario asignado no paga en 30 minutos, el asiento pasa al siguiente en la cola |
| **Ventana de Pago** | 30 minutos que tiene un usuario asignado para completar el pago |
| **Inventario Disponible** | Asientos en estado `Available` accesibles para cualquier usuario |
| **Asiento Bloqueado** | Asiento en estado `Reserved` retenido para la lista de espera durante la rotación |

### Entidades del dominio

```
WaitlistEntry          Event (de Catalog)
─────────────          ──────────────────
Id: Guid               Id: Guid
Email: string          Name: string
EventId: Guid   ──────►(agotado para activar waitlist)
SeatId?: Guid
OrderId?: Guid         Seat (de Inventory)
Status: string         ──────────────────
RegisteredAt           Id: Guid
AssignedAt?            EventId: Guid
ExpiresAt?             Status: Available|Reserved|Sold
```

### Máquina de estados de WaitlistEntry

```
              POST /join
[No existe] ──────────────► [pending]
                                │
                    reservation-expired (Kafka)
                     → AssignNext automático
                                │
                                ▼
                           [assigned]
                          ExpiresAt = now + 30min
                         /                    \
              payment-succeeded          ExpiresAt < now
              (Kafka)                  (WaitlistExpiryWorker)
                    │                         │
                    ▼                         ▼
              [completed]                [expired]
```

### Reglas de negocio

| ID | Regla |
|----|-------|
| **RN-01** | Un usuario solo puede tener una entrada activa (`pending` o `assigned`) por evento |
| **RN-02** | No se puede unir a la lista de espera si el evento tiene asientos disponibles |
| **RN-03** | La cola es FIFO estricto — ordenada por `RegisteredAt ASC` |
| **RN-04** | El usuario asignado tiene exactamente 30 minutos para completar el pago |
| **RN-05** | Si el tiempo expira y hay siguiente en cola, el asiento NO vuelve al inventario disponible — se rota directamente al siguiente |
| **RN-06** | Si el tiempo expira y la cola está vacía, el asiento se libera al inventario disponible |

---

## 3. Historias de Usuario

### HU-01 — Registro en Lista de Espera

```
Como usuario que visualiza un evento agotado
Quiero ingresar mi correo para unirme a la lista de espera
Para ser considerado automáticamente si un asiento se libera
```

**Criterios de aceptación clave:**
- El sistema solo permite registro si el evento tiene stock = 0
- Se retorna la posición en la cola al registrarse
- No se puede registrar dos veces con el mismo correo en el mismo evento

**Valor de negocio:** Captura la demanda insatisfecha y da al usuario sensación de equidad y control

---

### HU-02 — Asignación Automática al liberarse un asiento

```
Como usuario en la cola de espera
Quiero que el sistema me asigne un asiento automáticamente cuando uno se libere
Para asegurar mi lugar sin competir nuevamente por el inventario disponible
```

**Criterios de aceptación clave:**
- La asignación se dispara al recibir un evento `reservation-expired` de Kafka
- El primer usuario en cola (FIFO) recibe la asignación
- Se genera una orden de compra automática en Ordering
- El usuario recibe notificación con el link de pago y un contador de 30 minutos

**Valor de negocio:** Elimina la "carrera de clics" y garantiza que cada asiento liberado tenga un comprador

---

### HU-03 — Rotación de Asignación por Inacción

```
Como sistema de gestión de la lista de espera
Quiero detectar cuando un usuario asignado no completa el pago en 30 minutos
Para reasignar el asiento al siguiente en la cola sin liberarlo al inventario disponible
```

**Criterios de aceptación clave:**
- El sistema detecta automáticamente la expiración del timer de 30 minutos
- El asiento bloqueado se rota directamente al siguiente en cola (sin pasar por inventario disponible)
- Si la cola está vacía, el asiento se libera al inventario disponible
- El usuario expirado recibe notificación de expiración
- El nuevo asignado recibe notificación con su propio link de pago de 30 minutos

**Valor de negocio:** Garantiza que ningún asiento quede "atascado" entre usuarios que no pagaron y permite que siempre haya un comprador esperando

---

## 4. Criterios de Aceptación — Gherkin

### Feature: Sistema de Lista de Espera Inteligente

---

#### Escenario 1: Registro exitoso en lista de espera

```gherkin
Dado que el Evento "Concierto Rock 2026" tiene stock = 0
Cuando el usuario "jostin@example.com" envía POST /api/v1/waitlist/join con EventId válido
Entonces el sistema responde 201 Created
Y la Entrada queda registrada con Status = "pending"
Y la respuesta incluye la posición en la cola (entryId, position)
```

---

#### Escenario 2: Intento de registro con asientos disponibles

```gherkin
Dado que el Evento "Concierto Rock 2026" tiene stock > 0
Cuando el usuario "jostin@example.com" intenta unirse a la lista de espera
Entonces el sistema responde 409 Conflict
Y el mensaje indica "Hay tickets disponibles para este evento. La lista de espera no aplica."
```

---

#### Escenario 3: Registro duplicado en la misma lista de espera

```gherkin
Dado que "jostin@example.com" ya tiene una Entrada activa (pending o assigned) para "Concierto Rock 2026"
Cuando el mismo correo intenta registrarse nuevamente para el mismo Evento
Entonces el sistema responde 409 Conflict
Y el mensaje indica "Ya estás en la lista de espera de este evento."
```

---

#### Escenario 4: Asignación automática al expirar una reserva

```gherkin
Dado que "jostin@example.com" es el primero en la cola del Evento "Concierto Rock 2026"
Cuando Kafka recibe el evento "reservation-expired" con SeatId y concertEventId correspondientes
Entonces Waitlist crea una Orden de Compra automática para "jostin@example.com" en Ordering
Y actualiza el Status de la Entrada a "assigned"
Y establece ExpiresAt = ahora + 30 minutos
Y envía una notificación con el link de pago y validez de 30 minutos
```

---

#### Escenario 5: Rotación de asignación — siguiente en cola existe

```gherkin
Dado que "jostin@example.com" fue asignado y no pagó en 30 minutos
Y "segundo@example.com" es el siguiente en la cola del mismo Evento
Cuando WaitlistExpiryWorker detecta que ExpiresAt de "jostin" < ahora
Entonces el sistema marca la Entrada de "jostin@example.com" como "expired"
Y envía notificación de expiración a "jostin@example.com"
Y cancela la orden de compra de "jostin@example.com" en Ordering
Y crea una nueva orden de compra para "segundo@example.com"
Y actualiza la Entrada de "segundo@example.com" a "assigned" con nuevo ExpiresAt = ahora + 30 minutos
Y envía notificación con link de pago a "segundo@example.com"
Y el asiento NO vuelve al inventario disponible
```

---

#### Escenario 6: Rotación con cola vacía — liberar al inventario

```gherkin
Dado que "jostin@example.com" fue asignado y no pagó en 30 minutos
Y no hay más entradas "pending" en la cola para el Evento
Cuando WaitlistExpiryWorker detecta que ExpiresAt de "jostin" < ahora
Entonces el sistema marca la Entrada de "jostin@example.com" como "expired"
Y envía notificación de expiración a "jostin@example.com"
Y cancela la orden de compra de "jostin@example.com" en Ordering
Y llama a Inventory para liberar el asiento al inventario disponible
```

---

#### Escenario 7: Pago completado por usuario asignado

```gherkin
Dado que "jostin@example.com" tiene una Entrada con Status = "assigned"
Y su Orden de Compra tiene orderId = X
Cuando Kafka recibe el evento "payment-succeeded" con orderId = X
Entonces Waitlist actualiza la Entrada de "jostin@example.com" a Status = "completed"
```

---

#### Escenario 8: Consulta de pendientes (integración con Inventory)

```gherkin
Dado que hay 3 entradas con Status = "pending" para el EventId "abc123"
Cuando Inventory llama GET /api/v1/waitlist/has-pending?eventId=abc123
Entonces el sistema responde 200 OK
Y la respuesta incluye { "hasPending": true, "pendingCount": 3 }
```

---

#### Escenario 9: Validación de request inválido

```gherkin
Dado que el usuario envía POST /api/v1/waitlist/join
Cuando el campo "email" está vacío o tiene formato inválido
Entonces el sistema responde 400 Bad Request
Y la respuesta incluye los errores de validación específicos
```

---

#### Escenario 10: Servicio de catálogo no disponible

```gherkin
Dado que el Catalog Service no responde al consultar disponibilidad
Cuando el usuario "jostin@example.com" intenta unirse a la lista de espera
Entonces el sistema responde 503 Service Unavailable
Y el mensaje indica "No fue posible verificar la disponibilidad del evento."
```

---

## 5. Vista QA — Estrategia de Pruebas

### Pirámide de pruebas aplicada

```
           ▲
          /E2E\        Manual / Exploratorio
         /─────\
        /  Integ \     Consumidores Kafka, HTTP Clients
       /──────────\
      / Unit Tests \   Dominio + Handlers (cobertura total)
     /──────────────\
```

### Cobertura por TDD (Ciclos implementados)

#### Ciclos 1–6 — Dominio (`WaitlistEntry`)

| Ciclo | Test | Comportamiento validado |
|-------|------|------------------------|
| 1 | Create happy path | `WaitlistEntry.Create()` retorna entrada con Status=pending y RegisteredAt=now |
| 2 | Create guard — email vacío | Lanza excepción si email es blank |
| 3 | Create guard — eventId vacío | Lanza excepción si eventId es Guid.Empty |
| 4 | Assign sets fields | `Assign(seatId, orderId)` setea SeatId, OrderId, Status=assigned, ExpiresAt=now+30min |
| 5 | Assign guard — solo desde pending | Lanza excepción si se llama Assign desde estado distinto a pending |
| 6 | Complete / Expire transitions | `Complete()` y `Expire()` solo desde assigned; `IsAssignmentExpired()` verifica tiempo |

---

#### Ciclos 7–11 — JoinWaitlistHandler

| Ciclo | Test | Comportamiento validado |
|-------|------|------------------------|
| 7 | Catalog unavailable | Lanza `WaitlistServiceUnavailableException` (→ 503) |
| 8 | Stock disponible | Lanza `WaitlistConflictException` con mensaje "Hay tickets disponibles" (→ 409) |
| 9 | Entrada duplicada | Lanza `WaitlistConflictException` con mensaje "Ya estás en la lista" (→ 409) |
| 10 | Happy path — registro exitoso | Persiste entrada, retorna entryId y posición |
| 11 | Validación de request | FluentValidation rechaza email inválido o eventId vacío (→ 400) |

---

#### Ciclos 12–14 — AssignNextHandler

| Ciclo | Test | Comportamiento validado |
|-------|------|------------------------|
| 12 | Happy path — asignación | Crea orden en Ordering, llama Assign en entry, persiste, envía email |
| 13 | Cola vacía — no-op | Si no hay pending para el evento, no hace nada |
| 14 | Idempotencia — asiento ya asignado | Si el seat ya tiene un assigned entry, retorna sin acción (no duplica asignación) |

---

#### Ciclo 16 — CompleteAssignmentHandler

| Ciclo | Test | Comportamiento validado |
|-------|------|------------------------|
| 16 | Pago de orden que no es waitlist | Si no encuentra entry por orderId, retorna (no-op) |
| 16 | Pago de orden waitlist | Encuentra entry, llama Complete(), persiste |

---

#### Ciclos 17–19 — WaitlistExpiryWorker

| Ciclo | Test | Comportamiento validado |
|-------|------|------------------------|
| 17 | Rotación — siguiente existe | Expira entrada actual, cancela orden, crea nueva orden para siguiente, notifica ambos |
| 18 | Release — cola vacía | Expira entrada actual, cancela orden, llama Inventory.ReleaseSeat |
| 19 | Sin expirados — no-op | Worker ejecuta ciclo sin hacer nada si no hay expirados |

---

#### Ciclo 15 — ReservationExpiredConsumer

| Ciclo | Test | Comportamiento validado |
|-------|------|------------------------|
| 15 | Evento v3 válido | Deserializa y dispara AssignNextCommand con seatId y concertEventId |
| 15 | Evento sin concertEventId (v2 legacy) | Se omite silenciosamente (compatibilidad hacia atrás) |
| 15 | Evento con seatId vacío | Se omite con log de warning |

---

### Casos de prueba de integración manual

| ID | Escenario | Pasos | Resultado esperado |
|----|-----------|-------|-------------------|
| IT-01 | Flujo completo de waitlist | 1) Agotar asientos → 2) Join waitlist → 3) Expirar una reserva → 4) Verificar email recibido | Entry pasa a assigned, email enviado |
| IT-02 | Rotación por expiración de pago | 1) Join 2 usuarios → 2) Asignar primero → 3) Esperar 30 min (o simular) → 4) Verificar segundo asignado | Segundo recibe asignación, asiento no liberado |
| IT-03 | Liberación con cola vacía | 1) Join 1 usuario → 2) Asignar → 3) Expirar → 4) Verificar inventory | Inventory tiene asiento en Available |
| IT-04 | has-pending desde Inventory | Llamar manualmente GET /api/v1/waitlist/has-pending | Responde con conteo correcto |

---

## 6. Vista DEV — Implementación Técnica

### Estructura del servicio

```
services/waitlist/src/
├── Waitlist.Api/
│   ├── Program.cs                            ← Startup, CORS, migrations, DI
│   └── Controllers/
│       └── WaitlistController.cs             ← POST /join, GET /has-pending
│
├── Waitlist.Application/
│   ├── UseCases/
│   │   ├── JoinWaitlist/
│   │   │   ├── JoinWaitlistCommand.cs        ← record(Email, EventId) → JoinWaitlistResult
│   │   │   └── JoinWaitlistHandler.cs        ← validación + persistencia + posición
│   │   ├── AssignNext/
│   │   │   ├── AssignNextCommand.cs          ← record(SeatId, ConcertEventId)
│   │   │   └── AssignNextHandler.cs          ← FIFO + orden + email
│   │   └── CompleteAssignment/
│   │       ├── CompleteAssignmentCommand.cs  ← record(OrderId)
│   │       └── CompleteAssignmentHandler.cs  ← idempotencia + Complete()
│   └── Ports/
│       ├── IWaitlistRepository.cs
│       ├── ICatalogClient.cs                 ← GetAvailableCountAsync(eventId)
│       ├── IOrderingClient.cs                ← CreateWaitlistOrderAsync, CancelOrderAsync
│       ├── IInventoryClient.cs               ← ReleaseSeatAsync(seatId)
│       └── IEmailService.cs                  ← SendAsync(to, subject, body)
│
├── Waitlist.Domain/
│   └── Entities/
│       └── WaitlistEntry.cs                  ← Aggregate root con máquina de estados
│
└── Waitlist.Infrastructure/
    ├── Persistence/
    │   ├── WaitlistDbContext.cs              ← Schema bc_waitlist, índices FIFO/expiry/order
    │   └── WaitlistRepository.cs            ← 8 métodos de acceso a datos
    ├── Clients/
    │   ├── CatalogHttpClient.cs             ← GET /events/{id}/seatmap → cuenta available
    │   ├── OrderingHttpClient.cs            ← POST /orders/waitlist, PATCH /orders/{id}/cancel
    │   ├── InventoryHttpClient.cs           ← PUT /api/v1/seats/{id}/release
    │   └── SmtpEmailService.cs             ← Stub (log en dev)
    ├── Consumers/
    │   ├── ReservationExpiredConsumer.cs    ← Kafka: reservation-expired → AssignNext
    │   └── PaymentSucceededConsumer.cs      ← Kafka: payment-succeeded → CompleteAssignment
    └── Workers/
        └── WaitlistExpiryWorker.cs          ← Polling cada 10s → rotación/liberación
```

---

### Entidad de dominio: WaitlistEntry

```csharp
public class WaitlistEntry
{
    public const string StatusPending   = "pending";
    public const string StatusAssigned  = "assigned";
    public const string StatusExpired   = "expired";
    public const string StatusCompleted = "completed";

    public Guid      Id           { get; private set; }
    public string    Email        { get; private set; }
    public Guid      EventId      { get; private set; }
    public Guid?     SeatId       { get; private set; }
    public Guid?     OrderId      { get; private set; }
    public string    Status       { get; private set; }
    public DateTime  RegisteredAt { get; private set; }
    public DateTime? AssignedAt   { get; private set; }
    public DateTime? ExpiresAt    { get; private set; }

    // Factory
    public static WaitlistEntry Create(string email, Guid eventId) { ... }

    // Transiciones
    public void Assign(Guid seatId, Guid orderId)  // pending → assigned, ExpiresAt = now+30min
    public void Complete()                          // assigned → completed
    public void Expire()                            // assigned → expired

    // Query
    public bool IsAssignmentExpired() =>
        Status == StatusAssigned && DateTime.UtcNow > ExpiresAt;
}
```

---

### Endpoint: POST /api/v1/waitlist/join

```
Request:  { email: string, eventId: Guid }
Response:
  201 → { entryId: Guid, position: int }
  400 → { errors: string[] }           ← FluentValidation
  409 → { message: string }            ← Stock disponible o entrada duplicada
  503 → { message: string }            ← Catalog no disponible
```

**Lógica del handler:**
1. Catalog.GetAvailableCount(eventId) → si falla → 503
2. Si count > 0 → 409 "Hay tickets disponibles"
3. Si HasActiveEntry(email, eventId) → 409 "Ya estás en la lista"
4. WaitlistEntry.Create(email, eventId)
5. Repository.AddAsync(entry)
6. GetQueuePosition(eventId) → retornar posición

---

### Endpoint: GET /api/v1/waitlist/has-pending

```
Query:    ?eventId=Guid
Response:
  200 → { hasPending: bool, pendingCount: int }
  400 → { message: "eventId is required." }
```

**Propósito:** Usado por `Inventory.ReservationExpiryWorker` (ADR-03) para decidir si retener el asiento para la lista de espera o liberarlo al inventario disponible.

---

### Flujo de Asignación Automática

```
Kafka topic: reservation-expired
        │
        ▼
ReservationExpiredConsumer
  Deserializa ReservationExpiredEventV3
  Guard: concertEventId != empty
  Guard: seatId != empty
        │
        ▼
AssignNextCommand(seatId, concertEventId)
        │
        ▼
AssignNextHandler:
  1. HasAssignedEntryForSeat(seatId) → si ya asignado → no-op (idempotencia)
  2. GetNextPending(eventId) → si null → no-op (cola vacía)
  3. Ordering.CreateWaitlistOrder(seatId, price=0, guestToken=email, eventId)
  4. entry.Assign(seatId, orderId)    ← ExpiresAt = now + 30min
  5. Repository.UpdateAsync(entry)
  6. EmailService.SendAsync(email, "Asiento disponible", ...)
```

---

### Flujo de Rotación de Asignación (Worker)

```
WaitlistExpiryWorker — cada 10 segundos:
        │
        ▼
Repository.GetExpiredAssignedAsync()
  → WHERE Status='assigned' AND ExpiresAt <= NOW()
        │
        ▼ Para cada entrada expirada:
  1. entry.Expire()  → Status = "expired"
  2. Repository.UpdateAsync(entry)
  3. EmailService.Send(entry.Email, "Tu turno expiró")
        │
        ▼
  Repository.GetNextPending(entry.EventId)
        ├── Existe siguiente:
        │     4. Ordering.CancelOrder(entry.OrderId)
        │     5. Ordering.CreateWaitlistOrder(entry.SeatId, 0, next.Email, eventId)
        │     6. next.Assign(entry.SeatId, newOrderId)
        │     7. Repository.UpdateAsync(next)
        │     8. EmailService.Send(next.Email, "Tienes 30 minutos para pagar")
        │
        └── Cola vacía:
              4. Inventory.ReleaseSeat(entry.SeatId)
              5. Ordering.CancelOrder(entry.OrderId)
```

---

### Schema de Base de Datos

**Schema:** `bc_waitlist`

```sql
CREATE TABLE waitlist_entries (
    "Id"           UUID                     NOT NULL PRIMARY KEY,
    "Email"        VARCHAR(320)             NOT NULL,
    "EventId"      UUID                     NOT NULL,
    "SeatId"       UUID,
    "OrderId"      UUID,
    "Status"       VARCHAR(20)              NOT NULL,
    "RegisteredAt" TIMESTAMP WITH TIME ZONE NOT NULL,
    "AssignedAt"   TIMESTAMP WITH TIME ZONE,
    "ExpiresAt"    TIMESTAMP WITH TIME ZONE
);

-- FIFO: obtener el siguiente pending por evento en orden de llegada
CREATE INDEX idx_waitlist_fifo
ON waitlist_entries ("EventId", "Status", "RegisteredAt");

-- Worker: escanear asignaciones expiradas rápidamente
CREATE INDEX idx_waitlist_expiry
ON waitlist_entries ("ExpiresAt")
WHERE "Status" = 'assigned';

-- CompleteAssignment: lookup por orderId
CREATE INDEX idx_waitlist_order
ON waitlist_entries ("OrderId")
WHERE "OrderId" IS NOT NULL;
```

---

### Integración con otros servicios

| Dirección | Servicio | Mecanismo | Operación |
|-----------|---------|-----------|-----------|
| ← Recibe | Inventory | Kafka: `reservation-expired` | Trigger de asignación automática |
| ← Recibe | Payment | Kafka: `payment-succeeded` | Marca assignment como completado |
| → Llama | Catalog | HTTP GET | Verifica disponibilidad de asientos |
| → Llama | Ordering | HTTP POST | Crea orden de compra para usuario asignado |
| → Llama | Ordering | HTTP PATCH | Cancela orden al rotar o expirar |
| → Llama | Inventory | HTTP PUT | Libera asiento cuando la cola está vacía |
| ← Es llamado por | Inventory | HTTP GET | `has-pending` — ADR-03 |

---

### Configuración del servicio

```json
{
  "ConnectionStrings": {
    "Default": "Host=postgres;Port=5432;Database=ticketing;Username=postgres;Password=postgres;SearchPath=bc_waitlist"
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092"
  },
  "Services": {
    "CatalogUrl":   "http://catalog:5001",
    "OrderingUrl":  "http://ordering:5003",
    "InventoryUrl": "http://inventory:5002"
  }
}
```

---

## 7. Diferencias entre Diseño e Implementación Final

El diseño original (`docs/week7/feature_final.md`) planteó una arquitectura válida que evolucionó durante la implementación. Las diferencias son decisiones técnicas deliberadas, no desvíos del problema de negocio.

| Aspecto | Diseño original | Implementación final | Razón del cambio |
|---------|----------------|----------------------|-----------------|
| **Trigger de rotación** | Kafka `order-payment-timeout` publicado por Ordering | `WaitlistExpiryWorker` polling cada 10s dentro del propio servicio | La rotación es responsabilidad de Waitlist, no de Ordering. Mantiene el bounded context limpio |
| **Campo Priority** | `WaitlistEntry.Priority: int` | No existe — se usa `RegisteredAt ASC` para FIFO | `RegisteredAt` es la fuente de verdad del orden; un campo `priority` sería redundante |
| **Campo SeatId** | No contemplado en diseño | `SeatId?: Guid` agregado | Necesario para la lógica de rotación — el worker necesita saber qué asiento rotar |
| **ExpiresAt** | Timer implícito de 30min | `ExpiresAt: DateTime?` explícito en la entidad | Permite consultas SQL directas con índice filtrado; más eficiente que calcular en memoria |
| **Consulta `has-pending`** | No mencionada | `GET /api/v1/waitlist/has-pending` (ADR-03) | Punto de integración necesario para que Inventory decida si retener o liberar asiento |
| **Status como tipo** | `WaitlistStatus` enum | Constantes `string` en la entidad | Más simple de persistir en EF Core sin conversiones; igualmente expresivo |
| **CompleteAssignment** | No detallado | Handler explícito vía Kafka `payment-succeeded` | Necesario para cerrar el ciclo de vida de la entrada cuando el pago se confirma |

> Los objetivos de negocio (HU-01, HU-02, HU-03), las reglas de negocio (RN-01 a RN-06) y los criterios de aceptación permanecen **exactamente iguales** entre diseño e implementación.
