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

**Framework:** `WebApplicationFactory<Program>` + EF Core InMemory (`InMemoryDatabaseRoot`)
**Ubicación:** `services/waitlist/tests/integration/Waitlist.IntegrationTests/`
**Propósito:** Probar el ensamblaje de controlador → handler → repositorio sin infraestructura real
**Infraestructura:** Base de datos en memoria compartida vía `InMemoryDatabaseRoot`; servicios externos (`ICatalogClient`, `IOrderingClient`, `IInventoryClient`, `IEmailService`) mockeados con Moq; hosted services de Kafka eliminados del host de prueba

**Cobertura implementada (TI-01 a TI-07):**

| Test | Componente bajo prueba | Vía de entrada |
|------|------------------------|----------------|
| TI-01, TI-02, TI-03 | Controlador → `JoinWaitlistHandler` → `WaitlistRepository` | HTTP `POST /api/v1/waitlist/join` |
| TI-04, TI-05 | `AssignNextHandler` → `WaitlistRepository` | `IMediator.Send(AssignNextCommand)` directo |
| TI-06, TI-07 | `WaitlistExpiryWorker.ProcessExpiredEntriesAsync` → `WaitlistRepository` | Instancia directa con `IServiceScopeFactory` real |

**Qué valida este nivel que el unitario no puede:**
- Que el endpoint HTTP desambigua correctamente la excepción → código HTTP
- Que el pipeline de MediatR ejecuta el handler correcto
- Que la serialización/deserialización del request/response funciona
- Que el repositorio real (InMemory) persiste y lee el estado correctamente entre scopes
- Que el worker resuelve sus dependencias desde el contenedor IoC real y opera sobre la DB correcta

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

