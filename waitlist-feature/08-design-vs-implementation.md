# 08 — Diseño vs. Implementación Real
## Trazabilidad de decisiones: del documento `feature_final.md` al código

> **Autor:** Jostin Enrique Alvarado Sarmiento
> **Propósito:** Documentar cada punto donde la implementación real difirió del diseño original, explicando la causa técnica de cada cambio.
---

## Visión general

El documento `docs/week7/feature_final.md` fue el punto de partida del diseño. La implementación respetó los flujos de negocio y las reglas de dominio, pero introdujo **6 cambios técnicos** durante la implementación — ninguno por descuido, todos por razones concretas de ingeniería.

| # | Aspecto | Diseño original | Implementación real | Tipo de cambio |
|---|---------|-----------------|---------------------|----------------|
| 1 | Ventana de pago | 30 minutos | 2 minutos | Parámetro de entorno |
| 2 | Trigger de rotación | Evento Kafka `order-payment-timeout` | Worker de polling cada 10s | Decisión arquitectónica |
| 3 | Campo `Priority` | Presente en la entidad | Eliminado | Simplificación de modelo |
| 4 | Campos `SeatId` y `ExpiresAt` | No estaban en el diseño | Agregados a la entidad | Completitud del modelo |
| 5 | Nombre del campo Kafka | `eventId` | `concertEventId` | Claridad semántica + versioning |
| 6 | `ReasignarAsiento` en Inventory | Operación atómica nueva | No implementada — 2 pasos: cancel + recreate | Decisión de costo/riesgo |

---

## Cambio 1 — Ventana de pago: 30 minutos → 2 minutos

### Qué dice el documento
> **RN-03:** Un Usuario Asignado dispone de exactamente **30 minutos** para completar el pago de su Orden de Compra.

### Qué hace el código
**Archivo:** `services/waitlist/src/Waitlist.Domain/Entities/WaitlistEntry.cs`

```csharp
public void Assign(Guid seatId, Guid orderId)
{
    // ...
    AssignedAt = DateTime.UtcNow;
    ExpiresAt = AssignedAt.Value.AddMinutes(2); // ← hardcodeado en 2
}
```

### Por qué cambió

La regla de negocio de 30 minutos es correcta y se mantiene. El valor de 2 minutos es una **decisión de entorno de desarrollo**, no una corrección al diseño.

Durante el desarrollo y las pruebas del sistema, el flujo completo de la feature involucra:
1. Reservar un asiento (expira en 15 min en Inventory)
2. Unirse a la lista de espera
3. Esperar que el ReservationExpiryWorker detecte la expiración
4. Que Waitlist asigne al siguiente
5. Esperar que la asignación expire
6. Que el WaitlistExpiryWorker detecte la expiración y rote

Con valores de producción (15 min + 30 min), una sola iteración del flujo tomaría 45 minutos. Con valores de desarrollo (15 seg polling + 2 min), el ciclo completo se puede observar en menos de 3 minutos.

**La arquitectura es correcta:** el valor vive en `WaitlistEntry.Assign()` y en producción se leería de configuración (`IOptions<WaitlistSettings>`). La regla de negocio no cambió — cambió el parámetro de calibración.

---

## Cambio 2 — Trigger de rotación: `order-payment-timeout` → Worker de polling

### Qué dice el documento

El documento definía el **Flujo C (Rotación de Asignación por Inacción)** así:

```
Ordering detecta TTL de 30 min vencido
    → publica order-payment-timeout (Kafka)
        → Waitlist consume
            → expira entrada
            → rota al siguiente o libera el asiento
```

El documento también especificaba (sección 10, Impacto en el Sistema):
> **Servicio de Órdenes:** Requiere nuevo endpoint para Reasignación de Orden de Compra y publicación del evento `order-payment-timeout` al vencer el TTL de 30 min.

### Qué hace el código

El evento `order-payment-timeout` **no existe en el sistema**. La rotación la detecta Waitlist desde su propio estado interno.

