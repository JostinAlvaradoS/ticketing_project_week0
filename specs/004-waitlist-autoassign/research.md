# Research: Waitlist Autoassign

**Feature Branch**: `004-waitlist-autoassign`
**Date**: 2026-04-02
**Status**: Final

---

## Objetivo

Documentar las decisiones de investigación que sustentan el diseño técnico en `plan.md`. Cada decisión responde a una pregunta concreta planteada durante el análisis de la feature.

---

## R-001: ¿Dónde vive el `EventId` (concierto) en el dominio de Inventory?

**Pregunta**: El evento `reservation-expired` necesita indicar a qué cola de espera aplica. ¿Tiene la entidad `Reservation` del Inventory Service un campo `EventId`?

**Investigación**:

- Lectura de `services/inventory/src/Inventory.Domain/Entities/Reservation.cs` — el modelo tiene: `Id`, `SeatId`, `CustomerId`, `CreatedAt`, `ExpiresAt`, `Status`. **No tiene `EventId`.**
- Lectura de `services/inventory/src/Inventory.Domain/Entities/Seat.cs` — la entidad `Seat` tampoco almacena `EventId` (solo `Reserved: bool`).
- Contraste con `services/catalog/src/Catalog.Domain/Entities/Seat.cs` — el Catalog Service sí almacena `Seat.EventId` junto al estado completo (`available / reserved / sold`).

**Conclusión**: El `EventId` del concierto no está en `bc_inventory`. Para enriquecer `reservation-expired` con `concertEventId` es necesario:

1. Agregar `EventId` a `Inventory.Domain.Reservation`.
2. Propagar `EventId` desde `CreateReservationCommand` al crear la reserva.
3. Ejecutar migración EF Core que agrega la columna con valor por defecto temporal.

**Decisión**: Agregar `EventId` a `Reservation` y propagarlo desde la capa de aplicación al crear reservas.

---

## R-002: ¿El campo `eventId` actual en `reservation-expired` es el ID del concierto?

**Pregunta**: El payload actual del evento `reservation-expired` tiene un campo `eventId`. ¿Es el ID del concierto o algo más?

**Investigación**:

Lectura de `services/inventory/src/Inventory.Infrastructure/Workers/ReservationExpiryWorker.cs`:

```csharp
var @event = new
{
    eventId      = Guid.NewGuid().ToString("D"),   // ← generado en el momento
    reservationId = res.Id.ToString("D"),
    seatId        = res.SeatId.ToString("D"),
    customerId    = res.CustomerId
};
```

El `eventId` es **un UUID generado en el momento de publicar** — es el ID de idempotencia del mensaje Kafka, no el ID del evento/concierto.

**Conclusión**: Hay una colisión de nombres. El campo `eventId` es el ID del *mensaje* Kafka. Los consumidores existentes (`Ordering.ReservationEventConsumer`, `Payment.ReservationEventConsumer`) no lo utilizan como referencia al concierto — solo leen `reservationId` y `seatId`.

**Decisión**: Renombrar `eventId` → `messageId` en el payload. Agregar el nuevo campo `concertEventId` con el ID real del concierto. Los consumidores existentes no se ven afectados pues no leen ninguno de los dos campos.

---

## R-003: ¿Cómo gestionar el TTL de 30 minutos de la asignación de waitlist?

**Pregunta**: Una vez que el Waitlist Service asigna un turno, necesita expirar en 30 minutos si no hay pago. ¿Quién gestiona ese timer?

**Opciones evaluadas**:

| Opción | Descripción | Pros | Contras |
|---|---|---|---|
| A: Waitlist Service gestiona TTL | `WaitlistExpiryWorker` (BackgroundService) ejecuta cada 60s | Autónomo, sin cambios en Ordering | Introduce un worker adicional |
| B: Ordering Service gestiona TTL | Agregar `PaymentDueAt` a `Order`; nuevo worker en Ordering | Centraliza expiración en un servicio | Modifica dominio de Ordering, mayor blast radius |
| C: Kafka delayed events | Publicar evento con delay de 30 min | Sin polling | Kafka no tiene delay nativo; requiere scheduler externo |

**Investigación**: Se verificó que el Ordering Service ya tiene un dominio definido (`Order.StateDraft`, `Order.StatePending`). Agregar lógica de TTL de waitlist requeriría diferenciar entre órdenes regulares y de waitlist — agrega complejidad al dominio existente sin beneficio arquitectónico.