| ID | Descripción | Nivel | Tipo | Estrategia de prueba | Técnica específica | Frontera / Condición | Input | Resultado esperado | Qué se mockea y por qué | RN / HU |
|----|-------------|-------|------|----------------------|-------------------|----------------------|-------|-------------------|--------------------------|---------|
| TC-001 | Registro exitoso con stock=0 | Unit | Validación | Caja negra sobre el handler | Partición de equivalencia (clase válida: stock=0, email OK, sin duplicado) | Camino feliz | email válido, stock=0, sin duplicado | 201, entryId + position | `ICatalogClient` → simula stock=0 sin red; `IWaitlistRepository` → aisla de BD real | HU-01 |
| TC-002 | Rechazo por stock disponible | Unit | Validación | Caja negra sobre el handler | Partición de equivalencia (clase inválida: stock > 0) | Error de negocio | email válido, stock=5 | `WaitlistConflictException` (409) | `ICatalogClient` → fuerza respuesta stock>0 sin red real | RN-02 |
| TC-003 | Rechazo por entrada duplicada | Unit | Validación | Caja negra sobre el handler | Partición de equivalencia (clase inválida: entrada activa existente) | Error de negocio | email ya en pending/assigned | `WaitlistConflictException` (409) | `IWaitlistRepository.HasActiveEntryAsync()` → retorna true sin consultar BD | RN-01 |
| TC-004 | Catalog no disponible | Unit | Validación | Error guessing (falla de servicio externo) | Inyección de fallo: `HttpRequestException` en cliente | Camino de resiliencia | Catalog arroja excepción de red | `WaitlistServiceUnavailableException` (503) | `ICatalogClient` → lanza `HttpRequestException` para simular caída sin red real | Resiliencia |
| TC-005 | Email inválido — formato | Unit | Validación | Caja negra sobre el validador | Partición de equivalencia (3 clases inválidas: sin @, sin dominio, sin TLD) `[Theory]` | Frontera de formato | "not-an-email", "@nodomain", "no-at-sign" | `ValidationError` (400) | Ninguno — FluentValidation es lógica pura sin I/O | Guard |
| TC-006 | Email válido — pasa validación | Unit | Validación | Caja negra sobre el validador | Valor de frontera: formato mínimo válido | Camino feliz | "user@example.com" | `IsValid = true` | Ninguno — FluentValidación es lógica pura | Guard |
| TC-007 | Asignación automática FIFO | Unit | Validación + Verificación | Caja negra + Oracle del comportamiento | Transición de estado + verificación de puerto | Camino feliz de asignación | Primer entry en cola pending | `entry.Status = assigned`, `UpdateAsync` llamado 1 vez, email enviado 1 vez | `IWaitlistRepository` → provee entry sin BD; `IOrderingClient` → simula creación de orden sin HTTP; `IEmailService` → verifica llamada sin SMTP | RN-03, RN-04 |
| TC-008 | Cola vacía — sin asignación | Unit | Validación + Verificación | Análisis de valores límite | Caso límite: lista con 0 elementos | Frontera de colección vacía | `GetNextPendingAsync` retorna null | No-op: `CreateWaitlistOrderAsync` nunca llamado, `UpdateAsync` nunca llamado | `IWaitlistRepository.GetNextPendingAsync()` → null sin BD | RN-03 |
| TC-009 | Idempotencia de asignación | Unit | Verificación | Prueba de idempotencia | Duplicate message: mismo seatId llega dos veces | Resiliencia a duplicados Kafka | seatId ya tiene entry assigned | Early return: `GetNextPendingAsync` nunca llamado | `IWaitlistRepository.HasAssignedEntryForSeatAsync()` → true sin BD | Idempotencia |
| TC-010 | Rotación con siguiente en cola | Unit | Validación + Verificación | Caja negra + Oracle | Transición de estado encadenada (2 entradas cambian estado) | Camino feliz de rotación | expired entry + next entry pending | `expired.Status=expired`, `next.Status=assigned`, `ReleaseSeatAsync` NUNCA llamado | `IWaitlistRepository` → provee ambas entradas; `IOrderingClient` → cancel + create; `IEmailService` → 2 llamadas verificadas; `IInventoryClient` → verificado que NO se llama | RN-05 |
| TC-011 | Liberación con cola vacía | Unit | Validación + Verificación | Análisis de valores límite | Caso límite: siguiente = null | Frontera de cola vacía en rotación | expired entry + `GetNextPendingAsync` null | `expired.Status=expired`, `ReleaseSeatAsync` llamado 1 vez, `CancelOrderAsync` llamado 1 vez | `IInventoryClient` → verifica llamada sin HTTP; `IOrderingClient` → verifica cancel sin HTTP | RN-06 |
| TC-012 | Sin expirados — worker no actúa | Unit | Verificación | Análisis de valores límite | Caso límite: lista de expirados vacía | Frontera de lista vacía | `GetExpiredAssignedAsync` retorna `[]` | No-op: `UpdateAsync`, `ReleaseSeatAsync`, `CancelOrderAsync` NUNCA llamados | Todos los puertos verificados con `Times.Never` | Worker |
| TC-013 | Completar asignación por pago | Unit | Validación + Verificación | Transición de estado | State transition: Assigned → Completed | Camino feliz de cierre | `payment-succeeded`, orderId en waitlist | `entry.Status = completed`, `UpdateAsync` llamado 1 vez | `IWaitlistRepository.GetByOrderIdAsync()` → entry sin BD | HU-02 |
| TC-014 | Completar — orden no es de waitlist | Unit | Verificación | Prueba de idempotencia | Caso nulo: orden externa llega al consumer | Resiliencia a eventos de otros servicios | orderId sin entry asociado | No-op: `UpdateAsync` NUNCA llamado | `IWaitlistRepository.GetByOrderIdAsync()` → null | Idempotencia |
| TC-015 | Consumer v3 — despacha comando | Unit | Verificación | Caja negra sobre el consumer | Deserialización + contrato de comando | Camino feliz del consumer | JSON con `seatId` + `concertEventId` válidos | `IMediator.Send(AssignNextCommand)` llamado 1 vez con IDs correctos | `IMediator` → captura el comando con callback para inspeccionar campos | Ciclo 15 |
| TC-016 | Consumer v2 — descarta payload | Unit | Verificación | Error guessing (compatibilidad de versiones) | Payload legacy sin campo `concertEventId` | Resiliencia a mensajes viejos | JSON sin `concertEventId` (Guid.Empty al deserializar) | `IMediator.Send()` NUNCA llamado | `IMediator` → verificado que no se invoca | Compat. v2 |
| TC-017 | Estado: Create válido | Unit | Validación | Caja blanca sobre la entidad | Estado inicial: verificación de todas las propiedades post-factory | Camino feliz | email válido, eventId válido | `Status=pending`, timestamps correctos, SeatId/OrderId null | Ninguno — dominio puro sin I/O | Dominio |
| TC-018 | Assign: estado inválido | Unit | Validación | Transición de estados | Guards de máquina de estados `[Theory]` sobre 3 estados no-pending | Frontera de estados prohibidos | entry en assigned / expired / completed | `InvalidOperationException` | Ninguno — dominio puro | Dominio |
| TC-019 | Complete: estado inválido | Unit | Validación | Transición de estados | Guards de máquina de estados `[Theory]` sobre 3 estados no-assigned | Frontera de estados prohibidos | entry en pending / expired / completed | `InvalidOperationException` | Ninguno — dominio puro | Dominio |
| TC-020 | Expire: estado inválido | Unit | Validación | Transición de estados | Guards de máquina de estados `[Theory]` sobre 3 estados no-assigned | Frontera de estados prohibidos | entry en pending / expired / completed | `InvalidOperationException` | Ninguno — dominio puro | Dominio |
| TC-021 | `IsExpired` — ExpiresAt en pasado | Unit | Validación | Análisis de valores límite | Valor límite: timestamp en pasado | Frontera de expiración | `ExpiresAt = now - 1 seg` | `true` | Ninguno — lógica pura | Dominio |
| TC-022 | `IsExpired` — ExpiresAt en futuro | Unit | Validación | Análisis de valores límite | Valor límite: timestamp en futuro | Frontera de no-expiración | `ExpiresAt = now + 30 min` | `false` | Ninguno — lógica pura | Dominio |
| TC-023 | `IsExpired` — estado Pending | Unit | Validación | Análisis de valores límite | Caso especial: Pending sin ExpiresAt | Frontera de null | entry sin `ExpiresAt` (estado pending) | `false` | Ninguno — lógica pura | Dominio |
| TC-024 | Create con email vacío/espacios | Unit | Validación | Partición de equivalencia | Clase inválida: string vacío y whitespace `[Theory]` | Frontera de string vacío | `""`, `"   "` | `ArgumentException` | Ninguno — dominio puro | Guard |
| TC-025 | Create con EventId vacío | Unit | Validación | Análisis de valores límite | Valor límite: `Guid.Empty` | Frontera del GUID | `Guid.Empty` | `ArgumentException` | Ninguno — dominio puro | Guard |
| TI-01 | Registro exitoso — ensamblaje controlador → handler → repositorio | Integración | Validación | WebApplicationFactory + EF InMemory | Caja negra sobre el endpoint real con pipeline MediatR y DB in-memory | Camino feliz end-to-end del ensamblaje | `POST /api/v1/waitlist/join` — email válido, stock=0 | `201 Created`, `entryId` no vacío, `position=1`, entry persistida en DB con `Status=pending` | `ICatalogClient` → stock=0; `IOrderingClient`, `IInventoryClient`, `IEmailService` → sin actividad esperada | HU-01 |
| TI-02 | Rechazo por stock disponible — mapeo excepción → 409 | Integración | Validación | WebApplicationFactory + EF InMemory | Caja negra: verifica que el controlador traduce `WaitlistConflictException` al código HTTP correcto | Error de negocio a través del pipeline real | `POST /api/v1/waitlist/join` — stock=5 | `409 Conflict`, campo `message` presente | `ICatalogClient` → stock=5 | RN-02 |
| TI-03 | Duplicado — mapeo excepción → 409 con mensaje | Integración | Validación | WebApplicationFactory + EF InMemory | Caja negra: entry pre-sembrada en DB; verifica pipeline MediatR + repositorio real | Duplicado activo detectado por repositorio InMemory | `POST /api/v1/waitlist/join` — email ya en cola (pendiente en DB) | `409 Conflict`, `message` contiene "lista de espera" | `ICatalogClient` → stock=0; repositorio usa DB real InMemory | RN-01 |
| TI-04 | `AssignNextCommand` con entry pendiente — asignación real | Integración | Validación + Verificación | WebApplicationFactory + EF InMemory | Oracle: comando enviado via `IMediator` real; repositorio InMemory persiste el cambio | Camino feliz del handler con DB real | `AssignNextCommand(seatId, eventId)` — 1 entry pending en DB | `entry.Status=assigned`, `SeatId` y `OrderId` correctos en DB; `CreateWaitlistOrderAsync` y `SendAsync` llamados 1 vez cada uno | `IOrderingClient` → retorna `orderId`; `IEmailService` → `true` | RN-03, RN-04 |
| TI-05 | `AssignNextCommand` cola vacía — no-op real | Integración | Verificación | WebApplicationFactory + EF InMemory | Análisis de valores límite: comando sobre evento sin entries; repositorio real retorna null | Frontera de cola vacía con DB real | `AssignNextCommand(seatId, eventId)` — DB vacía para ese evento | `CreateWaitlistOrderAsync` nunca llamado; `ReleaseSeatAsync` nunca llamado | `IOrderingClient`, `IInventoryClient` → verificados con `Times.Never` | RN-03 |
| TI-06 | `WaitlistExpiryWorker` con siguiente en cola — rotación sin inventario | Integración | Validación + Verificación | WebApplicationFactory + EF InMemory | Oracle de estado encadenado: worker instanciado directamente con `IServiceScopeFactory` real; 2 entries en DB | Camino feliz de rotación con DB real | `ProcessExpiredEntriesAsync()` — entry assigned+expirada + entry pending en DB | `expiring@test.com` → `Status=expired`; `next@test.com` → `Status=assigned`; `CancelOrderAsync` × 1, `CreateWaitlistOrderAsync` × 1; `ReleaseSeatAsync` NUNCA llamado | `IOrderingClient` → cancel + create; `IEmailService` → `true`; `IInventoryClient` → verificado con `Times.Never` | RN-05 |
| TI-07 | `WaitlistExpiryWorker` cola vacía — liberación al inventario | Integración | Validación + Verificación | WebApplicationFactory + EF InMemory | Análisis de valores límite: worker con 1 sola entry expirada y sin siguiente en cola | Frontera de cola vacía en rotación con DB real | `ProcessExpiredEntriesAsync()` — solo 1 entry assigned+expirada, sin pending | `expiring@test.com` → `Status=expired`; `ReleaseSeatAsync` × 1; `CancelOrderAsync` × 1; `CreateWaitlistOrderAsync` NUNCA llamado | `IInventoryClient` → release; `IOrderingClient` → cancel; `IEmailService` → `true` | RN-06 |
| TC-E01 | Endpoint /health arranca | E2E | Caja negra | Prueba de sistema completo | Smoke test: servicio arranca y responde | Disponibilidad básica | `GET /health` | `200 OK` | Ninguno — sistema real completo con Docker Compose | Infraestructura |
| TC-E02 | Flujo /join end-to-end | E2E | Caja negra | Prueba de sistema completo | Flujo real con servicios reales | Contrato externo del endpoint | `POST /api/v1/waitlist/join` con evento real | `201 Created` o `409 Conflict` | Ninguno — sistema real completo | HU-01 |