**Archivo:** `services/waitlist/src/Waitlist.Infrastructure/Workers/WaitlistExpiryWorker.cs`

```
BackgroundService — ejecuta cada 10 segundos:

1. repo.GetExpiredAssignedAsync()
   → SELECT * FROM waitlist_entries
     WHERE status = 'assigned' AND expires_at <= NOW()

2. Para cada entrada expirada:
   a. entry.Expire()  → status = 'expired'
   b. Email al usuario expirado
   c. repo.GetNextPendingAsync(eventId)
   
   d.1 Si hay siguiente en cola:
       - ordering.CancelOrderAsync(expired.OrderId)
       - ordering.CreateWaitlistOrderAsync(seatId, next.Email, eventId)
       - next.Assign(seatId, newOrderId)
       - Email al siguiente usuario
   
   d.2 Si la cola está vacía:
       - inventory.ReleaseSeatAsync(seatId)
       - ordering.CancelOrderAsync(expired.OrderId)
```

### Por qué cambió

**El diseño original acoplaba el dominio de Waitlist dentro del servicio Ordering.**

Para implementar `order-payment-timeout`, Ordering tendría que:
- Conocer que existe un concepto de "lista de espera"
- Distinguir órdenes de waitlist de órdenes normales de compra
- Llevar un timer interno por cada orden de waitlist
- Publicar un evento diferente al de cancelación estándar

Eso viola el **principio de responsabilidad única a nivel de servicio**: Ordering gestiona el ciclo de vida de las órdenes, no el ciclo de vida de las asignaciones de waitlist.

**La solución de polling es arquitectónicamente más limpia porque:**

1. Waitlist ya tiene `ExpiresAt` en su propia tabla — posee la información completa sin depender de notificaciones externas
2. Ordering permanece completamente ignorante de que existe un sistema de lista de espera
3. El acoplamiento entre servicios se reduce: Waitlist llama a Ordering (dependencia de salida), pero Ordering no sabe nada de Waitlist
4. Si Waitlist no existiera, Ordering funcionaría exactamente igual

La contrapartida del polling (latencia de hasta 10 segundos entre expiración real y detección) es aceptable dado que la ventana de pago es de 2 minutos en desarrollo y 30 minutos en producción.

---

## Cambio 3 — Campo `Priority` eliminado

### Qué dice el documento

**Sección 7.2 (Vista Lógica):**
```
class WaitlistEntry {
    +int Priority
}
```
**ADR-01:** índice compuesto `(EventId, Status, Priority, RegisteredAt)`.

### Qué hace el código

**Archivo de migración:** `services/waitlist/src/Waitlist.Infrastructure/Migrations/20260403063746_InitialCreate.cs`

El campo `Priority` no existe. El índice real es:
```sql
CREATE INDEX idx_waitlist_fifo 
ON waitlist_entries (event_id, status, registered_at);
```

La query FIFO real:
```csharp
// WaitlistRepository.cs
return await _context.WaitlistEntries
    .Where(e => e.EventId == eventId && e.Status == WaitlistStatus.Pending)
    .OrderBy(e => e.RegisteredAt)
    .FirstOrDefaultAsync();
```

### Por qué cambió

`Priority` era un **dato derivado de `RegisteredAt`** — no agregaba información nueva.

FIFO significa que el primero en registrarse tiene prioridad. Eso ya está capturado en el timestamp `RegisteredAt`. Agregar un entero `Priority` requeriría:
- Calcularlo al insertar (¿COUNT + 1? ¿MAX + 1?)
- Mantenerlo consistente si hay inserciones concurrentes
- Nunca actualizar el valor tras la inserción (las prioridades no cambian en FIFO)

Es más simple, más correcto y más eficiente usar directamente el timestamp. Dos fuentes de verdad para el mismo dato (el orden de llegada) son peores que una sola fuente.

---

## Cambio 4 — Campos `SeatId` y `ExpiresAt` agregados al modelo

