# 02 — Criterios de Aceptación

> **Fase SDLC:** Análisis de Requisitos
> **Audiencia:** QA, Dev, Negocio
> **Metodología:** ATDD — estos escenarios son tests ejecutables, no solo documentación

---

## Por qué Gherkin

Los criterios de aceptación en Gherkin son el puente entre negocio y código. Un escenario Gherkin es:

1. **Lenguaje de negocio** — cualquier stakeholder puede leerlo y entender qué hace el sistema
2. **Contrato ejecutable** — cada escenario se traduce directamente a un test automatizado
3. **Documentación viva** — si el código cambia y el escenario ya no pasa, el test lo detecta

En este proyecto, cada escenario Gherkin tiene un test unitario correspondiente documentado en [`06-tdd-evidence.md`](./06-tdd-evidence.md).

---

## Feature: Sistema de Lista de Espera Inteligente

```gherkin
Feature: Sistema de Lista de Espera Inteligente
  Como plataforma de venta de boletos
  Quiero gestionar una cola de espera justa para eventos agotados
  Para garantizar que cada asiento liberado llegue al usuario que esperó más tiempo
```

---

## HU-01: Registro en Lista de Espera

### ESC-01 — Registro exitoso

```gherkin
Scenario: Registro exitoso en lista de espera
  Given  que el evento "Concierto Rock 2026" tiene stock = 0
  And    que "jostin@example.com" no tiene una entrada activa para este evento
  When   "jostin@example.com" envía POST /api/v1/waitlist/join con el EventId válido
  Then   el sistema responde 201 Created
  And    la respuesta incluye { "entryId": "<uuid>", "position": 1 }
  And    la entrada queda registrada con Status = "pending"
  And    RegisteredAt refleja el momento del registro
```

**Regla validada:** HU-01 — flujo principal de registro
**Test correspondiente:** `Handle_ValidEmail_ZeroStock_CreatesEntryAndReturnsPosition`

---

### ESC-02 — Rechazo por stock disponible

```gherkin
Scenario: Intento de registro con asientos disponibles
  Given  que el evento "Concierto Rock 2026" tiene stock = 5
  When   "jostin@example.com" envía POST /api/v1/waitlist/join
  Then   el sistema responde 409 Conflict
  And    el mensaje indica "Hay tickets disponibles para este evento. La lista de espera no aplica."
  And   no se crea ninguna entrada en la lista de espera
```

**Regla validada:** RN-02 — no unirse si hay stock
**Test correspondiente:** `Handle_StockAvailable_ThrowsWaitlistConflictException`

---

### ESC-03 — Rechazo por entrada duplicada

```gherkin
Scenario: Registro duplicado para el mismo evento
  Given  que "jostin@example.com" ya tiene una entrada con Status = "pending"
         para el evento "Concierto Rock 2026"
  When   "jostin@example.com" intenta registrarse nuevamente para el mismo evento
  Then   el sistema responde 409 Conflict
  And    el mensaje indica "Ya estás en la lista de espera de este evento."
  And    la entrada existente no se modifica

Scenario: Registro duplicado con entrada en estado assigned
  Given  que "jostin@example.com" tiene una entrada con Status = "assigned"
         para el evento "Concierto Rock 2026"
  When   "jostin@example.com" intenta registrarse nuevamente
  Then   el sistema responde 409 Conflict
  And    el mensaje indica "Ya estás en la lista de espera de este evento."
```

**Regla validada:** RN-01 — una entrada activa por usuario/evento
**Test correspondiente:** `Handle_DuplicateActiveEntry_ThrowsWaitlistConflictException`

---

### ESC-09 — Validación de request inválido

```gherkin
Scenario: Email con formato inválido
  Given  que el usuario envía POST /api/v1/waitlist/join
  When   el campo "email" contiene "no-es-un-email"
  Then   el sistema responde 400 Bad Request
  And    la respuesta incluye el error "Email format is invalid."

Scenario: Email vacío
  Given  que el usuario envía POST /api/v1/waitlist/join
  When   el campo "email" está vacío
  Then   el sistema responde 400 Bad Request
  And    la respuesta incluye el error de validación correspondiente

Scenario: EventId vacío
  Given  que el usuario envía POST /api/v1/waitlist/join
  When   el campo "eventId" es "00000000-0000-0000-0000-000000000000"
  Then   el sistema responde 400 Bad Request
```

**Regla validada:** Validación de entrada en frontera del sistema
**Test correspondiente:** `Validator_InvalidEmail_ReturnsValidationError`

---

### ESC-10 — Catálogo no disponible

```gherkin
Scenario: Catalog Service no responde
  Given  que el Catalog Service lanza una excepción HTTP al consultar disponibilidad
  When   "jostin@example.com" intenta unirse a la lista de espera
  Then   el sistema responde 503 Service Unavailable
  And    el mensaje indica que no fue posible verificar la disponibilidad
  And   no se crea ninguna entrada en la lista de espera
```

**Regla validada:** Resiliencia ante fallo de servicio externo
**Test correspondiente:** `Handle_CatalogUnavailable_ThrowsServiceUnavailableException`

---

## HU-02: Asignación Automática

### ESC-04 — Asignación al primer usuario en cola

```gherkin
Scenario: Asignación automática al expirar una reserva
  Given  que "jostin@example.com" es el primero en la cola del evento "Concierto Rock 2026"
  And    que la cola tiene 3 entradas con Status = "pending"
  When   Kafka recibe el evento "reservation-expired"
         con SeatId = "<uuid>" y concertEventId = "<uuid>"
  Then   el sistema crea una orden de compra automática para "jostin@example.com" en Ordering
  And    actualiza la entrada de "jostin@example.com" a Status = "assigned"
  And    establece ExpiresAt = momento_actual + 30 minutos
  And    envía una notificación a "jostin@example.com" con el link de pago
```