---

## 6. Estrategias de prueba por capa — qué usamos y por qué

### Mapa estrategia → capa

| Capa | Estrategia principal | Técnicas aplicadas | Por qué esta estrategia |
|------|---------------------|-------------------|------------------------|
| **Dominio** (`WaitlistEntry`) | Caja blanca | Transición de estados, partición de equivalencia, análisis de valores límite | El dominio es lógica pura sin I/O — se puede verificar directamente cada invariante y guard sin ningún mock |
| **Aplicación** (Handlers) | Caja negra + Oracle del comportamiento | Partición de equivalencia, error guessing, prueba de idempotencia, verificación de puertos | Los handlers orquestan servicios externos — se prueba el comportamiento observable (estado resultante + puertos invocados) sin saber cómo está implementado el repositorio o el cliente HTTP |
| **Infraestructura** (Worker, Consumer) | Caja gris | Análisis de valores límite (lista vacía / null), idempotencia, error guessing | El worker tiene lógica de decisión propia (rota vs. libera) y el consumer tiene lógica de filtrado (v2 vs. v3) — se verifica tanto el comportamiento como que los puertos correctos se invocan el número exacto de veces |
| **Sistema** (E2E) | Caja negra total | Smoke test, contrato de endpoint | El sistema real corre en Docker Compose — solo se observa el comportamiento externo; ningún detalle interno es visible ni relevante |