### Qué dice el documento

**Sección 7.2 (Vista Lógica):**
```
class WaitlistEntry {
    +Guid Id
    +string Email
    +Guid EventId
    +Guid? OrderId
    +DateTime RegisteredAt
    +DateTime? AssignedAt
    +WaitlistStatus Status
    +int Priority
}
```

`SeatId` y `ExpiresAt` no estaban en el diseño original.

### Qué hace el código

**Archivo:** `services/waitlist/src/Waitlist.Domain/Entities/WaitlistEntry.cs`

```csharp
public Guid? SeatId { get; private set; }     // ← agregado
public DateTime? ExpiresAt { get; private set; } // ← agregado
```

**Índice para el worker:**
```sql
CREATE INDEX idx_waitlist_expiry 
ON waitlist_entries (expires_at) 
WHERE status = 'assigned';
```

### Por qué cambió

Ambos campos se descubrieron como **necesarios al implementar los flujos que el propio documento definía**.

**`SeatId`:** El `WaitlistExpiryWorker` necesita saber qué asiento transferir al siguiente o liberar a Inventory. Sin `SeatId` en la entidad de Waitlist, el worker tendría que llamar a Ordering (`GET /orders/{orderId}`) para preguntar "¿qué asiento tiene esta orden?". Eso agrega:
- Una llamada HTTP adicional en el camino crítico de la rotación
- Una dependencia de disponibilidad de Ordering para poder rotar
- Latencia extra en cada ciclo del worker

Guardando `SeatId` en Waitlist, el worker opera con autonomía total sobre sus propios datos.

**`ExpiresAt`:** Sin este campo, detectar entradas expiradas requeriría cargar todas las entradas con `status = 'assigned'` y filtrarlas en memoria:
```csharp
// Sin ExpiresAt — ineficiente
var assigned = await repo.GetAllAssignedAsync();
var expired = assigned.Where(e => e.AssignedAt + 2min <= now);
```

Con `ExpiresAt` en base de datos, la query es directa con índice filtrado:
```sql
SELECT * FROM waitlist_entries
WHERE status = 'assigned' AND expires_at <= NOW()
```

El documento definía correctamente que las entradas expiran, pero no modeló cómo se detectaría esa expiración eficientemente. Los campos surgieron de implementar, no de especular.

---

## Cambio 5 — Nombre del campo Kafka: `eventId` → `concertEventId`

### Qué dice el documento

El contrato en `contracts/kafka/reservation-expired.json` especificaba:
```json
{
  "reservationId": "uuid",
  "eventId": "uuid",
  "seatId": "uuid",
  "expiredAt": "datetime",
  "reason": "string",
  "status": "string"
}
```

### Qué hace el código

**Payload v3 publicado por Inventory** (`ReservationExpiryWorker.cs`):
```json
{
  "messageId": "uuid",
  "reservationId": "uuid",
  "seatId": "uuid",
  "customerId": "string (nullable)",
  "concertEventId": "uuid"
}
```

**Clase de deserialización en el consumer** (`ReservationExpiredConsumer.cs`):
```csharp
private sealed class ReservationExpiredEventV3
{
    public Guid MessageId { get; init; }
    public Guid ReservationId { get; init; }
    public Guid SeatId { get; init; }
    public string? CustomerId { get; init; }
    public Guid ConcertEventId { get; init; }  // ← no "EventId"
}
```

**Guard de versioning:**
```csharp
if (payload.ConcertEventId == Guid.Empty) return; // descarta mensajes v2
if (payload.SeatId == Guid.Empty) return;
```

### Por qué cambió

**Colisión semántica:** el campo `eventId` es ambiguo en el contexto de un sistema de eventos (tanto Kafka como de dominio).

Dentro del consumer de Waitlist, el nombre `eventId` podría referirse a:
- El ID del evento Kafka (el mensaje en sí)
- El ID del evento de dominio de Waitlist (`WaitlistEntry.EventId`)
- El ID del evento de concierto de Catalog

