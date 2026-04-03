# TDD Cycle Tracker: Waitlist Autoassign

**Feature Branch**: `004-waitlist-autoassign`
**Date**: 2026-04-02
**Metodología**: BDD/TDD — Red → Green → Refactor

---

## Leyenda de estados

| Icono | Estado | Descripción |
|---|---|---|
| 🔴 | RED | Test escrito, falla (código no existe) |
| 🟢 | GREEN | Código mínimo implementado, test pasa |
| ♻️ | REFACTOR | Refactorizado, tests siguen verdes |
| ⬜ | PENDIENTE | Ciclo no iniciado |

---

## Fase 0 — Scaffold (sin TDD: infraestructura pura)

| Tarea | Descripción | Estado |
|---|---|---|
| S-01 | Estructura de proyectos .NET (`Waitlist.Domain`, `.Application`, `.Infrastructure`, `.Api`) | ♻️ |
| S-02 | `WaitlistDbContext` + migración `bc_waitlist.waitlist_entries` | ⬜ |
| S-03 | `WaitlistExpiryWorker` skeleton (BackgroundService vacío) | ♻️ |
| S-04 | DI wiring en `ServiceCollectionExtensions.cs` + `Program.cs` | ⬜ |
| S-05 | Migración Inventory: `ADD COLUMN event_id` a `bc_inventory.reservations` | ⬜ |
| S-06 | Docker Compose entry `waitlist-service` (port 5006) | ⬜ |

---

## Ciclos TDD — US1: Registro en Lista de Espera

### Ciclo 1 — Dominio: `WaitlistEntry.Create` (happy path)

**BDD**: *Given* email válido + eventId, *When* `WaitlistEntry.Create(email, eventId)`, *Then* entry con status `Pending`, `RegisteredAt` set, `SeatId`/`OrderId` null.

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Create_WithValidEmailAndEventId_ReturnsPendingEntry` | ♻️ |
| 🟢 GREEN | Implementar `WaitlistEntry.Create(string email, Guid eventId)` con factory method | ♻️ |
| ♻️ REFACTOR | Guards inline — suficientemente simples | ♻️ |

---

### Ciclo 2 — Dominio: `WaitlistEntry.Create` (validaciones guard)

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Create_WithBlankEmail_ThrowsArgumentException` | ♻️ |
| 🔴 RED | `Create_WithEmptyEventId_ThrowsArgumentException` | ♻️ |
| 🟢 GREEN | Guards `ArgumentException` en `Create()` | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 3 — Dominio: `WaitlistEntry.Assign` (happy path)

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Assign_WhenPending_SetsStatusAssignedAndTimestamps` | ♻️ |
| 🟢 GREEN | Implementar `Assign(Guid seatId, Guid orderId)` | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 4 — Dominio: `WaitlistEntry.Assign` (guard de estado)

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Assign_WhenNotPending_ThrowsInvalidOperationException` (parametrizado) | ♻️ |
| 🟢 GREEN | Guard `if (Status != StatusPending) throw` | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 5 — Dominio: `WaitlistEntry.Complete` y `Expire`

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Complete_WhenAssigned_SetsStatusCompleted` | ♻️ |
| 🔴 RED | `Expire_WhenAssigned_SetsStatusExpired` | ♻️ |
| 🔴 RED | `Complete/Expire_WhenNotAssigned_ThrowsInvalidOperationException` | ♻️ |
| 🟢 GREEN | `Complete()` + `Expire()` con guards | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 6 — Dominio: `WaitlistEntry.IsAssignmentExpired`

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | 3 tests de `IsAssignmentExpired` (past/future/pending) | ♻️ |
| 🟢 GREEN | Implementar `IsAssignmentExpired()` | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 7 — `JoinWaitlistHandler` — US1 Scenario 1 (stock=0 → 201)

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_ValidEmail_StockZero_CreatesEntryAndReturnsPosition` | ♻️ |
| 🟢 GREEN | `JoinWaitlistHandler` + ports + exceptions | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 8 — `JoinWaitlistHandler` — US1 Scenario 2 (stock > 0 → 409)

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_StockAvailable_ThrowsWaitlistConflictException` | ♻️ |
| 🟢 GREEN | Guard stock > 0 | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 9 — `JoinWaitlistHandler` — US1 Scenario 3 (duplicado → 409)

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_DuplicateActiveEntry_ThrowsWaitlistConflictException` | ♻️ |
| 🟢 GREEN | Guard `HasActiveEntryAsync` | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 10 — `JoinWaitlistCommandValidator` — US1 Scenario 4 (email inválido)

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `JoinWaitlistCommandValidator_InvalidEmail_HasValidationError` | ♻️ |
| 🟢 GREEN | FluentValidation `EmailAddress()` rule | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 11 — `JoinWaitlistHandler` — Catalog 503

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_CatalogClientThrows_ThrowsServiceUnavailableException` | ♻️ |
| 🟢 GREEN | try/catch → `WaitlistServiceUnavailableException` | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

## Ciclos TDD — US2: Asignación Automática

### Ciclo 12 — Aplicación: `AssignNextHandler` — Escenario US2.1 (hay pending → asigna)

**BDD**: *Given* `IWaitlistRepository.GetNextPending(eventId)` retorna entry, *And* `IOrderingClient.CreateWaitlistOrder` retorna `orderId`, *When* `AssignNextHandler.Handle(command)`, *Then* entry pasa a `Assigned`, `UpdateAsync` llamado, correo enviado.

**Spec ref**: US2 Scenario 1

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_PendingEntryExists_AssignsAndSendsEmail` | ⬜ |
| 🟢 GREEN | Implementar `AssignNextHandler` con mocks `IWaitlistRepository`, `IOrderingClient`, `IEmailService` | ⬜ |
| ♻️ REFACTOR | Extraer lógica de notificación a método privado | ⬜ |