---

### Fronteras identificadas y cómo se cubren

| Frontera | Qué puede fallar | TC que la cubre | Técnica |
|----------|-----------------|-----------------|---------|
| **Stock = 0 vs. Stock > 0** | Aceptar cuando hay asientos disponibles | TC-001, TC-002 | Partición de equivalencia |
| **Email vacío / solo espacios** | No rechazar strings whitespace | TC-024 `[Theory]` | Partición de equivalencia |
| **Guid.Empty en EventId** | Crear entry con ID nulo | TC-025 | Análisis de valores límite |
| **ExpiresAt en el pasado exacto** | No detectar expiración por off-by-one | TC-021, TC-022, TC-023 | Análisis de valores límite |
| **Cola con 0 elementos** | Intentar asignar cuando la cola está vacía | TC-008, TC-012 | Análisis de valores límite |
| **Siguiente entry = null** | Error en rotación cuando no hay sucesor | TC-011 | Análisis de valores límite |
| **Mismo seatId recibido dos veces** | Asignar dos veces el mismo asiento | TC-009 | Prueba de idempotencia |
| **OrderId que no pertenece a waitlist** | Marcar como completed una orden externa | TC-014 | Prueba de idempotencia |
| **Payload Kafka v2 sin `concertEventId`** | Procesar mensajes legados como válidos | TC-016 | Error guessing (compatibilidad) |
| **Transición de estado inválida** | Asignar un entry ya expirado | TC-018, TC-019, TC-020 `[Theory]` | Transición de estados |
| **Servicio externo caído** | Propagar error incorrecto al caller | TC-004 | Error guessing (inyección de fallo) |