**Decisión**: Opción A. El `WaitlistExpiryWorker` en el Waitlist Service es análogo al `ReservationExpiryWorker` en el Inventory Service. El TTL es una responsabilidad del Waitlist Service ya que es su invariante de dominio.

---

## R-004: ¿Cómo evitar que el asiento se libere al pool general cuando hay cola activa?

**Pregunta**: El `ReservationExpiryWorker` llama `seat.Release()` antes de publicar `reservation-expired`. Si hay una cola activa, este asiento debería ser reasignado — no liberado. ¿Cómo resolver la race condition?

**Opciones evaluadas**:

| Opción | Descripción | Pros | Contras |
|---|---|---|---|
| A: Worker consulta `has-pending` antes de liberar | HTTP GET síncrono al Waitlist Service antes de `seat.Release()` | Simple, sin cambios de diseño de Kafka | Introduce dependencia HTTP Inventory → Waitlist |
| B: Waitlist re-bloquea el asiento post-evento | El asiento se libera brevemente; Waitlist lo re-reserva | Sin cambio al worker | Ventana de carrera visible para usuarios |
| C: Saga con compensación | Orquesta la transferencia atómica | Consistencia fuerte | Complejidad excesiva para MVP |

**Investigación**: La duración de la ventana en la Opción B depende de la latencia Kafka (~100-500ms) + tiempo de procesamiento. En eventos con alta demanda esto podría resultar en que otro usuario compre el asiento durante esa ventana.

**Decisión**: Opción A con fallback. Si el Waitlist Service no responde, el worker usa comportamiento original (libera el asiento). La consulta `has-pending` es de solo lectura y rápida (~10-50ms).

---

## R-005: ¿Cómo debe crear el Waitlist Service una orden en el Ordering Service?

**Pregunta**: Para asignar un turno, el Waitlist Service necesita crear una orden de compra. ¿Cómo interactuar con el Ordering Service?

**Investigación**:

- Lectura de `services/ordering/src/Ordering.Api/Controllers/OrdersController.cs` — el endpoint `POST /orders` requiere `userId` (Guid obligatorio). No existe soporte para `guestToken`.
- El flujo regular requiere: 1) `POST /cart/add` → 2) `POST /orders/checkout`. Dos llamadas HTTP.
- La arquitectura usa hexagonal: agregar un nuevo endpoint + handler es aditivo sin modificar lógica existente.

**Conclusión**: El endpoint existente no es compatible con flujo de invitado sin modificaciones. Una sola llamada atómica es preferible a dos llamadas para evitar inconsistencias.

**Decisión**: Agregar `POST /orders/waitlist` endpoint interno + `CreateWaitlistOrderCommand` handler. Retorna `{ orderId }`. El endpoint NO está expuesto al frontend directamente (uso interno entre microservicios).

---

## R-006: ¿PostgreSQL o Redis para la cola FIFO?

**Pregunta**: La cola de espera necesita orden FIFO estricto, durabilidad y cambios de estado transaccionales. ¿Qué tecnología usar?

**Opciones evaluadas**:

| Criterio | PostgreSQL | Redis Sorted Sets |
|---|---|---|
| Durabilidad | ACID full | Requiere AOF/RDB |
| FIFO | ORDER BY registered_at ASC | ZSCORE en timestamp |
| Transacciones | Totales (BEGIN/COMMIT) | MULTI/EXEC limitado |
| Auditoría | Queries SQL estándar | Difícil |
| Infraestructura nueva | No (ya existe) | Sí (requiere Redis) |

**Investigación**: El `docker-compose.yml` del proyecto ya incluye PostgreSQL. Redis no está en el stack actual.

**Decisión**: PostgreSQL con índice compuesto `(event_id, status, registered_at ASC)`. La query FIFO es `WHERE event_id = X AND status = 'pending' ORDER BY registered_at ASC LIMIT 1`. Latencia estimada ~2-5ms con índice.

---

## R-007: ¿Cómo implementar idempotencia en los consumers del Waitlist Service?

**Pregunta**: Kafka garantiza at-least-once delivery. ¿Cómo prevenir asignaciones duplicadas si `reservation-expired` llega dos veces?

**Investigación**:

- Patrón verificado en `services/ordering/src/Ordering.Infrastructure/Events/ReservationEventConsumer.cs` — usa guard de estado: si la orden ya está cancelada, omite el procesamiento.
- El mismo patrón se usa en `services/inventory/src/Inventory.Infrastructure/Consumers/PaymentFailedConsumer.cs`.

**Decisión**: Guard por estado + unicidad a nivel de base de datos:

1. `ReservationExpiredConsumer`: verificar si ya existe una entrada `Assigned` para el `seatId` antes de asignar.
2. `PaymentSucceededConsumer`: verificar si la entrada ya está `Completed` antes de `entry.Complete()`.
3. `WaitlistExpiryWorker`: verificar si la entrada ya está `Expired` antes de `entry.Expire()`.
4. Constraint único `(email, event_id)` para entradas `Pending`/`Assigned` a nivel de base de datos como segunda capa de protección.

---

## R-008: ¿El Catalog Service tiene endpoint para verificar disponibilidad de un evento?

**Pregunta**: Al recibir `POST /waitlist/join`, se necesita verificar si el evento tiene stock = 0. ¿Existe este endpoint en Catalog?

**Investigación**:

- Lectura de `services/catalog/src/Catalog.Api/Controllers/EventsController.cs` — existe `GET /events/{eventId}` que retorna el evento con su lista de asientos.
- Lectura de `services/catalog/src/Catalog.Api/Controllers/SeatsController.cs` — existe `GET /seats?eventId={id}` para listar asientos de un evento.

**Conclusión**: No existe un endpoint dedicado de disponibilidad que retorne directamente `{ availableCount: N }`. El Waitlist Service puede consultar `GET /events/{eventId}` y calcular stock disponible en cliente, o usar `GET /seats?eventId={id}&status=available`.

**Decisión**: El `CatalogHttpClient` en el Waitlist Service llamará `GET /events/{eventId}/seats?status=available` y verificará si el resultado tiene `count == 0`. Si el Catalog Service no responde, retornar `503 Service Unavailable` al cliente (no hacer fallback silencioso — per spec FR-002 nota de Assumptions).

---

## R-009: ¿Qué pasa si `payment-succeeded` llega antes que el Waitlist procese la asignación?

**Pregunta**: Hay una ventana entre `entry.Assign()` y el correo. ¿Qué pasa si el usuario paga instantáneamente?

**Investigación**: La secuencia de acciones en `AssignNextHandler` es:
1. HTTP POST → crear orden (retorna `orderId`)
2. `entry.Assign(seatId, orderId)`
3. `IWaitlistRepository.UpdateAsync(entry)` — persiste `Assigned` + `OrderId`
4. Enviar correo

El evento `payment-succeeded` es procesado por `WaitlistPaymentSucceededConsumer` que llama `IWaitlistRepository.GetByOrderId(orderId)`. Si la entrada no está persistida aún (paso 3 no completado), `GetByOrderId` retorna `null`.

**Decisión**: Si `GetByOrderId` retorna `null`, el consumer ignora el evento sin error. Este es el guard de idempotencia inverso (evento llegó antes de la persistencia). La probabilidad es muy baja (< 1ms window). En at-least-once, si el `payment-succeeded` llega de nuevo después de la persistencia, se procesará correctamente.

---

## Resumen de Decisiones

| ID | Pregunta | Decisión |
|---|---|---|
| R-001 | `EventId` en Inventory | Agregar a `Reservation` + migración |
| R-002 | `eventId` en payload es ID de concierto? | No — renombrar a `messageId`, agregar `concertEventId` |
| R-003 | ¿Quién gestiona TTL 30 min? | Waitlist Service vía `WaitlistExpiryWorker` |
| R-004 | ¿Cómo evitar race al liberar asiento? | Worker consulta `has-pending` + fallback |
| R-005 | ¿Cómo crear orden en Ordering? | Nuevo endpoint `POST /orders/waitlist` |
| R-006 | ¿PostgreSQL o Redis? | PostgreSQL (ya en stack, ACID, sin infra nueva) |
| R-007 | Idempotencia en consumers | Guard por estado + constraint DB |
| R-008 | ¿Catalog tiene endpoint de disponibilidad? | Usar `GET /events/{id}/seats?status=available` |
| R-009 | ¿`payment-succeeded` antes de persistencia? | `GetByOrderId` retorna null → ignorar |