---

### Ciclo 13 — Aplicación: `AssignNextHandler` — Escenario US2.2 (cola vacía → no acción)

**BDD**: *Given* `GetNextPending` retorna null, *When* handle, *Then* no se llama a `IOrderingClient` ni `IWaitlistRepository.UpdateAsync`.

**Spec ref**: US2 Scenario 2

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_EmptyQueue_NoActionTaken` | ⬜ |
| 🟢 GREEN | Agregar early return `if (next == null) return` | ⬜ |
| ♻️ REFACTOR | — | ⬜ |

---

### Ciclo 14 — Aplicación: `AssignNextHandler` — Idempotencia (reserv-expired duplicado)

**BDD**: *Given* ya existe una entrada `Assigned` para el `seatId`, *When* llega segundo `reservation-expired`, *Then* no se crea segunda orden ni segunda asignación.

**Spec ref**: Edge Cases — dos `reservation-expired` para mismo asiento

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_SeatAlreadyAssigned_SkipsProcessing` | ⬜ |
| 🟢 GREEN | Agregar guard `HasAssignedEntryForSeat(seatId)` al inicio del handler | ⬜ |
| ♻️ REFACTOR | — | ⬜ |

---

### Ciclo 15 — Consumer: `ReservationExpiredConsumer` — deserialización + dispatch

**BDD**: *Given* mensaje Kafka `reservation-expired` v3 con `concertEventId`, *When* consumer lo procesa, *Then* envía `AssignNextCommand` con `concertEventId` y `seatId` correctos.

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Consume_ValidMessage_DispatchesAssignNextCommand` | ⬜ |
| 🟢 GREEN | Implementar `ReservationExpiredConsumer` con deserialización de v3 payload | ⬜ |
| ♻️ REFACTOR | — | ⬜ |

---

### Ciclo 16 — Aplicación: `CompleteAssignmentHandler` — Escenario US2.3 (pago exitoso → Completed)

**BDD**: *Given* `IWaitlistRepository.GetByOrderId(orderId)` retorna entry `Assigned`, *When* `CompleteAssignmentHandler.Handle`, *Then* entry pasa a `Completed`, `UpdateAsync` llamado.

**Spec ref**: US2 Scenario 3

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_AssignedEntry_CompletesSuccessfully` | ⬜ |
| 🔴 RED | `Handle_NullEntry_DoesNothing` (idempotencia) | ⬜ |
| 🟢 GREEN | Implementar `CompleteAssignmentHandler` | ⬜ |
| ♻️ REFACTOR | — | ⬜ |

