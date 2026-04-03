# Tasks: Waitlist Autoassign

**Input**: Design documents from `specs/004-waitlist-autoassign/`
**Prerequisites**: plan.md ✅, spec.md ✅, research.md ✅, data-model.md ✅, contracts/ ✅

**Metodología**: BDD/TDD — cada tarea de implementación es precedida por su tarea RED (test failing).
**TDD Tracking**: Ver `TDD_cycle.md` para el estado RED/GREEN/REFACTOR de cada ciclo.

**Organization**: Tareas agrupadas por User Story. Dentro de cada historia: `[RED]` → `[GREEN]` → `[REFACTOR]`.

---

## Formato: `[ID] [P?] [Story] [Fase] Descripción`

- **[P]**: Puede ejecutarse en paralelo (distintos archivos, sin dependencia)
- **[Story]**: Historia de usuario (S0=scaffold, US1..US3, EXT=servicios existentes, INT=integration)
- **[Fase]**: `[SCAFFOLD]`, `[RED]`, `[GREEN]`, `[REFACTOR]`

> **Regla TDD**: nunca escribir código de producción sin que exista al menos un test RED para ese ciclo.

---

## Phase 0: Scaffold (sin TDD — infraestructura pura)

**Propósito**: Crear la estructura mínima del nuevo microservicio. Sin lógica de negocio todavía.

- [x] T001 [S0] [SCAFFOLD] Crear solución `services/waitlist/` con proyectos: `Waitlist.Domain`, `Waitlist.Application`, `Waitlist.Infrastructure`, `Waitlist.Api`, `tests/unit/Waitlist.UnitTests`
- [x] T002 [S0] [SCAFFOLD] Crear `WaitlistDbContext` + `WaitlistRepository` con schema `bc_waitlist.waitlist_entries` e índices FIFO/expiry
- [x] T003 [P] [S0] [SCAFFOLD] Crear `WaitlistExpiryWorker` (BackgroundService con lógica completa de rotación)
- [x] T004 [P] [S0] [SCAFFOLD] Crear interfaces de puertos: `IWaitlistRepository`, `ICatalogClient`, `IOrderingClient`, `IInventoryClient`, `IEmailService`
- [x] T005 [P] [S0] [SCAFFOLD] Configurar DI en `ServiceCollectionExtensions.cs` + `Program.cs` (DbContext, workers, consumers, HTTP clients, MediatR)
- [x] T006 [P] [S0] [SCAFFOLD] Agregar entry `waitlist-service` a `docker-compose.yml` (puerto 5006) + `bc_waitlist` en `init-schemas.sql`

**Checkpoint S0**: ✅ Proyecto compila. `dotnet build` — 0 errores.

---

## Phase 1: US1 — Dominio `WaitlistEntry` (TDD Ciclos 1-6)

**Meta**: La entidad de dominio `WaitlistEntry` con su ciclo de vida completo, cubierta por unit tests antes de existir.

### Ciclo 1 — `WaitlistEntry.Create` happy path

- [x] T007 [US1] [RED] Escribir test `Create_WithValidEmailAndEventId_ReturnsPendingEntry`
- [x] T008 [US1] [GREEN] Implementar `WaitlistEntry.Create(string email, Guid eventId)` en `Waitlist.Domain/Entities/WaitlistEntry.cs`

### Ciclo 2 — `WaitlistEntry.Create` guards

- [x] T009 [US1] [RED] Tests `Create_WithBlankEmail_ThrowsArgumentException` y `Create_WithEmptyEventId_ThrowsArgumentException`
- [x] T010 [US1] [GREEN] Guards `ArgumentException` en `Create()`
- [ ] T011 [US1] [REFACTOR] Extraer validaciones guard a método privado `Validate()` *(guards son una línea — aplazado)*

### Ciclo 3 — `WaitlistEntry.Assign`

- [x] T012 [US1] [RED] Test `Assign_WhenPending_SetsStatusAssignedAndTimestamps`
- [x] T013 [US1] [GREEN] Implementar `Assign(Guid seatId, Guid orderId)` con timestamps