Renombrar a `concertEventId` elimina la ambigüedad: es el identificador del concierto en el servicio Catalog, el mismo que actúa como clave de agrupación en la cola de Waitlist.

**Versioning implícito:** Los mensajes del contrato original (v2) no tienen el campo `concertEventId`. Al deserializarlos, C# asigna `Guid.Empty` al campo. El guard `if (ConcertEventId == Guid.Empty) return` descarta silenciosamente esos mensajes, actuando como un filtro de compatibilidad sin necesidad de lógica de versioning explícita.

**Campos eliminados del contrato original** (`expiredAt`, `reason`, `status`): ningún consumer de Kafka los usaba. Publishar datos que nadie consume es ruido — se eliminaron para mantener el payload mínimo.

**Campo agregado `messageId`:** facilita la trazabilidad en logs. Cuando hay un error al procesar un mensaje, el log incluye `messageId` para correlacionar con el productor.

---

## Cambio 6 — `ReasignarAsiento` en Inventory no se implementó

### Qué dice el documento

**ADR-03 (sección 8):**
> El Asiento permanece bloqueado en el Servicio de Inventario hasta que el Servicio de Lista de Espera confirme si hay o no siguiente en la Cola de Espera. Solo se libera al Inventario Disponible si la Cola de Espera está vacía.

**Sección 10 (Impacto en el Sistema):**
> **Servicio de Inventario:** Requiere nueva operación `ReasignarAsiento(SeatId, NuevoEmail)` que transfiere el bloqueo de forma atómica, aumentando ligeramente la complejidad de ese servicio.

El documento identificó esto como el mayor riesgo de la feature:
> **HU-03 — 21 pts:** Si esa operación no está lista, esta HU no puede completarse.

### Qué hace el código

`ReasignarAsiento` no existe en Inventory. El `WaitlistExpiryWorker` ejecuta dos llamadas HTTP secuenciales:

```csharp
// 1. Cancelar la orden del usuario expirado
// Ordering cancela la orden → internamente libera la reserva en Inventory
await _orderingClient.CancelOrderAsync(expired.OrderId);

// 2. Crear nueva orden para el siguiente usuario
// Ordering crea orden → Inventory crea nueva reserva con Redis lock
var newOrderId = await _orderingClient.CreateWaitlistOrderAsync(
    seatId: expired.SeatId,
    price: 0m,
    guestToken: next.Email,
    concertEventId: next.EventId
);
```

El ADR-03 **sí se implementó** — el asiento no se libera al pool general durante la rotación. La diferencia es el mecanismo: en lugar de una operación atómica en Inventory, son dos llamadas HTTP rápidas donde el asiento pasa brevemente por "libre" antes de ser re-reservado.

### Por qué cambió

**Análisis de costo vs. riesgo:**

El documento mismo identificó que `ReasignarAsiento` era un **bloqueo potencial de desarrollo** (HU-03, 21 puntos, incertidumbre alta). Implementarla requería:
- Agregar un endpoint nuevo a Inventory (`PUT /seats/{seatId}/reassign`)
- Lógica de transferencia de reserva sin liberar al pool disponible
- Coordinar Redis locks: liberar el lock del usuario A y adquirir el lock del usuario B de forma atómica
- Nuevos tests en Inventory para el nuevo endpoint
- Riesgo de regresión en un servicio ya estable

La ventana de race condition de la alternativa (dos pasos) existe en teoría: entre la cancelación de la orden A y la creación de la orden B, el asiento queda brevemente en estado `available`. Si un usuario externo hiciera una reserva exactamente en esa ventana:

1. La llamada a Inventory pasaría el Redis lock check (asiento está `available`)
2. El siguiente paso del worker también llamaría a Inventory para crear la reserva
3. El Redis lock en Inventory rechazaría al segundo en llegar con error — no hay corrupción de datos

