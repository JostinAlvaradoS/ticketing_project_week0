# Guía de Automatización — Feature 004: Waitlist Autoassign

**Feature Branch**: `004-waitlist-autoassign`
**Fecha**: 2026-04-03
**Referencias**:
- [spec.md](./spec.md) — criterios de aceptación y reglas de negocio
- [plan.md](./plan.md) — flujos técnicos y arquitectura
- [data-model.md](./data-model.md) — entidades y ciclo de vida
- [asds.md](../../asds.md) — diseño original del autor
- [contracts/](./contracts/) — schemas JSON de eventos y endpoints

---

## Índice

1. [Estrategia de testing](#1-estrategia-de-testing)
2. [Entorno y prerequisitos](#2-entorno-y-prerequisitos)
3. [Fixtures SQL (semillas de datos)](#3-fixtures-sql-semillas-de-datos)
4. [Hooks del ciclo de vida](#4-hooks-del-ciclo-de-vida)
5. [Page Objects (POM)](#5-page-objects-pom)
6. [Tasks — Screenplay Pattern](#6-tasks--screenplay-pattern)
7. [Questions — Screenplay Pattern](#7-questions--screenplay-pattern)
8. [Escenarios Gherkin completos](#8-escenarios-gherkin-completos)
9. [API-Screenplay: guía de implementación por escenario](#9-api-screenplay-guía-de-implementación-por-escenario)
10. [Kafka Integration: guía de implementación por escenario](#10-kafka-integration-guía-de-implementación-por-escenario)
11. [UI Screenplay: guía de implementación por escenario](#11-ui-screenplay-guía-de-implementación-por-escenario)
12. [Contratos de referencia](#12-contratos-de-referencia)
13. [Matriz de escenarios vs método de test](#13-matriz-de-escenarios-vs-método-de-test)

---

## 1. Estrategia de testing

```
┌─────────────────────────────────────────────────────────────────────┐
│  Capa UI — Screenplay + POM                                         │
│  Herramienta: Playwright / Selenium + Serenity-JS o Boa Constrictor │
│  Scope: flujos visibles desde el navegador (registro modal, errores)│
├─────────────────────────────────────────────────────────────────────┤
│  Capa API — API-Screenplay / REST-Assured / Supertest               │
│  Scope: contratos HTTP directos, sin UI                             │
│  Cubre: US1 completo + consultas internas + regresión               │
├─────────────────────────────────────────────────────────────────────┤
│  Capa Kafka — Integration tests con producer/consumer directo       │
│  Scope: flujos event-driven (US2 y US3)                             │
│  Cubre: asignación automática, pago exitoso, rotación worker        │
└─────────────────────────────────────────────────────────────────────┘
```

### Principio de responsabilidad por capa

| Capa | Qué valida | Qué NO valida |
|---|---|---|
| UI Screenplay | Flujo del usuario en el navegador, visibilidad de elementos, mensajes de éxito/error | Lógica interna de servicios |
| API-Screenplay | Contratos HTTP, códigos de respuesta, estructura del body, persistencia en DB | Comportamiento visual |
| Kafka Integration | Estado resultante tras consumir eventos, idempotencia, rotación del worker | Presentación en UI |

---

## 2. Entorno y prerequisitos

### Servicios requeridos (Docker Compose)

| Servicio | Puerto | Rol |
|---|---|---|
| `waitlist-service` | 5006 | Servicio bajo prueba principal |
| `ordering-service` | 5002 | Dependencia HTTP (crear/cancelar órdenes) |
| `inventory-service` | 5003 | Dependencia HTTP (liberar asientos) |
| `catalog-service` | 5001 | Dependencia HTTP (verificar stock) |
| `postgres` | 5432 | DB compartida con schemas separados |
| `kafka` | 9092 | Broker de mensajería |
| `frontend` | 3000 | Solo para tests UI |

### Variables de entorno para el runner de tests

```env
WAITLIST_BASE_URL=http://localhost:5006
ORDERING_BASE_URL=http://localhost:5002
INVENTORY_BASE_URL=http://localhost:5003
CATALOG_BASE_URL=http://localhost:5001
FRONTEND_BASE_URL=http://localhost:3000
KAFKA_BOOTSTRAP=localhost:9092
DB_CONNECTION=Host=localhost;Port=5432;Database=ticketing;Username=admin;Password=admin
```

### UUIDs de datos maestros (constantes de test)

Definir como constantes reutilizables en un archivo `TestConstants` o equivalente:

```
EVENT_ID_AGOTADO     = "c9d0e1f2-0000-0000-0000-000000000001"
EVENT_ID_DISPONIBLE  = "c9d0e1f2-0000-0000-0000-000000000002"
SEAT_ID_TEST         = "e5f6a7b8-0000-0000-0000-000000000001"
RESERVATION_ID_TEST  = "a1b2c3d4-0000-0000-0000-000000000001"
EMAIL_PRIMERO        = "primero@example.com"
EMAIL_SEGUNDO        = "segundo@example.com"
EMAIL_TESTER         = "tester@example.com"
```

---

## 3. Fixtures SQL (semillas de datos)

Ejecutar contra la base de datos de test. Organizados por precondición de escenario.

### FIX-01: Evento agotado (stock = 0)
> Precondición para US1 escenarios 1, 3, 4, 5 y todos los US2/US3

```sql
-- Todos los seats del evento agotado pasan a reservado
UPDATE bc_catalog.seats
SET status = 'reserved'
WHERE event_id = 'c9d0e1f2-0000-0000-0000-000000000001';
```

### FIX-02: Evento con stock disponible (stock > 0)
> Precondición para US1 escenario 2

```sql
-- Al menos un seat disponible
UPDATE bc_catalog.seats
SET status = 'available'
WHERE event_id = 'c9d0e1f2-0000-0000-0000-000000000002'
LIMIT 1;
```

### FIX-03: Entry pending existente (para test de duplicado)
> Precondición para US1 escenario 3

```sql
INSERT INTO bc_waitlist.waitlist_entries
    (id, email, event_id, status, registered_at)
VALUES
    (gen_random_uuid(),
     'tester@example.com',
     'c9d0e1f2-0000-0000-0000-000000000001',
     'pending',
     NOW());
```

### FIX-04: Cola con 3 entries pending (para test de posición FIFO)

```sql
INSERT INTO bc_waitlist.waitlist_entries
    (id, email, event_id, status, registered_at)
VALUES
    (gen_random_uuid(), 'primero@example.com',  'c9d0e1f2-0000-0000-0000-000000000001', 'pending', NOW() - INTERVAL '3 minutes'),
    (gen_random_uuid(), 'segundo@example.com',  'c9d0e1f2-0000-0000-0000-000000000001', 'pending', NOW() - INTERVAL '2 minutes'),
    (gen_random_uuid(), 'tercero@example.com',  'c9d0e1f2-0000-0000-0000-000000000001', 'pending', NOW() - INTERVAL '1 minute');
```

### FIX-05: Entry pending (para trigger de asignación automática via Kafka)
> Precondición para US2 escenario 1

```sql
INSERT INTO bc_waitlist.waitlist_entries
    (id, email, event_id, status, registered_at)
VALUES
    (gen_random_uuid(),
     'primero@example.com',
     'c9d0e1f2-0000-0000-0000-000000000001',
     'pending',
     NOW());
```

### FIX-06: Entry assigned con orderId conocido (para test de pago exitoso)
> Precondición para US2 escenario 3

```sql
INSERT INTO bc_waitlist.waitlist_entries
    (id, email, event_id, seat_id, order_id, status, registered_at, assigned_at, expires_at)
VALUES
    (gen_random_uuid(),
     'pagador@example.com',
     'c9d0e1f2-0000-0000-0000-000000000001',
     'e5f6a7b8-0000-0000-0000-000000000001',
     'f1a2b3c4-0000-0000-0000-000000000001',   -- orderId conocido
     'assigned',
     NOW() - INTERVAL '5 minutes',
     NOW() - INTERVAL '2 minutes',
     NOW() + INTERVAL '28 minutes');
```

### FIX-07: Entry assigned VENCIDA + entry pending siguiente (para test de rotación US3.1)

```sql
INSERT INTO bc_waitlist.waitlist_entries
    (id, email, event_id, seat_id, order_id, status, registered_at, assigned_at, expires_at)
VALUES
    -- entrada expirada
    ('aaaaaaaa-0000-0000-0000-000000000001',
     'primero@example.com',
     'c9d0e1f2-0000-0000-0000-000000000001',
     'e5f6a7b8-0000-0000-0000-000000000001',
     'f1a2b3c4-0000-0000-0000-000000000001',
     'assigned',
     NOW() - INTERVAL '35 minutes',
     NOW() - INTERVAL '31 minutes',
     NOW() - INTERVAL '1 minute'),           -- expires_at en el pasado = vencida
    -- siguiente en cola
    (gen_random_uuid(),
     'segundo@example.com',
     'c9d0e1f2-0000-0000-0000-000000000001',
     NULL,
     NULL,
     'pending',
     NOW() - INTERVAL '30 minutes',
     NULL,
     NULL);
```

### FIX-08: Entry assigned VENCIDA SIN siguiente (para test de cola vacía US3.2)

```sql
INSERT INTO bc_waitlist.waitlist_entries
    (id, email, event_id, seat_id, order_id, status, registered_at, assigned_at, expires_at)
VALUES
    ('bbbbbbbb-0000-0000-0000-000000000001',
     'unico@example.com',
     'c9d0e1f2-0000-0000-0000-000000000001',
     'e5f6a7b8-0000-0000-0000-000000000002',
     'f1a2b3c4-0000-0000-0000-000000000002',
     'assigned',
     NOW() - INTERVAL '35 minutes',
     NOW() - INTERVAL '31 minutes',
     NOW() - INTERVAL '1 minute');
```

### FIX-CLEANUP: Limpieza general (ejecutar en AfterScenario)

```sql
DELETE FROM bc_waitlist.waitlist_entries
WHERE event_id IN (
    'c9d0e1f2-0000-0000-0000-000000000001',
    'c9d0e1f2-0000-0000-0000-000000000002'
);
```

---

## 4. Hooks del ciclo de vida

### Jerarquía de hooks

```
BeforeAll
  └── BeforeFeature (@waitlist)
        └── BeforeScenario (@waitlist-api | @waitlist-ui | @waitlist-kafka)
              └── [Escenario]
        └── AfterScenario
  └── AfterFeature
AfterAll
```

---

### `BeforeAll`

```
PROPÓSITO: Validar que el entorno esté listo antes de correr cualquier test.

ACCIONES:
  1. Verificar conectividad:
     GET http://localhost:5006/health          → esperar 200
     GET http://localhost:5001/health          → esperar 200
     GET http://localhost:5002/health          → esperar 200
     GET http://localhost:5003/health          → esperar 200

  2. Verificar conexión a Kafka:
     Listar topics → confirmar que 'reservation-expired' y 'payment-succeeded' existen

  3. Verificar conexión a DB:
     SELECT 1 FROM bc_waitlist.waitlist_entries LIMIT 1

  4. Aplicar FIX-01 y FIX-02 (estado inicial de events en Catalog)

FALLA SI: cualquier health check retorna != 200 en timeout de 30s.
```

---

### `BeforeFeature` (tag: `@waitlist`)

```
PROPÓSITO: Garantizar estado limpio de la tabla waitlist antes de la suite.

ACCIONES:
  1. Ejecutar FIX-CLEANUP
  2. Registrar en log: "Waitlist table cleared — feature suite starting"
```

---

### `BeforeScenario` (tag: `@waitlist-api`)

```
PROPÓSITO: Limpiar estado entre escenarios de API.

ACCIONES:
  1. Ejecutar FIX-CLEANUP
  2. Reset del mock/stub de Catalog si fue modificado en el escenario anterior
  3. Inicializar el objeto ApiActor con baseUrl = WAITLIST_BASE_URL
```

---

### `BeforeScenario` (tag: `@waitlist-kafka`)

```
PROPÓSITO: Garantizar que el consumer del Waitlist Service tiene offset actualizado.

ACCIONES:
  1. Ejecutar FIX-CLEANUP
  2. Crear un KafkaProducer de test configurado con KAFKA_BOOTSTRAP
  3. Crear un KafkaConsumer de test (para verificar mensajes de salida si aplica)
  4. Registrar messageId de test = gen_random_uuid() — usar en el payload del escenario
```

---

### `BeforeScenario` (tag: `@waitlist-ui`)

```
PROPÓSITO: Preparar navegador y estado de datos para UI.

ACCIONES:
  1. Ejecutar FIX-CLEANUP
  2. Ejecutar FIX-01 (evento agotado visible en UI)
  3. Inicializar WebDriver / Playwright con baseUrl = FRONTEND_BASE_URL
  4. Navegar a la página del evento agotado (EVENT_ID_AGOTADO)
```

---

### `AfterScenario` (tag: `@waitlist-api` | `@waitlist-kafka`)

```
PROPÓSITO: Limpiar efectos secundarios en servicios externos.

ACCIONES:
  1. Si el escenario creó una orden (orderId fue capturado en el contexto):
     PATCH http://localhost:5002/api/v1/orders/{orderId}/cancel
  2. Ejecutar FIX-CLEANUP
  3. Cerrar KafkaProducer/Consumer de test si existen
  4. Registrar resultado del escenario en el log de auditoría
```

---

### `AfterScenario` (tag: `@waitlist-ui`)

```
PROPÓSITO: Cerrar navegador y limpiar datos.

ACCIONES:
  1. Capturar screenshot si el escenario falló (guardar en reports/screenshots/)
  2. Cerrar WebDriver / Playwright
  3. PATCH cancelar orden si fue creada
  4. Ejecutar FIX-CLEANUP
```

---

### `AfterAll`

```
PROPÓSITO: Dejar el entorno en estado inicial.

ACCIONES:
  1. Ejecutar FIX-CLEANUP
  2. Reset de bc_catalog.seats al estado original (todos 'available' o 'reserved' según fixture base)
  3. Generar reporte final de Serenity / Allure
```

---

## 5. Page Objects (POM)

### `EventsPage`

```
Descripción: Página principal de listado/detalle de eventos.

Selectores:
  soldOutLabel        → [data-testid="sold-out"]
                        texto: "Agotado" | "Sold Out"
  joinWaitlistButton  → [data-testid="join-waitlist-btn"]
                        texto: "Unirse a lista de espera"
  buyTicketButton     → [data-testid="buy-ticket-btn"]
                        (visible solo cuando hay stock)

Precondición de visibilidad:
  joinWaitlistButton  → solo visible cuando soldOutLabel está presente
  buyTicketButton     → solo visible cuando soldOutLabel NO está presente
```

---

### `WaitlistModal`

```
Descripción: Modal que aparece al hacer clic en "Unirse a lista de espera".

Selectores:
  container           → [data-testid="waitlist-modal"] | .modal-waitlist
  emailInput          → [data-testid="waitlist-email"] | input[name="email"]
  submitButton        → [data-testid="waitlist-submit"] | button[type="submit"]
  closeButton         → [data-testid="waitlist-close"] | button.modal-close
  successMessage      → [data-testid="waitlist-success"]
  positionText        → [data-testid="waitlist-position"]
  errorMessage        → [data-testid="waitlist-error"]
  conflictMessage     → [data-testid="waitlist-conflict"]
  loadingSpinner      → [data-testid="waitlist-loading"]

Estados del modal:
  IDLE        → emailInput visible, submitButton enabled
  LOADING     → loadingSpinner visible, submitButton disabled
  SUCCESS     → successMessage visible, positionText visible
  ERROR       → errorMessage visible (validación frontend)
  CONFLICT    → conflictMessage visible (409 del backend)
```

---

## 6. Tasks — Screenplay Pattern

### `NavigateToSoldOutEvent`

```
Propósito: Ir a la página de un evento agotado.
Parámetros: eventId (string)

Pasos:
  1. actor.navigatesTo(FRONTEND_BASE_URL + "/events/" + eventId)
  2. actor.waitsFor(EventsPage.soldOutLabel).toBeVisible(timeout: 5s)
```

---

### `OpenWaitlistModal`

```
Propósito: Abrir el modal de lista de espera.
Precondición: estar en la página de un evento agotado.

Pasos:
  1. actor.waitsFor(EventsPage.joinWaitlistButton).toBeClickable(timeout: 3s)
  2. actor.clicksOn(EventsPage.joinWaitlistButton)
  3. actor.waitsFor(WaitlistModal.container).toBeVisible(timeout: 3s)
  4. actor.waitsFor(WaitlistModal.emailInput).toBeVisible(timeout: 2s)
```

---

### `SubmitWaitlistForm`

```
Propósito: Ingresar email y enviar el formulario del modal.
Parámetros: email (string)

Pasos:
  1. actor.clearsField(WaitlistModal.emailInput)
  2. actor.entersValue(email).into(WaitlistModal.emailInput)
  3. actor.clicksOn(WaitlistModal.submitButton)
  4. actor.waitsFor(WaitlistModal.loadingSpinner).toDisappear(timeout: 10s)
```

---

### `JoinWaitlist`

```
Propósito: Flujo completo de registro desde la UI (compone NavigateToSoldOutEvent + OpenWaitlistModal + SubmitWaitlistForm).
Parámetros: eventId (string), email (string)

Pasos:
  1. actor.attemptsTo(NavigateToSoldOutEvent.forEvent(eventId))
  2. actor.attemptsTo(OpenWaitlistModal)
  3. actor.attemptsTo(SubmitWaitlistForm.withEmail(email))
```

---

### `PostJoinWaitlistAPI`

```
Propósito: Llamar directamente al endpoint POST /waitlist/join (sin UI).
Parámetros: email (string), eventId (string)

Pasos:
  1. actor.callsAPI(
       method:  POST
       url:     WAITLIST_BASE_URL + "/api/v1/waitlist/join"
       headers: { "Content-Type": "application/json" }
       body:    { "email": email, "eventId": eventId }
     )
  2. actor.remembers("lastResponse", response)
  3. actor.remembers("lastStatusCode", response.statusCode)
```

---

### `CheckHasPendingAPI`

```
Propósito: Llamar GET /waitlist/has-pending.
Parámetros: eventId (string)

Pasos:
  1. actor.callsAPI(
       method:  GET
       url:     WAITLIST_BASE_URL + "/api/v1/waitlist/has-pending?eventId=" + eventId
     )
  2. actor.remembers("hasPendingResponse", response)
```

---

### `PublishReservationExpiredEvent`

```
Propósito: Producir mensaje Kafka reservation-expired.
Parámetros: messageId, reservationId, seatId, customerId, concertEventId

Pasos:
  1. kafkaProducer.send(
       topic:   "reservation-expired"
       key:     seatId
       payload: {
         "messageId":      messageId,
         "reservationId":  reservationId,
         "seatId":         seatId,
         "customerId":     customerId,
         "concertEventId": concertEventId
       }
     )
  2. actor.remembers("publishedMessageId", messageId)
  3. actor.waits(500ms)   -- dar tiempo al consumer para iniciar procesamiento
```

---

### `PublishPaymentSucceededEvent`

```
Propósito: Producir mensaje Kafka payment-succeeded.
Parámetros: paymentId, orderId, customerId, reservationId

Pasos:
  1. kafkaProducer.send(
       topic:   "payment-succeeded"
       key:     orderId
       payload: {
         "paymentId":     paymentId,
         "orderId":       orderId,
         "customerId":    customerId,
         "reservationId": reservationId
       }
     )
```

---

### `WaitForWaitlistEntryStatus`

```
Propósito: Polling a DB hasta que una entry alcance el status esperado.
Parámetros: email (string), eventId (string), expectedStatus (string), timeoutSeconds (int = 10)

Pasos:
  1. Iniciar timer
  2. Cada 500ms:
     SELECT status FROM bc_waitlist.waitlist_entries
     WHERE email = email AND event_id = eventId
     ORDER BY registered_at DESC LIMIT 1
  3. Si status == expectedStatus → continuar
  4. Si timer > timeoutSeconds → lanzar TimeoutException con mensaje descriptivo
```

---

## 7. Questions — Screenplay Pattern

### `TheLastResponseStatus`

```
Retorna: int — el HTTP status code del último response guardado en contexto del actor.
Uso: actor.asksAbout(TheLastResponseStatus.code())
```

---

### `TheWaitlistEntryInDB`

```
Retorna: WaitlistEntryDTO | null
Parámetros: email (string), eventId (string)
Query:
  SELECT id, email, event_id, seat_id, order_id, status, registered_at, assigned_at, expires_at
  FROM bc_waitlist.waitlist_entries
  WHERE email = :email AND event_id = :eventId
  ORDER BY registered_at DESC LIMIT 1
Uso: actor.asksAbout(TheWaitlistEntryInDB.forEmail(email).andEvent(eventId))
```

---

### `TheWaitlistPositionInDB`

```
Retorna: int — posición FIFO del email en la cola del evento.
Parámetros: email (string), eventId (string)
Query:
  SELECT COUNT(*) + 1 AS position
  FROM bc_waitlist.waitlist_entries
  WHERE event_id = :eventId
    AND status = 'pending'
    AND registered_at < (
      SELECT registered_at FROM bc_waitlist.waitlist_entries
      WHERE email = :email AND event_id = :eventId AND status = 'pending'
    )
Uso: actor.asksAbout(TheWaitlistPositionInDB.forEmail(email).andEvent(eventId))
```

---

### `TheWaitlistEntryCountInDB`

```
Retorna: int — cantidad de entries para un eventId y status.
Parámetros: eventId (string), status (string | null = cualquiera)
Query:
  SELECT COUNT(*) FROM bc_waitlist.waitlist_entries
  WHERE event_id = :eventId [AND status = :status]
Uso: actor.asksAbout(TheWaitlistEntryCountInDB.forEvent(eventId).withStatus("assigned"))
```

---

### `TheModalSuccessMessage`

```
Retorna: string — texto del elemento WaitlistModal.successMessage.
Uso: actor.asksAbout(TheModalSuccessMessage.text())
```

---

### `IsModalVisible`

```
Retorna: boolean — si WaitlistModal.container está visible en el DOM.
Uso: actor.asksAbout(IsModalVisible.now())
```

---

## 8. Escenarios Gherkin completos

### Feature: Sistema de Lista de Espera Inteligente

---

#### US1 — Registro en Lista de Espera

```gherkin
@waitlist @waitlist-api @US1
Feature: Registro en Lista de Espera

  Background:
    Given el entorno de pruebas está disponible
    And la tabla bc_waitlist.waitlist_entries está limpia para el evento de prueba

  @happy-path @TI-01
  Scenario: Registro exitoso cuando el evento está agotado
    Given el evento "c9d0e1f2-0000-0000-0000-000000000001" tiene stock = 0
    When el actor llama POST /api/v1/waitlist/join con email "tester@example.com" y ese eventId
    Then el sistema responde 201 Created
    And el body contiene una posición mayor o igual a 1
    And el body contiene un entryId con formato UUID válido
    And existe una entrada en bc_waitlist.waitlist_entries con status "pending" para ese email y evento

  @negative @TI-02
  Scenario: Rechazo cuando el evento tiene asientos disponibles
    Given el evento "c9d0e1f2-0000-0000-0000-000000000002" tiene stock > 0
    When el actor llama POST /api/v1/waitlist/join con email "tester@example.com" y ese eventId
    Then el sistema responde 409 Conflict
    And el body contiene un mensaje indicando que hay tickets disponibles

  @negative @TI-03
  Scenario: Rechazo por duplicado activo en la cola
    Given el evento "c9d0e1f2-0000-0000-0000-000000000001" tiene stock = 0
    And ya existe una entrada con status "pending" para "tester@example.com" en ese evento
    When el actor llama POST /api/v1/waitlist/join con email "tester@example.com" y ese eventId
    Then el sistema responde 409 Conflict
    And el body contiene un mensaje indicando que ya está en la lista de espera

  @negative @TI-04
  Scenario: Rechazo por email con formato inválido
    Given el evento "c9d0e1f2-0000-0000-0000-000000000001" tiene stock = 0
    When el actor llama POST /api/v1/waitlist/join con email "noesuncorreo" y ese eventId
    Then el sistema responde 400 Bad Request
    And el body contiene errores de validación para el campo "email"

  @edge-case @TI-05
  Scenario: Error de servicio cuando el Catalog Service no responde
    Given el Catalog Service está configurado para retornar timeout
    When el actor llama POST /api/v1/waitlist/join con email "tester@example.com" y eventId válido
    Then el sistema responde 503 Service Unavailable

  @TI-06
  Scenario: Consulta de cola activa retorna hasPending true
    Given existe al menos una entrada con status "pending" para el evento "c9d0e1f2-0000-0000-0000-000000000001"
    When el actor llama GET /api/v1/waitlist/has-pending con ese eventId
    Then el sistema responde 200 OK
    And el body contiene hasPending = true

  @TI-07
  Scenario: Consulta de cola activa retorna hasPending false
    Given no existen entradas con status "pending" para el evento "c9d0e1f2-0000-0000-0000-000000000001"
    When el actor llama GET /api/v1/waitlist/has-pending con ese eventId
    Then el sistema responde 200 OK
    And el body contiene hasPending = false

  @re-registration
  Scenario: Re-registro permitido tras turno expirado
    Given "tester@example.com" tiene una entrada con status "expired" para el evento
    When el actor llama POST /api/v1/waitlist/join con ese email y eventId
    Then el sistema responde 201 Created
    And existe una nueva entrada con status "pending" para ese email y evento
```

---

#### US2 — Asignación Automática

```gherkin
@waitlist @waitlist-kafka @US2
Feature: Asignación Automática al Liberarse un Asiento

  Background:
    Given el entorno de pruebas está disponible
    And la tabla bc_waitlist.waitlist_entries está limpia para el evento de prueba
    And el Ordering Service está disponible

  @happy-path @TI-09
  Scenario: Asignación automática al primer pending cuando llega reservation-expired
    Given existe una entrada con status "pending" para "primero@example.com" en el evento "c9d0e1f2-0000-0000-0000-000000000001"
    When se publica en Kafka el evento "reservation-expired" con:
      | campo          | valor                                    |
      | messageId      | <uuid-generado>                          |
      | reservationId  | a1b2c3d4-0000-0000-0000-000000000001     |
      | seatId         | e5f6a7b8-0000-0000-0000-000000000001     |
      | customerId     | test-customer                            |
      | concertEventId | c9d0e1f2-0000-0000-0000-000000000001     |
    Then en un máximo de 10 segundos la entrada de "primero@example.com" tiene status "assigned"
    And la entrada tiene order_id con valor UUID no nulo
    And la entrada tiene assigned_at con valor no nulo
    And la entrada tiene expires_at = assigned_at + 30 minutos (tolerancia ±2s)
    And en el Ordering Service existe una orden con status "pending" para ese seatId

  @negative @TI-10
  Scenario: Sin asignación cuando la cola del evento está vacía
    Given no existen entradas con status "pending" para el evento "c9d0e1f2-0000-0000-0000-000000000001"
    When se publica en Kafka el evento "reservation-expired" con concertEventId "c9d0e1f2-0000-0000-0000-000000000001"
    Then no se crean nuevas entradas en bc_waitlist.waitlist_entries
    And el asiento "e5f6a7b8-0000-0000-0000-000000000001" queda con status "available" en el Inventory Service

  @happy-path @TI-11
  Scenario: Pago exitoso marca la entrada como completed
    Given existe una entrada con status "assigned" para "pagador@example.com" con order_id "f1a2b3c4-0000-0000-0000-000000000001"
    When se publica en Kafka el evento "payment-succeeded" con:
      | campo     | valor                                |
      | paymentId | <uuid-generado>                      |
      | orderId   | f1a2b3c4-0000-0000-0000-000000000001 |
      | customerId| test-customer                        |
    Then en un máximo de 10 segundos la entrada tiene status "completed"

  @idempotency @TI-12
  Scenario: Idempotencia ante doble publicación de reservation-expired para el mismo seatId
    Given existe una entrada con status "assigned" para seatId "e5f6a7b8-0000-0000-0000-000000000001"
    When se publica el evento "reservation-expired" dos veces con el mismo seatId
    Then solo existe una entrada con status "assigned" para ese seatId en bc_waitlist
    And solo existe una orden en el Ordering Service para ese seatId
```

---

#### US3 — Rotación por Inacción

```gherkin
@waitlist @waitlist-api @US3
Feature: Rotación de Asignación por Inacción

  Background:
    Given el entorno de pruebas está disponible
    And la tabla bc_waitlist.waitlist_entries está limpia para el evento de prueba
    And el Ordering Service está disponible
    And el Inventory Service está disponible

  @happy-path @TI-13
  Scenario: Rotación al siguiente pending cuando el turno asignado vence
    Given existe una entrada con status "assigned" para "primero@example.com" con expires_at hace 1 minuto
    And existe una entrada con status "pending" para "segundo@example.com" en el mismo evento
    When el WaitlistExpiryWorker ejecuta su ciclo
    Then la entrada de "primero@example.com" tiene status "expired"
    And la entrada de "segundo@example.com" tiene status "assigned"
    And la entrada de "segundo@example.com" tiene un order_id no nulo
    And el seatId original NO fue liberado al Inventory general

  @negative @TI-14
  Scenario: Liberación del asiento al pool cuando la cola está vacía al vencer el turno
    Given existe una entrada con status "assigned" para "unico@example.com" con expires_at hace 1 minuto
    And no existen entradas con status "pending" para el mismo evento
    When el WaitlistExpiryWorker ejecuta su ciclo
    Then la entrada de "unico@example.com" tiene status "expired"
    And el asiento asociado queda con status "available" en el Inventory Service
    And la orden asociada queda con status "cancelled" en el Ordering Service

  @edge-case @TI-15
  Scenario: Una entrada ya completada no es procesada por el worker
    Given existe una entrada con status "completed" con expires_at en el pasado
    When el WaitlistExpiryWorker ejecuta su ciclo
    Then la entrada mantiene status "completed" sin cambios
    And no se realizó ninguna llamada al Inventory Service para ese seatId
```

---

#### Regresión

```gherkin
@waitlist @regression
Feature: Regresión — servicios existentes no afectados

  @TR-01
  Scenario: Flujo de compra directa no crea entradas en waitlist
    Given un usuario realiza el flujo estándar de compra de ticket
    When el pago es procesado exitosamente
    Then no existe ninguna entrada en bc_waitlist.waitlist_entries asociada a esa orden
    And la orden tiene status "completed" en el Ordering Service

  @TR-02
  Scenario: Expiración normal de reserva sin cola activa libera el asiento
    Given no existen entradas pending para el evento de prueba
    When una reserva de 15 minutos expira en el Inventory Service
    Then el asiento queda con status "available" en el Inventory Service
    And el topic "reservation-expired" recibe exactamente un mensaje
    And bc_waitlist.waitlist_entries no tiene nuevas entradas

  @TR-03
  Scenario: Inventory worker consulta has-pending y recibe fallback ante timeout de Waitlist
    Given el Waitlist Service está configurado para retornar timeout en has-pending
    When una reserva expira en el Inventory Service
    Then el Inventory Service libera el asiento normalmente (comportamiento previo)
    And se registra un warning en el log del Inventory Service
```

---

#### UI Screenplay

```gherkin
@waitlist @waitlist-ui @UI
Feature: Registro en Lista de Espera desde la interfaz de usuario

  Background:
    Given el navegador está abierto en la página del evento agotado
    And el evento tiene stock = 0

  @TCE-01
  Scenario: Registro exitoso desde el modal de la UI
    When el actor realiza la tarea JoinWaitlist con email "tester@example.com"
    Then el modal muestra el mensaje de éxito
    And el mensaje de éxito contiene la posición en la cola

  @TCE-02
  Scenario: Error de validación por email inválido en el formulario
    When el actor abre el modal de lista de espera
    And el actor ingresa "noesuncorreo" en el campo email
    And el actor hace clic en el botón de envío
    Then el modal muestra un mensaje de error de validación
    And el mensaje de error hace referencia al campo email

  @TCE-03
  Scenario: El botón de lista de espera no aparece cuando hay tickets disponibles
    Given el actor navega a un evento con stock > 0
    Then el botón "Unirse a lista de espera" no es visible
    And el botón de compra normal sí es visible
```

---

## 9. API-Screenplay: guía de implementación por escenario

Esta sección describe el mapeo directo entre cada paso Gherkin y la implementación técnica.

---

### TI-01 — Registro exitoso (stock=0 → 201)

```
HOOK BEFORE:
  ejecutar FIX-01 (event agotado)
  ejecutar FIX-CLEANUP (tabla limpia)

GIVEN "el evento tiene stock = 0":
  → FIX-01 ya aplicado, no requiere acción adicional en el step

WHEN "el actor llama POST /waitlist/join":
  → task: PostJoinWaitlistAPI(email="tester@example.com", eventId=EVENT_ID_AGOTADO)
  → endpoint: POST http://localhost:5006/api/v1/waitlist/join
  → body: { "email": "tester@example.com", "eventId": "c9d0e1f2-0000-0000-0000-000000000001" }

THEN "responde 201":
  → question: TheLastResponseStatus.code() == 201

THEN "body contiene posición >= 1":
  → parse response.body.position as int
  → assert position >= 1

THEN "body contiene entryId UUID válido":
  → parse response.body.entryId as string
  → assert matches regex: /^[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}$/i

THEN "existe entrada en DB con status pending":
  → question: TheWaitlistEntryInDB.forEmail("tester@example.com").andEvent(EVENT_ID_AGOTADO)
  → assert entry != null
  → assert entry.status == "pending"
```

---

### TI-02 — Stock disponible → 409

```
HOOK BEFORE:
  ejecutar FIX-02 (event disponible)

WHEN:
  → task: PostJoinWaitlistAPI(email="tester@example.com", eventId=EVENT_ID_DISPONIBLE)

THEN "responde 409":
  → assert statusCode == 409

THEN "body contiene mensaje de tickets disponibles":
  → parse response.body.message as string
  → assert message.toLowerCase() contains "disponibles" OR "available"
```

---

### TI-03 — Duplicado activo → 409

```
HOOK BEFORE:
  ejecutar FIX-03 (entry pending preexistente para tester@example.com)

WHEN:
  → task: PostJoinWaitlistAPI(email="tester@example.com", eventId=EVENT_ID_AGOTADO)

THEN "responde 409":
  → assert statusCode == 409

THEN "body contiene mensaje de ya está en lista":
  → parse response.body.message
  → assert contains "lista" OR "already"
```

---

### TI-04 — Email inválido → 400

```
WHEN:
  → task: PostJoinWaitlistAPI(email="noesuncorreo", eventId=EVENT_ID_AGOTADO)

THEN "responde 400":
  → assert statusCode == 400

THEN "body contiene errores de validación para campo email":
  → parse response.body.errors as array/object
  → assert errors contains entry with field == "email" OR key == "Email"
```

---

### TI-05 — Catalog caído → 503

```
HOOK BEFORE:
  configurar WireMock/stub en http://localhost:5001 para que
  GET /api/v1/events/*/availability retorne connection timeout (delay 5000ms)

WHEN:
  → task: PostJoinWaitlistAPI(email="tester@example.com", eventId=EVENT_ID_AGOTADO)

THEN "responde 503":
  → assert statusCode == 503

HOOK AFTER adicional:
  resetear stub de Catalog Service
```

---

### TI-06 y TI-07 — has-pending

```
TI-06:
  HOOK BEFORE: ejecutar FIX-05 (entry pending en DB)
  WHEN: task CheckHasPendingAPI(eventId=EVENT_ID_AGOTADO)
  THEN: statusCode == 200 AND response.body.hasPending == true

TI-07:
  HOOK BEFORE: FIX-CLEANUP (sin entries)
  WHEN: task CheckHasPendingAPI(eventId=EVENT_ID_AGOTADO)
  THEN: statusCode == 200 AND response.body.hasPending == false
```

---

### TI-13 — Rotación por inacción (trigger manual del worker)

```
HOOK BEFORE:
  ejecutar FIX-07 (entry assigned vencida + entry pending siguiente)

GIVEN "entry assigned con expires_at vencida":
  → FIX-07 ya crea los datos con timestamps manipulados

WHEN "el WaitlistExpiryWorker ejecuta su ciclo":
  OPCIÓN A — endpoint de test (recomendada si existe):
    POST http://localhost:5006/test/trigger-expiry-worker
    esperar 200

  OPCIÓN B — esperar el ciclo automático:
    thread.sleep(60s + 5s buffer)
    (no recomendada para suites rápidas)

  OPCIÓN C — manipular el clock del servicio (ej: NodaTime test clock):
    avanzar el reloj interno del servicio en 31 minutos
    esperar próximo ciclo del worker

THEN "entry primero@example.com tiene status expired":
  → task: WaitForWaitlistEntryStatus(email="primero@example.com", eventId=..., status="expired", timeout=15s)
  → question: TheWaitlistEntryInDB.forEmail("primero@example.com") → assert status == "expired"

THEN "entry segundo@example.com tiene status assigned":
  → task: WaitForWaitlistEntryStatus(email="segundo@example.com", eventId=..., status="assigned", timeout=15s)
  → assert entry.order_id != null
  → assert entry.expires_at == entry.assigned_at + 30min (±2s)

THEN "seatId original NO fue liberado":
  → GET http://localhost:5003/api/v1/seats/{SEAT_ID_TEST}
  → assert response.body.status != "available"
```

---

## 10. Kafka Integration: guía de implementación por escenario

### Configuración del producer/consumer de test

```
KafkaProducer configuración:
  bootstrap.servers = KAFKA_BOOTSTRAP
  key.serializer    = StringSerializer
  value.serializer  = StringSerializer (JSON string)
  acks              = all
  retries           = 3

KafkaConsumer configuración (para verificar mensajes de salida):
  bootstrap.servers = KAFKA_BOOTSTRAP
  group.id          = test-consumer-group-{uuid}    ← único por ejecución
  auto.offset.reset = latest
  key.deserializer  = StringDeserializer
  value.deserializer= StringDeserializer
```

---

### TI-09 — Asignación automática

```
HOOK BEFORE:
  ejecutar FIX-05 (entry pending para primero@example.com)
  inicializar KafkaProducer

STEP "se publica reservation-expired":
  → task: PublishReservationExpiredEvent(
      messageId      = gen_random_uuid(),
      reservationId  = "a1b2c3d4-0000-0000-0000-000000000001",
      seatId         = "e5f6a7b8-0000-0000-0000-000000000001",
      customerId     = "test-customer",
      concertEventId = EVENT_ID_AGOTADO
    )

THEN "entry tiene status assigned en máx 10s":
  → task: WaitForWaitlistEntryStatus(email="primero@example.com", eventId=EVENT_ID_AGOTADO, status="assigned", timeout=10)

THEN "entry tiene order_id no nulo":
  → question: TheWaitlistEntryInDB → assert order_id != null

THEN "entry tiene expires_at = assigned_at + 30min":
  → entry = TheWaitlistEntryInDB(...)
  → diff = entry.expires_at - entry.assigned_at
  → assert diff >= 29min 58s AND diff <= 30min 2s

THEN "existe orden en Ordering con status pending":
  → GET http://localhost:5002/api/v1/orders?seatId=e5f6a7b8-0000-0000-0000-000000000001
  → assert response.body[0].status == "pending"
```

---

### TI-10 — Cola vacía al recibir reservation-expired

```
HOOK BEFORE:
  FIX-CLEANUP (sin entries)

STEP "se publica reservation-expired":
  → task: PublishReservationExpiredEvent(concertEventId=EVENT_ID_AGOTADO, seatId=SEAT_ID_TEST, ...)

ESPERAR 5s para dar tiempo al consumer

THEN "no se crean entradas en DB":
  → question: TheWaitlistEntryCountInDB.forEvent(EVENT_ID_AGOTADO) == 0

THEN "asiento queda available en Inventory":
  → GET http://localhost:5003/api/v1/seats/e5f6a7b8-0000-0000-0000-000000000001
  → assert response.body.status == "available"
```

---

### TI-11 — Pago exitoso → Completed

```
HOOK BEFORE:
  ejecutar FIX-06 (entry assigned con orderId conocido)

STEP "se publica payment-succeeded":
  → task: PublishPaymentSucceededEvent(
      paymentId     = gen_random_uuid(),
      orderId       = "f1a2b3c4-0000-0000-0000-000000000001",
      customerId    = "test-customer",
      reservationId = "a1b2c3d4-0000-0000-0000-000000000001"
    )

THEN "entry tiene status completed en máx 10s":
  → task: WaitForWaitlistEntryStatus(email="pagador@example.com", ..., status="completed", timeout=10)
```

---

### TI-12 — Idempotencia

```
HOOK BEFORE:
  INSERT entry con status='assigned' y seat_id=SEAT_ID_TEST en DB directamente

STEP "se publica reservation-expired dos veces":
  mismo payload con seatId=SEAT_ID_TEST, messageId diferente en cada llamada
  PublishReservationExpiredEvent(...) x2 con pausa de 200ms entre ellas

ESPERAR 5s

THEN "solo 1 entry assigned para ese seatId":
  → SELECT COUNT(*) FROM bc_waitlist.waitlist_entries
    WHERE seat_id='e5f6a7b8-0000-0000-0000-000000000001' AND status='assigned'
  → assert count == 1

THEN "solo 1 orden en Ordering para ese seatId":
  → GET http://localhost:5002/api/v1/orders?seatId=e5f6a7b8-0000-0000-0000-000000000001
  → assert response.body.length == 1
```

---

## 11. UI Screenplay: guía de implementación por escenario

### TCE-01 — Registro exitoso desde la UI

```
HOOK BEFORE:
  ejecutar FIX-01 (event agotado)
  ejecutar FIX-CLEANUP
  abrir navegador en /events/c9d0e1f2-0000-0000-0000-000000000001

WHEN "el actor realiza la tarea JoinWaitlist":
  → actor.attemptsTo(JoinWaitlist.forEvent(EVENT_ID_AGOTADO, "tester@example.com"))

THEN "el modal muestra mensaje de éxito":
  → question: IsModalVisible.now() == true
  → question: elemento WaitlistModal.successMessage es visible
  → question: TheModalSuccessMessage.text() != ""

THEN "el mensaje contiene la posición":
  → assert TheModalSuccessMessage.text().matches(/posición \d+/ OR /position \d+/)
```

---

### TCE-02 — Error por email inválido en UI

```
WHEN "el actor abre el modal":
  → actor.attemptsTo(NavigateToSoldOutEvent.forEvent(EVENT_ID_AGOTADO))
  → actor.attemptsTo(OpenWaitlistModal)

WHEN "el actor ingresa email inválido y envía":
  → actor.attemptsTo(SubmitWaitlistForm.withEmail("noesuncorreo"))

THEN "modal muestra mensaje de error":
  → assert WaitlistModal.errorMessage es visible
  → assert WaitlistModal.successMessage NO es visible

THEN "mensaje hace referencia al campo email":
  → assert WaitlistModal.errorMessage.text().toLowerCase() contains "email"
```

---

### TCE-03 — Botón no visible con stock disponible

```
HOOK BEFORE:
  ejecutar FIX-02 (event disponible)
  abrir navegador en /events/c9d0e1f2-0000-0000-0000-000000000002

THEN "botón lista de espera no es visible":
  → assert EventsPage.joinWaitlistButton NO existe en DOM OR tiene display:none

THEN "botón de compra normal sí es visible":
  → assert EventsPage.buyTicketButton es visible
```

---

## 12. Contratos de referencia

### `POST /api/v1/waitlist/join`

```json
Request:
{
  "email":   "string (formato email, requerido)",
  "eventId": "string (UUID, requerido)"
}

Response 201:
{
  "position": "integer (≥1)",
  "entryId":  "string (UUID)"
}

Response 409:
{
  "message": "string"
}

Response 400:
{
  "errors": { "email": ["mensaje de validación"] }
}
```

---

### `GET /api/v1/waitlist/has-pending`

```json
Query param: eventId (UUID, requerido)

Response 200:
{
  "hasPending":    "boolean",
  "pendingCount":  "integer (opcional)"
}
```

Ver contrato completo en: [contracts/has-pending-response.json](./contracts/has-pending-response.json)

---

### Kafka topic: `reservation-expired` (v3)

```json
{
  "messageId":      "string (UUID) — ID de idempotencia del mensaje",
  "reservationId":  "string (UUID)",
  "seatId":         "string (UUID)",
  "customerId":     "string",
  "concertEventId": "string (UUID) — ID del concierto/evento"
}
```

> Campo renombrado respecto a v2: `eventId` → `messageId`. Campo nuevo: `concertEventId`.
Ver contrato completo en: [contracts/reservation-expired-v3.json](./contracts/reservation-expired-v3.json)

---

### `POST /api/v1/orders/waitlist` (interno)

```json
Request:
{
  "seatId":         "string (UUID)",
  "price":          "number (decimal ≥0)",
  "guestToken":     "string (email)",
  "concertEventId": "string (UUID)"
}

Response 201:
{
  "orderId": "string (UUID)"
}

Response 409: orden ya existe para ese seatId
```

Ver contrato completo en: [contracts/waitlist-order-request.json](./contracts/waitlist-order-request.json)

---

## 13. Matriz de escenarios vs método de test

| ID | Historia | Escenario | API-Screenplay | UI Screenplay | Kafka Integration | Fixture necesario |
|---|---|---|:---:|:---:|:---:|---|
| TI-01 | US1 | Registro exitoso stock=0 → 201 | ✅ | ✅ | — | FIX-01 |
| TI-02 | US1 | Stock disponible → 409 | ✅ | ✅ | — | FIX-02 |
| TI-03 | US1 | Duplicado activo → 409 | ✅ | — | — | FIX-03 |
| TI-04 | US1 | Email inválido → 400 | ✅ | ✅ | — | — |
| TI-05 | US1 | Catalog caído → 503 | ✅ (stub) | — | — | Mock Catalog |
| TI-06 | US1 | has-pending = true | ✅ | — | — | FIX-05 |
| TI-07 | US1 | has-pending = false | ✅ | — | — | FIX-CLEANUP |
| — | US1 | Re-registro tras expired | ✅ | — | — | entry expired en DB |
| TI-09 | US2 | Asignación automática | — | — | ✅ | FIX-05 |
| TI-10 | US2 | Cola vacía al recibir evento | — | — | ✅ | FIX-CLEANUP |
| TI-11 | US2 | Pago exitoso → Completed | — | — | ✅ | FIX-06 |
| TI-12 | US2 | Idempotencia doble evento | — | — | ✅ | entry assigned en DB |
| TI-13 | US3 | Rotación con siguiente pending | ✅ (DB seed) | — | — | FIX-07 |
| TI-14 | US3 | Cola vacía al expirar turno | ✅ (DB seed) | — | — | FIX-08 |
| TI-15 | US3 | Entry completed no procesada | ✅ (DB seed) | — | — | entry completed en DB |
| TR-01 | Regresión | Compra directa sin waitlist | ✅ | — | — | — |
| TR-02 | Regresión | Inventory expira sin cola activa | — | — | ✅ | FIX-CLEANUP |
| TR-03 | Regresión | Fallback ante timeout de Waitlist | ✅ (stub) | — | — | Mock Waitlist |
| TCE-01 | UI | Registro exitoso desde modal | — | ✅ | — | FIX-01 |
| TCE-02 | UI | Error email inválido en UI | — | ✅ | — | FIX-01 |
| TCE-03 | UI | Botón no visible con stock | — | ✅ | — | FIX-02 |

---

*Documento generado para la feature `004-waitlist-autoassign`. Actualizar junto con cambios en `spec.md` o `plan.md`.*