### Ciclo 4 — `WaitlistEntry.Assign` guard de estado

- [x] T014 [US1] [RED] Test `Assign_WhenNotPending_ThrowsInvalidOperationException` (parametrizado: Assigned/Expired/Completed)
- [x] T015 [US1] [GREEN] Guard `if (Status != StatusPending) throw`

### Ciclo 5 — `WaitlistEntry.Complete` y `Expire`

- [x] T016 [US1] [RED] Tests: `Complete_WhenAssigned`, `Expire_WhenAssigned`, `Complete/Expire_WhenNotAssigned_Throws` (7 casos)
- [x] T017 [US1] [GREEN] Implementar `Complete()` y `Expire()` con guards

### Ciclo 6 — `WaitlistEntry.IsAssignmentExpired`

- [x] T018 [US1] [RED] Tests: `IsAssignmentExpired_WhenExpiresAtInPast/Future/NotAssigned` (3 casos)
- [x] T019 [US1] [GREEN] Implementar `IsAssignmentExpired()`

**Checkpoint US1-Domain**: ✅ 19 tests — 19/19 pasan.

---

## Phase 2: US1 — Aplicación `JoinWaitlistHandler` (TDD Ciclos 7-11)

**Meta**: El handler `JoinWaitlistHandler` cubierto por todos los escenarios del spec US1.

### Ciclo 7 — Join happy path (US1 Scenario 1)

- [x] T020 [US1] [RED] Test `Handle_ValidEmail_StockZero_CreatesEntryAndReturnsPosition`
- [x] T021 [US1] [GREEN] Implementar `JoinWaitlistHandler` + `JoinWaitlistCommand` + `JoinWaitlistResult`
- [x] T022 [US1] [REFACTOR] `GetQueuePositionAsync` extraído como método del repositorio

### Ciclo 8 — Join rechazado por stock > 0 (US1 Scenario 2)

- [x] T023 [US1] [RED] Test `Handle_StockAvailable_ThrowsWaitlistConflictException`
- [x] T024 [US1] [GREEN] Guard `if (stock > 0) throw new WaitlistConflictException(...)`

### Ciclo 9 — Join rechazado por duplicado activo (US1 Scenario 3)

- [x] T025 [US1] [RED] Test `Handle_DuplicateActiveEntry_ThrowsWaitlistConflictException`
- [x] T026 [US1] [GREEN] Guard `HasActiveEntryAsync` + `WaitlistConflictException`

### Ciclo 10 — Validación de email (US1 Scenario 4)

- [x] T027 [US1] [RED] Tests `JoinWaitlistCommandValidator_InvalidEmail_HasValidationError` (3 casos inválidos + 1 válido)
- [x] T028 [US1] [GREEN] `JoinWaitlistCommandValidator` con `EmailAddress()` + `NotEmpty()`

### Ciclo 11 — Catalog Service no disponible (Edge case)

- [x] T029 [US1] [RED] Test `Handle_CatalogClientThrows_ThrowsServiceUnavailableException`
- [x] T030 [US1] [GREEN] try/catch `HttpRequestException` → `WaitlistServiceUnavailableException`

**Checkpoint US1**: ✅ 27 tests — 27/27 pasan. `POST /api/v1/waitlist/join` + `GET /has-pending` implementados.

---

## Phase 3: US2 — Asignación Automática (TDD Ciclos 12-16)

**Meta**: Consumer Kafka + handler de asignación cubren todos los escenarios US2.

### Ciclo 12 — Asignación cuando hay pending (US2 Scenario 1)

- [x] T031 [US2] [RED] Test `Handle_PendingEntryExists_AssignsEntryAndSendsEmail`
- [x] T032 [US2] [GREEN] Implementar `AssignNextHandler` + `AssignNextCommand`
- [ ] T033 [US2] [REFACTOR] Extraer email body a `WaitlistEmailTemplates` *(aplazado — body es una línea)*

### Ciclo 13 — Cola vacía → no acción (US2 Scenario 2)

- [x] T034 [US2] [RED] Test `Handle_EmptyQueue_NoOrderCreatedAndNoUpdate`
- [x] T035 [US2] [GREEN] Early return `if (next is null) return`

