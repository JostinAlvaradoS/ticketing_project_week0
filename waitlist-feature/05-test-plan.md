# 05 — Test Plan

> **Fase SDLC:** Pruebas
> **Audiencia:** QA, Tech Lead
> **Estándar:** IEEE 829 (adaptado)

---

## 1. Alcance

### En scope

| Componente | Qué se prueba |
|-----------|--------------|
| `WaitlistEntry` (Dominio) | Máquina de estados: transiciones válidas e inválidas, guards, cálculo de ExpiresAt |
| `JoinWaitlistHandler` | Validación de stock, duplicados, persistencia, posición en cola |
| `AssignNextHandler` | FIFO, idempotencia, creación de orden, notificación |
| `CompleteAssignmentHandler` | Cierre de ciclo, idempotencia (orden no pertenece a waitlist) |
| `WaitlistExpiryWorker` | Rotación directa, liberación al inventario, ciclo sin expirados |
| `ReservationExpiredConsumer` | Deserialización, dispatch de comando, compatibilidad v2 |
| Endpoints HTTP | Contrato de request/response, códigos de estado, manejo de excepciones |

### Fuera de scope

| Componente | Razón |
|-----------|-------|
| Catalog Service internals | Servicio externo — se mockea vía `ICatalogClient` |
| Ordering Service internals | Servicio externo — se mockea vía `IOrderingClient` |
| Inventory Service internals | Servicio externo — se mockea vía `IInventoryClient` |
| SMTP real | Side effect externo — se mockea vía `IEmailService` |
| Pruebas de carga (10k usuarios/min) | Fuera del alcance del entorno de entrenamiento |

---

## 2. Pirámide de Pruebas

```
              ┌─────────────┐
              │    E2E      │  5%   Docker Compose completo
              │  (Sistema)  │       system-e2e-test.sh
              ├─────────────┤
              │ Integración │  15%  WebApplicationFactory
              │(Componente) │       In-Memory DB
              ├─────────────┤
              │   Unitarias │  80%  xUnit + Moq + FluentAssertions
              │  (Dominio + │       Infraestructura mockeada
              │ Aplicación) │
              └─────────────┘
```

**Justificación de la distribución:**
- La mayoría de las reglas de negocio viven en el dominio y la capa de aplicación
- Las pruebas unitarias son las más baratas de escribir, mantener y ejecutar
- Las pruebas de integración validan el ensamblaje de capas, no la lógica
- E2E valida el contrato observable desde el exterior

---

## 3. Técnicas de prueba aplicadas

### Partición de equivalencia

Dividir los valores de entrada en clases donde el sistema debe comportarse igual para todos los valores de la clase:

| Campo | Clase válida | Clase inválida 1 | Clase inválida 2 |
|-------|-------------|-----------------|-----------------|
| `email` | "user@example.com" | "" (vacío) | "no-es-email" |
| `eventId` | Guid válido | Guid.Empty | null |
| `availableCount` | 0 (unirse permitido) | > 0 (unirse rechazado) | — |
| `Status` en Assign | "pending" | "assigned" | "completed" |

### Análisis de valores límite

Para el timer de 30 minutos:

| Valor | Comportamiento esperado |
|-------|------------------------|
| `ExpiresAt = now - 1ms` | El worker debe detectarla como expirada |
| `ExpiresAt = now` | El worker debe detectarla como expirada |
| `ExpiresAt = now + 1ms` | El worker NO debe detectarla como expirada |

### Pruebas de transición de estados

Cada transición tiene al menos un test positivo (transición válida) y un test negativo (guard que impide transición inválida):

```
pending → assigned  : ✓ test positivo | ✓ test negativo (desde assigned)
assigned → completed: ✓ test positivo | ✓ test negativo (desde pending)
assigned → expired  : ✓ test positivo | ✓ test negativo (desde pending)
```

### Pruebas de idempotencia

Las operaciones que pueden recibir el mismo mensaje más de una vez deben comportarse de forma segura:

| Handler | Caso idempotente | Comportamiento esperado |
|---------|-----------------|------------------------|
| `AssignNextHandler` | Mismo `seatId` llega dos veces | Segunda llamada retorna sin acción |
| `CompleteAssignmentHandler` | `orderId` no pertenece a waitlist | Retorna sin acción |
| `ReservationExpiredConsumer` | Payload v2 sin `concertEventId` | Descartado silenciosamente |

---

## 4. Niveles de prueba

### Nivel 1 — Pruebas Unitarias (Unit Tests)

**Framework:** xUnit + Moq + FluentAssertions
**Ubicación:** `services/waitlist/tests/unit/Waitlist.UnitTests/`
**Infraestructura:** Solo mocks — sin base de datos, sin HTTP, sin Kafka

**Subdivisión:**

```
Domain/
└── WaitlistEntryTests.cs          ← TDD Ciclos 1-6

Application/
├── JoinWaitlistHandlerTests.cs    ← TDD Ciclos 7-11
├── AssignNextHandlerTests.cs      ← TDD Ciclos 12-14 + 16
└── ReservationExpiredConsumerTests.cs ← TDD Ciclo 15
```

**Qué se verifica vs. qué se valida:**

| Tipo | Ejemplo | Técnica |
|------|---------|---------|
| Verificación | `_repoMock.Verify(UpdateAsync, Times.Once)` | Mock.Verify — confirma que el puerto fue invocado |
| Validación | `entry.Status.Should().Be("assigned")` | FluentAssertions — confirma que la regla de negocio se cumplió |

### Nivel 2 — Pruebas de Componente (Integration Tests)