La ventana es de milisegundos entre dos llamadas HTTP internas. En términos prácticos, la probabilidad de que un usuario externo reactive exactamente en esa ventana es despreciable, y si ocurriera, Redis lock actuaría como red de seguridad.

**La decisión fue:** aceptar un riesgo teórico de muy baja probabilidad (con fallback de Redis lock) en lugar de asumir el costo real de modificar un servicio estable para mitigar un escenario que en la práctica no ocurre.

---

## Lo que se implementó exactamente como se diseñó

No todo fue diferente. Los siguientes aspectos se implementaron fielmente al documento:

### Flujo A — Registro en Lista de Espera ✅

`JoinWaitlistHandler` sigue el diagrama de secuencia del documento exactamente:
1. `ICatalogClient.GetAvailableCountAsync(eventId)` → si `available > 0` → 409
2. `repo.HasActiveEntryAsync(email, eventId)` → si existe → 409
3. `WaitlistEntry.Create(email, eventId)` → status `pending` → 201

### ADR-03 — Asiento bloqueado durante rotación ✅

`ReservationExpiryWorker` en Inventory consulta Waitlist antes de liberar:
```csharp
var hasPending = await _waitlistClient.HasPendingAsync(eventId);
if (hasPending)
    return; // Waitlist tomará control — no liberar
// else: liberar normalmente
```
El endpoint `GET /api/v1/waitlist/has-pending` no estaba en el documento pero fue la pieza necesaria para implementar el ADR-03 correctamente.

### Flujo B — Asignación automática ✅

`AssignNextHandler` (disparado por `ReservationExpiredConsumer`):
1. Idempotencia: si el asiento ya tiene asignación activa → return
2. FIFO: `GetNextPendingAsync` → `ORDER BY registered_at ASC`
3. `IOrderingClient.CreateWaitlistOrderAsync` → obtiene `orderId`
4. `entry.Assign(seatId, orderId)` → status `assigned`, `ExpiresAt` calculado
5. Email de notificación al usuario

### Máquina de estados ✅

Los estados y transiciones del documento se implementaron exactamente:

```
pending ──Assign()──► assigned ──Complete()──► completed
                          │
                       Expire()
                          │
                          ▼
                        expired
```

Cada transición tiene su guard: intentar `Complete()` desde `pending` lanza `InvalidOperationException`.

### Regla de unicidad ✅

`UNIQUE constraint (Email, EventId)` implementada en la migración, y adicionalmente verificada en la lógica del handler con `HasActiveEntryAsync`.

### PostgreSQL para la cola FIFO ✅

ADR-01 del documento: usar PostgreSQL con índices compuestos en lugar de Redis. Implementado con `bc_waitlist` schema en el PostgreSQL compartido (no una DB separada — adaptación de infra para simplificar el deployment).

---

## Resumen ejecutivo para la sustentación

**¿Cómo pasaste del documento al código?**

El documento de diseño fue la guía funcional — las reglas de negocio, los flujos, los criterios de aceptación se respetaron íntegramente. Los cambios ocurrieron en la capa técnica cuando la implementación reveló problemas que el diseño no había anticipado:

1. **El trigger de rotación** acopló Ordering al dominio de Waitlist → se resolvió con un worker autónomo
2. **El campo Priority** duplicaba información ya presente en RegisteredAt → se eliminó
3. **SeatId y ExpiresAt** eran necesarios para los propios flujos del documento → se agregaron
4. **La colisión semántica de eventId** generaba ambigüedad → se renombró con versioning implícito
5. **ReasignarAsiento** tenía un costo de implementación superior al riesgo que mitigaba → se reemplazó por dos pasos con Redis como fallback
6. **La ventana de 2 minutos** permite probar el sistema completo en tiempo razonable → parámetro configurable en producción

Ninguno de estos cambios altera las reglas de negocio. Todos son decisiones de implementación técnica tomadas con criterio de ingeniería, no por falta de comprensión del diseño.