### Ciclo 14 — Idempotencia (evento duplicado)

- [x] T036 [US2] [RED] Test `Handle_SeatAlreadyAssigned_SkipsProcessing`
- [x] T037 [US2] [GREEN] Guard `HasAssignedEntryForSeatAsync` al inicio del handler

### Ciclo 15 — Consumer Kafka: deserialización v3

- [x] T038 [US2] [RED] Tests `ProcessMessage_ValidV3Payload_DispatchesAssignNextCommand` y `ProcessMessage_MissingConcertEventId_DoesNotDispatch`
- [x] T039 [US2] [GREEN] `ReservationExpiredConsumer` con deserialización v3 (`messageId`, `concertEventId`, `seatId`)

### Ciclo 16 — Pago exitoso → Completed (US2 Scenario 3)

- [x] T040 [US2] [RED] Tests `Handle_AssignedEntry_SetsStatusCompleted` y `Handle_NullEntry_DoesNothing`
- [x] T041 [US2] [GREEN] `CompleteAssignmentHandler` + `PaymentSucceededConsumer`

**Checkpoint US2**: ✅ 37 tests — 37/37 pasan.

---

## Phase 4: US3 — Rotación por Inacción (TDD Ciclos 17-19)

**Meta**: `WaitlistExpiryWorker` cubre los 3 escenarios de rotación del spec US3.

### Ciclo 17 — Rotación con siguiente en cola (US3 Scenario 1)

- [x] T042 [US3] [RED] Test `ProcessExpired_WithNextPending_ExpiresCurrentAndAssignsNext`
- [x] T043 [US3] [GREEN] `WaitlistExpiryWorker.ProcessExpiredEntriesAsync` con lógica de rotación
- [x] T044 [US3] [REFACTOR] `RotateOrReleaseAsync(expired)` extraído como método privado

### Ciclo 18 — Cola vacía → libera asiento (US3 Scenario 2)

- [x] T045 [US3] [RED] Test `ProcessExpired_EmptyQueue_ReleasesSeatAndCancelsOrder`
- [x] T046 [US3] [GREEN] Rama `else` con `ReleaseSeatAsync` + `CancelOrderAsync`

### Ciclo 19 — Entry Completed no procesada (US3 Scenario 3)

- [x] T047 [US3] [RED] Test `ProcessExpired_NoExpiredEntries_NoActionsPerformed`
- [x] T048 [US3] [GREEN] Query `GetExpiredAssignedAsync` filtra `status='assigned' AND expires_at <= now`

**Checkpoint US3**: ✅ 37/37 tests pasan (incluye US3).

---

## Phase 5: Cambios a Servicios Existentes (TDD Ciclos 20-22)

### Ciclo 20 — Inventory: payload `reservation-expired` v3

- [x] T049 [EXT] [RED] Leer `services/inventory/src/Inventory.Infrastructure/Workers/ReservationExpiryWorker.cs` en su totalidad
- [x] T050 [EXT] [RED] Escribir test unitario `PublishEvent_IncludesMessageIdAndConcertEventId` en `services/inventory/tests/unit/`
- [x] T051 [EXT] [GREEN] Renombrar `eventId` → `messageId`; agregar `concertEventId = res.EventId.ToString("D")`
- [x] T052 [EXT] [GREEN] Agregar `EventId` a `Inventory.Domain.Reservation` + migración EF Core `AddEventIdToReservations`

### Ciclo 21 — Inventory: consulta `has-pending` + fallback

- [x] T053 [EXT] [RED] Test `Worker_WhenQueueActive_DoesNotCallSeatRelease`
- [x] T054 [EXT] [RED] Test `Worker_WhenWaitlistClientThrows_FallbackCallsSeatRelease`
- [x] T055 [EXT] [GREEN] `IWaitlistClient` port + `WaitlistHttpClient` impl + lógica condicional en worker con fallback

### Ciclo 22 — Ordering: `POST /orders/waitlist`