---

## Ciclos TDD — US3: Rotación por Inacción

### Ciclo 17 — Worker: `WaitlistExpiryWorker` — Escenario US3.1 (hay siguiente → rotación)

**BDD**: *Given* entry `Assigned` con `ExpiresAt` hace 1 min, *And* hay siguiente `Pending` en cola, *When* worker ejecuta, *Then* primera entry → `Expired`, segunda → `Assigned`, correo enviado a segunda.

**Spec ref**: US3 Scenario 1

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `ExecuteAsync_ExpiredAssigned_WithNextPending_RotatesAndAssigns` | ⬜ |
| 🟢 GREEN | Implementar lógica de rotación en `WaitlistExpiryWorker.ProcessExpiredAsync` | ⬜ |
| ♻️ REFACTOR | Extraer `RotateToNextAsync(expired)` como método independiente | ⬜ |

---

### Ciclo 18 — Worker: `WaitlistExpiryWorker` — Escenario US3.2 (cola vacía → libera asiento)

**BDD**: *Given* entry `Assigned` expirada, *And* cola vacía, *When* worker ejecuta, *Then* entry → `Expired`, `IInventoryClient.ReleaseSeat(seatId)` llamado, `IOrderingClient.CancelOrder(orderId)` llamado.

**Spec ref**: US3 Scenario 2

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `ExecuteAsync_ExpiredAssigned_EmptyQueue_ReleasesSeatAndCancelsOrder` | ⬜ |
| 🟢 GREEN | Agregar rama `else` con `IInventoryClient.ReleaseSeat` + `IOrderingClient.CancelOrder` | ⬜ |
| ♻️ REFACTOR | — | ⬜ |

---

### Ciclo 19 — Worker: `WaitlistExpiryWorker` — Escenario US3.3 (ya completado → no acción)

**BDD**: *Given* entry en `Completed`, *When* worker ejecuta (filtro no la debería incluir), *Then* no se realiza ninguna acción.

**Spec ref**: US3 Scenario 3

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `ExecuteAsync_CompletedEntry_IsIgnored` | ⬜ |
| 🟢 GREEN | Confirmar que query del worker filtra solo `status = 'assigned' AND expires_at <= now` | ⬜ |
| ♻️ REFACTOR | — | ⬜ |

---

## Ciclos TDD — Cambios a Servicios Existentes

### Ciclo 20 — Inventory: `reservation-expired` v3 payload

**BDD**: *Given* reserva expirada con `EventId` set, *When* `ReservationExpiryWorker` publica, *Then* payload tiene `messageId` (no `eventId`), `concertEventId = reservation.EventId`.

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `ProcessExpiredReservations_PublishesPayload_WithMessageIdAndConcertEventId` | ♻️ |
| 🟢 GREEN | Renombrar `eventId` → `messageId`; agregar `concertEventId = res.EventId`; `EventId` en `Reservation`; migración EF | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 21 — Inventory: `GET /waitlist/has-pending` consulta + fallback

**BDD**: *Given* Waitlist responde `hasPending=true`, *When* worker detecta reserva expirada, *Then* NO llama `seat.Release()`.
**BDD alt**: *Given* Waitlist no responde (timeout), *When* worker detecta reserva expirada, *Then* SÍ llama `seat.Release()` (fallback).

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `ProcessExpiredReservations_WhenQueueActive_DoesNotReleaseSeat` | ♻️ |
| 🔴 RED | `ProcessExpiredReservations_WhenWaitlistClientThrows_FallbackReleasesSeat` | ♻️ |
| 🟢 GREEN | `IWaitlistClient` port + `WaitlistHttpClient` impl + `ShouldReleaseSeatAsync` con fallback | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

### Ciclo 22 — Ordering: `POST /orders/waitlist` endpoint