**Framework:** `WebApplicationFactory` + InMemory EF Core
**Propósito:** Probar el ensamblaje de controlador → handler → repositorio sin infraestructura real
**Infraestructura:** Base de datos en memoria, servicios externos aún mockeados

**Qué valida este nivel que el unitario no puede:**
- Que el endpoint HTTP desambigua correctamente la excepción → código HTTP
- Que el pipeline de MediatR ejecuta el handler correcto
- Que la serialización/deserialización del request/response funciona

### Nivel 3 — Pruebas de Sistema (E2E)

**Herramienta:** `system-e2e-test.sh` en el pipeline CI/CD
**Infraestructura:** Docker Compose completo — todos los 12 contenedores
**Qué valida:**
- Que el servicio arranca y las migraciones se aplican
- Que `GET /health` responde 200
- Que `POST /api/v1/waitlist/join` con datos válidos responde correctamente
- Que el flujo end-to-end completo (reserva → waitlist → pago) produce un boleto

---

## 5. Matriz de casos de prueba

| ID | Descripción | Nivel | Tipo | Input | Resultado esperado | RN / HU |
|----|-------------|-------|------|-------|-------------------|---------|
| TC-001 | Registro exitoso con stock=0 | Unit | Validación | email válido, stock=0, sin duplicado | 201, entryId + position | HU-01 |
| TC-002 | Rechazo por stock disponible | Unit | Validación | email válido, stock=5 | WaitlistConflictException | RN-02 |
| TC-003 | Rechazo por entrada duplicada | Unit | Validación | email ya en pending | WaitlistConflictException | RN-01 |
| TC-004 | Catalog no disponible | Unit | Validación | Catalog throws | WaitlistServiceUnavailableException | Resiliencia |
| TC-005 | Email inválido | Unit | Validación | "no-es-email" | ValidationError | Guard |
| TC-006 | Asignación automática FIFO | Unit | Validación | 3 pendientes, primer entry | entry.Status=assigned, email enviado | RN-03, RN-04 |
| TC-007 | Cola vacía en asignación | Unit | Validación | 0 pendientes | No-op | RN-03 |
| TC-008 | Idempotencia de asignación | Unit | Verificación | seatId ya asignado | No-op, UpdateAsync no llamado | Idempotencia |
| TC-009 | Rotación con siguiente en cola | Unit | Validación | expired + next exists | next.Status=assigned, asiento no liberado | RN-05 |
| TC-010 | Liberación con cola vacía | Unit | Validación | expired + cola vacía | ReleaseSeatAsync llamado | RN-06 |
| TC-011 | Completar asignación por pago | Unit | Validación | payment-succeeded, orderId matches | entry.Status=completed | HU-02 |
| TC-012 | Completar asignación — orden no waitlist | Unit | Verificación | orderId no en waitlist | No-op | Idempotencia |
| TC-013 | Consumer v2 descartado | Unit | Verificación | payload sin concertEventId | AssignNext no llamado | Compat. v2 |
| TC-014 | Consumer v3 válido | Unit | Verificación | payload con seatId + concertEventId | AssignNextCommand enviado | Flujo |
| TC-015 | Endpoint /health | E2E | Caja negra | GET /health | 200 OK | Infraestructura |
| TC-016 | Endpoint /join e2e | E2E | Caja negra | POST /join con evento real | 201 o 409 | HU-01 |

---

## 6. QA Gates — Criterios de calidad no negociables

| Gate | Criterio | Consecuencia de fallo |
|------|---------|----------------------|
| **Gate 1: Suite verde** | 0 tests en rojo | Pipeline bloquea el merge |
| **Gate 2: Cobertura** | ≥ 85% en Domain + Application | SonarCloud reporta Quality Gate failed |
| **Gate 3: Sin vulnerabilidades críticas** | Trivy: 0 CRITICAL/HIGH | Pipeline falla con exit code 1 |
| **Gate 4: Arquitectura** | NetArchTest: Domain sin dependencias externas | CI falla en Architecture Tests |
| **Gate 5: Build** | `dotnet build` sin errores ni warnings tratados como error | Branch Protection bloquea merge |

---

## 7. Los 7 Principios del Testing aplicados a esta feature

| # | Principio | Aplicación concreta |
|---|-----------|---------------------|
| 1 | **Las pruebas muestran presencia de defectos** | Los tests no garantizan que no hay bugs — garantizan que los escenarios testeados funcionan. Por eso se cubren casos edge. |
| 2 | **Pruebas exhaustivas son imposibles** | Se usa partición de equivalencia. No se prueban 1000 emails inválidos — se prueba una clase representativa. |
| 3 | **Pruebas tempranas (Shift-Left)** | TDD garantiza shift-left máximo: el test existe antes que el código. El bug existe 0 segundos. |
| 4 | **Agrupamiento de defectos** | `WaitlistExpiryWorker` es el componente más complejo (orquesta 4 servicios externos) → mayor densidad de tests. |
| 5 | **Paradoja del pesticida** | Se agregan casos edge con cada iteración: idempotencia, payload v2, cola vacía, timeout de Catalog. |
| 6 | **Las pruebas dependen del contexto** | En Waitlist, la idempotencia y la gestión de estados son críticas → tests específicos. En Notification, la prioridad es no duplicar emails. Cada servicio tiene su contexto. |
| 7 | **La falacia de la ausencia de errores** | Un sistema sin bugs que no cumple RN-05 (no libera el asiento durante rotación) es inútil. ATDD garantiza que los requisitos de negocio están cubiertos. |