---

### Verificar vs. Validar — ejemplos concretos del código

**Verificar** (confirma que el sistema llamó al puerto correcto):
```csharp
// TC-009 — Idempotencia: el repositorio NO fue consultado para buscar el siguiente
_repoMock.Verify(r => r.GetNextPendingAsync(It.IsAny<Guid>()), Times.Never);

// TC-010 — Rotación: el inventario NO fue liberado (asiento se transfiere, no se suelta)
_inventoryMock.Verify(i => i.ReleaseSeatAsync(It.IsAny<Guid>()), Times.Never);

// TC-015 — Consumer v3: el mediator fue invocado exactamente una vez con el comando correcto
_mediatorMock.Verify(m => m.Send(It.IsAny<AssignNextCommand>(), default), Times.Once);
```

**Validar** (confirma que la regla de negocio produce el estado correcto):
```csharp
// TC-007 — FIFO: el entry asignado tiene los campos correctos
entry.Status.Should().Be(WaitlistStatus.Assigned);
entry.SeatId.Should().Be(expectedSeatId);
entry.OrderId.Should().Be(expectedOrderId);

// TC-010 — Rotación: el entry expirado cambió de estado, el siguiente fue asignado
expiredEntry.Status.Should().Be(WaitlistStatus.Expired);
nextEntry.Status.Should().Be(WaitlistStatus.Assigned);

// TC-022 — Dominio: la expiración no se activa antes de tiempo
entry.IsAssignmentExpired().Should().BeFalse();
```

---

### Por qué mockeamos lo que mockeamos

| Puerto mockeado | En qué tests | Razón del mock |
|-----------------|-------------|---------------|
| `IWaitlistRepository` | TC-001 a TC-016 (todos los handlers) | Aislar de PostgreSQL — los tests deben correr en < 10ms sin BD levantada |
| `ICatalogClient` | TC-001, TC-002, TC-004 | Aislar de HTTP — `GetAvailableCountAsync` requiere red real y Catalog Service corriendo |
| `IOrderingClient` | TC-007, TC-009, TC-010, TC-011 | Aislar de HTTP — `CreateWaitlistOrderAsync` y `CancelOrderAsync` requieren Ordering Service |
| `IInventoryClient` | TC-010, TC-011, TC-012 | Aislar de HTTP — `ReleaseSeatAsync` requiere Inventory Service; además se verifica que NO se llame en rotación |
| `IEmailService` | TC-007, TC-010 | Aislar de SMTP — side effect externo; además se verifica cantidad exacta de llamadas |
| `IMediator` | TC-015, TC-016 | Aislar del pipeline de MediatR — probar solo la lógica de deserialización y despacho del consumer |
| `IServiceScopeFactory` | TC-010, TC-011, TC-012 (Worker) | El worker usa DI scoped — el scope factory se mockea para inyectar todos los puertos sin levantar el contenedor IoC real |

---

## 7. QA Gates — Criterios de calidad no negociables

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