**BDD**: *Given* `{ seatId, price, guestToken }` válidos, *When* `POST /orders/waitlist`, *Then* orden creada en estado `pending`, retorna `{ orderId }`.

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `Handle_ValidRequest_CreatesOrderAndReturnsOrderId` | ♻️ |
| 🔴 RED | `Handle_DuplicateSeatId_ThrowsDuplicateSeatOrderException` | ♻️ |
| 🟢 GREEN | `CreateWaitlistOrderCommand` + `CreateWaitlistOrderHandler` + `DuplicateSeatOrderException` + endpoint | ♻️ |
| ♻️ REFACTOR | — | ♻️ |

---

## Ciclos TDD — Integración

### Ciclo 23 — Integration: `POST /waitlist/join` (DB real)

**BDD**: *Given* DB real (Testcontainers), *And* stock=0 (mock Catalog HTTP), *When* `POST /api/v1/waitlist/join`, *Then* `201`, fila en `bc_waitlist.waitlist_entries`.

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `JoinWaitlist_Integration_StockZero_Returns201_AndPersists` | ⬜ |
| 🟢 GREEN | Configurar `WebApplicationFactory` + Testcontainers PostgreSQL | ⬜ |
| ♻️ REFACTOR | Extraer builders de fixtures a helpers reutilizables | ⬜ |

---

### Ciclo 24 — Integration: Consumer + DB (auto-asignación)

**BDD**: *Given* DB con entry `Pending`, *When* consumer procesa `reservation-expired` (mock Kafka), *Then* entry pasa a `Assigned` en DB, mock `IOrderingClient` fue llamado.

| Fase | Descripción | Estado |
|---|---|---|
| 🔴 RED | `ReservationExpiredConsumer_Integration_AssignsFirstPending` | ⬜ |
| 🟢 GREEN | Test con Testcontainers + mock `IOrderingClient` + mock `IEmailService` | ⬜ |
| ♻️ REFACTOR | — | ⬜ |

---

## Resumen de ciclos

| Ciclo | Historia | Tipo | Tests | Estado |
|---|---|---|---|---|
| 1 | US1 | Unit/Domain | `Create` happy path | ⬜ |
| 2 | US1 | Unit/Domain | `Create` guards | ⬜ |
| 3 | US1 | Unit/Domain | `Assign` happy path | ⬜ |
| 4 | US1 | Unit/Domain | `Assign` guard estado | ⬜ |
| 5 | US1 | Unit/Domain | `Complete` + `Expire` | ⬜ |
| 6 | US1 | Unit/Domain | `IsAssignmentExpired` | ⬜ |
| 7 | US1 | Unit/App | Join — stock=0 → 201 | ⬜ |
| 8 | US1 | Unit/App | Join — stock>0 → 409 | ⬜ |
| 9 | US1 | Unit/App | Join — duplicado → 409 | ⬜ |
| 10 | US1 | Unit/App | Join — email inválido → 400 | ⬜ |
| 11 | US1 | Unit/App | Join — Catalog 503 | ⬜ |
| 12 | US2 | Unit/App | Assign — hay pending | ⬜ |
| 13 | US2 | Unit/App | Assign — cola vacía | ⬜ |
| 14 | US2 | Unit/App | Assign — idempotencia | ⬜ |
| 15 | US2 | Unit/Infra | Consumer — deserialización v3 | ⬜ |
| 16 | US2 | Unit/App | Complete — pago exitoso | ⬜ |
| 17 | US3 | Unit/Worker | Worker — rotación | ⬜ |
| 18 | US3 | Unit/Worker | Worker — libera asiento | ⬜ |
| 19 | US3 | Unit/Worker | Worker — ya completado | ⬜ |
| 20 | Inventory | Unit | Payload v3 | ⬜ |
| 21 | Inventory | Unit | `has-pending` + fallback | ⬜ |
| 22 | Ordering | Unit | `POST /orders/waitlist` | ⬜ |
| 23 | US1 | Integration | Join — DB real | ⬜ |
| 24 | US2 | Integration | Consumer — DB + mock | ⬜ |

**Total ciclos**: 24 | **Total tests RED**: ~38