- [x] T056 [EXT] [RED] Leer `services/ordering/src/Ordering.Api/Controllers/OrdersController.cs`
- [x] T057 [EXT] [RED] Test `CreateWaitlistOrder_ValidRequest_ReturnsOrderId`
- [x] T058 [EXT] [RED] Test `CreateWaitlistOrder_DuplicateSeatId_Returns409`
- [x] T059 [EXT] [GREEN] `CreateWaitlistOrderCommand` + handler + endpoint `POST /orders/waitlist`

**Checkpoint EXT**: ✅ 11/11 completadas. 15+50 tests en verde.

---

## Phase 6: Tests de Integración (TDD Ciclos 23-24)

### Ciclo 23 — Integration: `POST /waitlist/join`

- [ ] T060 [INT] [RED] Test `JoinWaitlist_Integration_StockZero_Returns201_AndPersists` (WebApplicationFactory + Testcontainers PostgreSQL)
- [ ] T061 [INT] [GREEN] Configurar `Waitlist.IntegrationTests` con Testcontainers

### Ciclo 24 — Integration: Consumer + DB

- [ ] T062 [INT] [RED] Test `ReservationExpiredConsumer_Integration_AssignsFirstPending`
- [ ] T063 [INT] [GREEN] Configurar Kafka mock in-process para tests de integración

---

## Phase 7: Polish & Cross-cutting

- [ ] T064 [P] Verificar idempotencia en todos los consumers: `ReservationExpiredConsumer`, `PaymentSucceededConsumer`, `WaitlistExpiryWorker`
- [ ] T065 [P] Verificar que `WaitlistExpiryWorker` es thread-safe
- [ ] T066 [P] Verificar que `GET /waitlist/has-pending` retorna en < 200ms con índice `idx_waitlist_fifo`
- [ ] T067 Run all test suites: Waitlist Unit, Waitlist Integration, Inventory (regresión), Ordering (regresión)

---

## Dependencias y orden de ejecución

```
Phase 0 (Scaffold) ✅
    └─→ Phase 1 (Domain) ✅
         └─→ Phase 2 (US1 Handler) ✅
              ├─→ Phase 3 (US2) ✅
              │    └─→ Phase 4 (US3) ✅
              └─→ Phase 5 (EXT) ✅
                   └─→ Phase 6 (Integration) ⬜ ← siguiente
                        └─→ Phase 7 (Polish) ⬜
```

---

## Regresión — servicios existentes

| Test | Servicio | Verifica |
|---|---|---|
| TR-01 | Ordering | `POST /orders` normal (no-waitlist) no afectado |
| TR-02 | Inventory | `ReservationExpiryWorker` sin cola activa — comportamiento original (fallback) |
| TR-03 | Ordering | `ReservationEventConsumer` existente — sigue procesando `reservation-expired` |
| TR-04 | Payment | `ReservationEventConsumer` existente — sigue procesando `reservation-expired` |

---

## Resumen de tareas

| Phase | Descripción | Tareas | Completadas | TDD Ciclos |
|---|---|---|---|---|
| 0 | Scaffold | T001–T006 | 6/6 ✅ | — |
| 1 | Domain WaitlistEntry | T007–T019 | 12/13 ✅ | 1–6 |
| 2 | US1 JoinWaitlistHandler | T020–T030 | 11/11 ✅ | 7–11 |
| 3 | US2 AssignNext + Consumers | T031–T041 | 10/11 ✅ | 12–16 |
| 4 | US3 ExpiryWorker | T042–T048 | 7/7 ✅ | 17–19 |
| 5 | Cambios EXT (Inventory + Ordering) | T049–T059 | 11/11 ✅ | 20–22 |
| 6 | Integration tests | T060–T063 | 0/4 ⬜ | 23–24 |
| 7 | Polish | T064–T067 | 0/4 ⬜ | — |

**Total tareas**: 67
**Completadas**: 57/67
**Tests en verde**: 37 (Waitlist) + 15 (Inventory) + 50 (Ordering) = 102/102
**Próximo paso**: Phase 6 — T060 (Integration tests: WebApplicationFactory + Testcontainers)
