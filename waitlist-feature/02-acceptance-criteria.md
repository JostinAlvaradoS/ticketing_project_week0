# 02 — Criterios de Aceptación

> **Fase SDLC:** Análisis de Requisitos
> **Audiencia:** QA, Dev, Negocio
> **Metodología:** ATDD — estos escenarios son el contrato entre negocio y desarrollo

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

## Escenarios definidos en el diseño

Estos son los 6 escenarios originales definidos en la fase de diseño. Constituyen el contrato mínimo que el sistema debe cumplir.

---

### ESC-01 — Registro exitoso en lista de espera

```gherkin
Dado que el evento "Concierto Rock 2026" tiene stock = 0
Cuando el usuario "jostin@example.com" se registra en la waitlist con su correo
Entonces el sistema responde 201 Created
```

**Regla validada:** HU-01 — flujo principal de registro
**Test correspondiente:** `Handle_ValidEmail_ZeroStock_CreatesEntryAndReturnsPosition`

---

### ESC-02 — Intento de registro con tickets disponibles

```gherkin
Dado que el evento "Concierto Rock 2026" tiene stock > 0
Cuando el usuario "jostin@example.com" intenta unirse a la lista de espera
Entonces el sistema responde "Hay tickets disponibles, realiza la compra directamente"
```

**Regla validada:** RN-02 — no unirse si hay stock disponible
**Test correspondiente:** `Handle_StockAvailable_ThrowsWaitlistConflictException`

---

### ESC-03 — Registro duplicado en la misma lista

```gherkin
Dado que "jostin@example.com" ya está registrado en la lista del evento "Concierto Rock 2026"
Cuando el mismo correo intenta registrarse nuevamente para el mismo evento
Entonces el sistema responde "Ya estás en la lista de espera para este evento"
```

**Regla validada:** RN-01 — una sola entrada activa por usuario por evento
**Test correspondiente:** `Handle_DuplicateActiveEntry_ThrowsWaitlistConflictException`

---

### ESC-04 — Asignación automática al expirar una reserva

```gherkin
Dado que "jostin@example.com" es el primero en la lista de espera del evento "Concierto Rock 2026"
Cuando el tiempo de pago inicial caduca
Entonces el sistema crea una orden automática para "jostin@example.com"
Y actualiza el estado de la entrada a Asignado
Y envía un correo con el enlace de pago con validez de 30 minutos
```

**Regla validada:** RN-03 (FIFO), RN-04 (ventana de 30 minutos)
**Test correspondiente:** `Handle_PendingEntryExists_AssignsEntryAndSendsEmail`

---

### ESC-05 — Liberación por inacción con siguiente en cola

```gherkin
Dado que "jostin@example.com" fue asignado y no pagó en 30 minutos
Y "segundo@example.com" es el siguiente en la lista
Cuando el sistema detecta este hecho
Entonces el sistema marca la entrada de "jostin@example.com" como Expirado
Y reasigna el asiento directamente a "segundo@example.com" sin liberarlo al pool general
Y envía correo de pago a "segundo@example.com" con validez de 30 minutos
```

**Regla validada:** RN-04 (ventana de tiempo), RN-05 (asiento no vuelve al pool durante rotación)
**Test correspondiente:** `ProcessExpired_WithNextPending_ExpiresCurrentAndAssignsNext`

---

### ESC-06 — Liberación por inacción con cola vacía

```gherkin
Dado que "jostin@example.com" fue asignado y no pagó en 30 minutos
Y no hay más usuarios en la lista de espera del evento
Cuando el sistema detecta este hecho
Entonces el sistema cancela la orden y libera el asiento al pool general
```

**Regla validada:** RN-06 — liberar el asiento cuando la cola se agota
**Test correspondiente:** `ProcessExpired_EmptyQueue_ReleasesSeatAndCancelsOrder`

---

## Escenarios adicionales de cobertura

Estos escenarios no estaban en el diseño original pero se identificaron durante la implementación como casos de borde necesarios para garantizar la robustez del sistema.

---

### ESC-07 — Pago completado exitosamente por usuario asignado

```gherkin
Dado que "jostin@example.com" tiene una Entrada con Estado = Asignado
Y su Orden de Compra fue pagada exitosamente
Cuando el sistema recibe la confirmación del pago
Entonces la Entrada de "jostin@example.com" pasa a Estado = Completado
Y la asignación queda cerrada permanentemente
```

**Regla validada:** Cierre del ciclo de vida de la Entrada
**Test correspondiente:** `Handle_AssignedEntry_SetsStatusCompleted`

---

### ESC-08 — Coordinación con Inventario para retención del asiento (ADR-03)

```gherkin
Dado que hay usuarios con Estado = Pendiente en la lista de espera para un evento
Cuando el servicio de Inventario verifica si debe liberar un asiento expirado
Entonces el sistema confirma que hay usuarios en espera
Y el servicio de Inventario retiene el asiento sin liberarlo al inventario disponible

Dado que no hay usuarios con Estado = Pendiente para el evento
Cuando el servicio de Inventario verifica si debe liberar un asiento expirado
Entonces el sistema confirma que la cola está vacía
Y el servicio de Inventario libera el asiento al inventario disponible
```

**Regla validada:** ADR-03 — el asiento permanece bloqueado durante la rotación
**Test correspondiente:** `ProcessExpiredReservations_WhenQueueActive_DoesNotReleaseSeat` (Inventory service)

---

### ESC-09 — Solicitud con datos inválidos

```gherkin
Dado que un usuario intenta unirse a la lista de espera
Cuando el correo proporcionado no tiene un formato válido
Entonces el sistema responde 400 Bad Request
Y la respuesta incluye el error de validación correspondiente
```

**Regla validada:** Validación de entrada en la frontera del sistema
**Test correspondiente:** `JoinWaitlistCommandValidator_InvalidEmail_HasValidationError`

---

### ESC-10 — Catálogo no disponible

```gherkin
Dado que el servicio de Catálogo no está disponible
Cuando "jostin@example.com" intenta unirse a la lista de espera
Entonces el sistema responde 503 Service Unavailable
Y no se crea ninguna Entrada en la Lista de Espera
```

**Regla validada:** Resiliencia ante fallo de servicio externo
**Test correspondiente:** `Handle_CatalogClientThrows_ThrowsServiceUnavailableException`

---

### ESC-11 — Idempotencia de asignación

```gherkin
Dado que un asiento ya fue asignado a un usuario de la lista de espera
Cuando el sistema recibe nuevamente la notificación de expiración para ese mismo asiento
Entonces el sistema no crea una segunda asignación
Y la entrada existente no se modifica
```

**Regla validada:** El sistema tolera la entrega duplicada de eventos (semántica at-least-once de Kafka)
**Test correspondiente:** `Handle_SeatAlreadyAssigned_SkipsProcessing`

---

## Mapa de cobertura de reglas de negocio

```
RN-01 (una entrada activa por usuario/evento)  ──► ESC-03 ✓
RN-02 (no unirse si hay stock)                 ──► ESC-02 ✓
RN-03 (cola FIFO)                              ──► ESC-04 ✓
RN-04 (ventana de 30 minutos)                  ──► ESC-04, ESC-05, ESC-06 ✓
RN-05 (asiento no se libera durante rotación)  ──► ESC-05, ESC-08 ✓
RN-06 (liberar si la cola se agota)            ──► ESC-06 ✓
```

Cada regla de negocio tiene al menos un escenario de aceptación.
Cada escenario tiene un test automatizado correspondiente.
**Cobertura total de reglas: 6/6.**