**Regla validada:** RN-03 (FIFO), RN-04 (ventana 30 min)
**Test correspondiente:** `Handle_PendingEntryExists_AssignsEntryAndSendsEmail`

---

### ESC — Cola vacía al expirar reserva

```gherkin
Scenario: No hay usuarios en cola cuando expira una reserva
  Given  que no hay entradas con Status = "pending" para el evento "Concierto Rock 2026"
  When   Kafka recibe el evento "reservation-expired" con SeatId y concertEventId
  Then   el sistema no realiza ninguna asignación
  And    el asiento queda disponible para el flujo normal de compra
```

**Regla validada:** Comportamiento cuando la cola está vacía
**Test correspondiente:** `Handle_EmptyQueue_DoesNothing`

---

### ESC — Idempotencia de asignación

```gherkin
Scenario: Evento reservation-expired recibido dos veces para el mismo asiento
  Given  que el asiento "<seatId>" ya tiene una entrada en Status = "assigned"
  When   Kafka entrega nuevamente el evento "reservation-expired" para ese mismo asiento
  Then   el sistema no crea una segunda asignación
  And    la entrada existente no se modifica
```

**Regla validada:** Semántica at-least-once de Kafka — el sistema es idempotente
**Test correspondiente:** `Handle_SeatAlreadyAssigned_ReturnsWithoutAction`

---

## HU-03: Rotación de Asignación

### ESC-05 — Rotación con siguiente en cola

```gherkin
Scenario: Rotación cuando el usuario asignado no paga en 30 minutos
  Given  que "jostin@example.com" tiene una entrada con Status = "assigned"
         y ExpiresAt < momento_actual (venció)
  And    que "segundo@example.com" es el siguiente en cola con Status = "pending"
  When   WaitlistExpiryWorker detecta la entrada expirada
  Then   la entrada de "jostin@example.com" pasa a Status = "expired"
  And    se envía notificación de expiración a "jostin@example.com"
  And    se cancela la orden de compra de "jostin@example.com" en Ordering
  And    se crea una nueva orden de compra para "segundo@example.com"
  And    la entrada de "segundo@example.com" pasa a Status = "assigned"
         con nuevo ExpiresAt = momento_actual + 30 minutos
  And    se envía notificación con link de pago a "segundo@example.com"
  And    el asiento NO vuelve al inventario disponible en ningún momento
```

**Regla validada:** RN-04, RN-05 — la más crítica del dominio
**Test correspondiente:** `ProcessExpired_NextExists_RotatesDirectlyWithoutReleasingInventory`

---

### ESC-06 — Liberación con cola vacía

```gherkin
Scenario: Cola vacía cuando el usuario asignado no paga
  Given  que "jostin@example.com" tiene una entrada con Status = "assigned"
         y ExpiresAt < momento_actual
  And    que no hay más entradas con Status = "pending" para el evento
  When   WaitlistExpiryWorker detecta la entrada expirada
  Then   la entrada de "jostin@example.com" pasa a Status = "expired"
  And    se envía notificación de expiración a "jostin@example.com"
  And    se cancela la orden de compra en Ordering
  And    se llama a Inventory para liberar el asiento al inventario disponible
```

**Regla validada:** RN-06 — liberación cuando la cola se agota
**Test correspondiente:** `ProcessExpired_EmptyQueue_ReleasesToInventory`

---

### ESC-07 — Pago completado por usuario asignado

```gherkin
Scenario: Usuario en waitlist completa el pago exitosamente
  Given  que "jostin@example.com" tiene una entrada con Status = "assigned"
         y OrderId = "<orderId>"
  When   Kafka recibe el evento "payment-succeeded" con orderId = "<orderId>"
  Then   la entrada de "jostin@example.com" pasa a Status = "completed"
  And    la asignación queda cerrada permanentemente
```

**Regla validada:** Cierre del ciclo de vida de la entrada
**Test correspondiente:** `Handle_AssignedEntryWithMatchingOrder_CompletesAssignment`

---

## ESC-08 — Consulta de pendientes (integración ADR-03)

```gherkin
Scenario: Inventory consulta si hay usuarios en espera antes de liberar asiento
  Given  que hay 3 entradas con Status = "pending" para el EventId "abc-123"
  When   Inventory llama GET /api/v1/waitlist/has-pending?eventId=abc-123
  Then   el sistema responde 200 OK
  And    la respuesta es { "hasPending": true, "pendingCount": 3 }

Scenario: No hay usuarios en espera
  Given  que no hay entradas con Status = "pending" para el EventId "abc-123"
  When   Inventory llama GET /api/v1/waitlist/has-pending?eventId=abc-123
  Then   el sistema responde 200 OK
  And    la respuesta es { "hasPending": false, "pendingCount": 0 }
```

**Regla validada:** ADR-03 — integración con Inventory para decisión de retención/liberación
**Test correspondiente:** Prueba de integración del endpoint

---

## Mapa de cobertura de reglas de negocio

```
RN-01 ──► ESC-03 ✓
RN-02 ──► ESC-02 ✓
RN-03 ──► ESC-04 ✓ (orden FIFO verificado)
RN-04 ──► ESC-04, ESC-05, ESC-06 ✓
RN-05 ──► ESC-05 ✓ (el más crítico — asiento no liberado)
RN-06 ──► ESC-06 ✓
```

Cada regla de negocio tiene al menos un escenario de aceptación. Cada escenario tiene un test automatizado. **Cobertura total de reglas: 6/6.**
