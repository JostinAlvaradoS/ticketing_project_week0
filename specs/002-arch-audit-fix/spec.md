# Feature Specification: Corrección de Arquitectura — Audit Fix

**Feature Branch**: `002-arch-audit-fix`
**Created**: 2026-04-02
**Status**: Ready for Planning
**Input**: Hallazgos de auditoría de Clean Architecture + Hexagonal + Clean Code sobre el codebase del ticketing platform.

## Overview

Corrección sistemática de 15 violaciones detectadas en auditoría de arquitectura distribuidas en 7 microservicios .NET. Los hallazgos se agrupan en 4 categorías: violaciones de la regla de dependencias, domain model anémico, incumplimientos de clean code y deuda técnica que impacta correctitud del sistema.

Todos los cambios deben mantener el comportamiento externo de los servicios (contratos HTTP y eventos Kafka no cambian).

## User Scenarios & Testing

### User Story 1 — Corregir inversión de dependencias: puertos mal ubicados y DbContext en Application (Priority: P1)

El handler `CreateReservationCommandHandler` en Inventory.Application inyecta `InventoryDbContext` directamente. Los puertos `IRedisLock`/`IKafkaProducer` están en `Inventory.Domain.Ports`. En Identity, `IPasswordHasher`, `ITokenGenerator` e `IUserRepository` están en `Identity.Domain.Ports`. En Fulfillment, `IEventPublisher` vive en `Fulfillment.Infrastructure.Events`.

**Why this priority**: Es la violación más grave de la regla de dependencias. Un handler que conoce DbContext no puede ser testeado sin levantar base de datos real.

**Independent Test**: `Inventory.Application.csproj` no debe tener `<ProjectReference>` a `Inventory.Infrastructure`. Tests unitarios de Application deben pasar sin DbContext.

**Acceptance Scenarios**:

1. **Given** `CreateReservationCommandHandler`, **When** se revisan sus dependencias inyectadas, **Then** solo interfaces de `Application.Ports` — sin `InventoryDbContext`, sin tipos de Infrastructure.
2. **Given** `Inventory.Application.csproj`, **When** se listan `<ProjectReference>`, **Then** NO existe referencia a `Inventory.Infrastructure`.
3. **Given** namespace `Inventory.Application.Ports`, **When** se listan archivos, **Then** existen `IRedisLock.cs`, `IKafkaProducer.cs`, `ISeatRepository.cs`, `IReservationRepository.cs`.
4. **Given** namespace `Identity.Application.Ports`, **When** se listan archivos, **Then** existen `IPasswordHasher.cs`, `ITokenGenerator.cs`, `IUserRepository.cs`.
5. **Given** namespace `Fulfillment.Application.Ports`, **When** se listan archivos, **Then** existe `IEventPublisher.cs` con namespace `Fulfillment.Application.Ports`.

---

### User Story 2 — Enriquecer Domain Model de Ordering: eliminar anemia de Order (Priority: P1)

`Order` tiene todos los setters públicos, sin factory method, sin métodos de dominio. La lógica de negocio (transiciones de estado, cálculo de totales, validación de pertenencia) vive en los handlers de Application.

**Why this priority**: Sin dominio rico las reglas de negocio no tienen un hogar único — están duplicadas y sin protección.

**Independent Test**: Tests en `Ordering.Domain.UnitTests` validan comportamiento del dominio (transiciones de estado, reglas de negocio) — no solo property getters.

**Acceptance Scenarios**:

1. **Given** la clase `Order`, **When** se llama `Order.Create(userId, guestToken)`, **Then** retorna instancia con `State = OrderState.Draft`, `Id` generado y `CreatedAt = UtcNow`.
2. **Given** `Order` en `Draft`, **When** se llama `order.AddItem(seatId, price)`, **Then** agrega `OrderItem` y `TotalAmount` se recalcula automáticamente.
3. **Given** `Order` en `Draft` con ítems, **When** se llama `order.Checkout()`, **Then** estado cambia a `OrderState.Pending`.
4. **Given** `Order` en `Draft` sin ítems, **When** se llama `order.Checkout()`, **Then** lanza `InvalidOperationException("Order is empty")`.
5. **Given** `Order` en estado distinto a `Draft`, **When** se llama `order.Checkout()`, **Then** lanza `InvalidOperationException`.
6. **Given** `Order`, **When** se inspeccionan setters de `State`, `TotalAmount` e `Items`, **Then** son privados o `init`-only.
7. **Given** `AddToCartHandler` y `CheckoutOrderHandler`, **When** se revisa el código, **Then** no contienen lógica de transición de estados ni cálculos de `TotalAmount`.

---

### User Story 3 — Enriquecer Domain Model de Inventory: factory methods en Reservation y Seat (Priority: P2)

`Reservation` se construye con object initializer y magic strings en el handler. `Seat` no tiene método `Reserve()`.

**Acceptance Scenarios**:

1. **Given** clase `Reservation`, **When** se llama `Reservation.Create(seatId, customerId, ttlMinutes)`, **Then** retorna instancia con `Status = ReservationStatus.Active`, `CreatedAt = UtcNow`, `ExpiresAt = CreatedAt + ttlMinutes`.
2. **Given** `Seat` disponible, **When** se llama `seat.Reserve()`, **Then** `Reserved = true`.
3. **Given** `Seat` ya reservado, **When** se llama `seat.Reserve()`, **Then** lanza `InvalidOperationException`.
4. **Given** `CreateReservationCommandHandler`, **When** construye `Reservation`, **Then** usa `Reservation.Create(...)` — no object initializer con magic strings.

---

### User Story 4 — Eliminar magic strings de estados de dominio (Priority: P2)

Estados como `"draft"`, `"pending"`, `"active"`, `"expired"` están como literales dispersos. Además `ProcessPaymentHandler` compara con `"completed"` que no existe en el modelo (bug de idempotencia silencioso).

**Acceptance Scenarios**:

1. **Given** entidad `Order`, **When** se listan sus constantes, **Then** existen `StateDraft`, `StatePending`, `StatePaid`, `StateFulfilled`, `StateCancelled`.
2. **Given** entidad `Reservation`, **When** se listan sus constantes, **Then** existen `StatusActive`, `StatusExpired`, `StatusConfirmed`.
3. **Given** `ProcessPaymentHandler`, **When** se revisa la verificación de idempotencia, **Then** usa `Payment.StatusSucceeded` — no el string `"completed"`.
4. **Given** todos los `.cs` del proyecto, **When** `grep '"completed"'` en comparaciones de pago, **Then** cero resultados.

---

### User Story 5 — Corregir swallowing de excepciones en handlers de Ordering (Priority: P2)

`AddToCartHandler` y `CheckoutOrderHandler` tienen `catch (Exception ex)` genérico que convierte errores de sistema en `{ success: false }` con `200 OK`.

**Acceptance Scenarios**:

1. **Given** `AddToCartHandler`, **When** el repositorio lanza `TaskCanceledException`, **Then** la excepción se propaga — no se convierte en respuesta fallida.
2. **Given** `CheckoutOrderHandler`, **When** el repositorio lanza `DbUpdateException`, **Then** la excepción se propaga — no se convierte en respuesta fallida.
3. **Given** los handlers, **When** se revisa el código, **Then** no existe `catch (Exception ex)` genérico.

---

### User Story 6 — Corregir anonymous type y hardcode en OrdersController (Priority: P3)

`GetOrderDetails` retorna `new { ... }` anonymous type y tiene `"guest@example.com"` hardcodeado.

**Acceptance Scenarios**:

1. **Given** `GET /orders/{id}`, **When** se revisa la respuesta, **Then** retorna DTO tipado `OrderEnrichmentDto` definido en `Ordering.Application.DTOs`.
2. **Given** pedido de guest, **When** se obtiene `CustomerEmail`, **Then** devuelve `null` — no email hardcodeado.

---

### Edge Cases

- Cambios al Domain Model de Ordering **NO** deben romper `Ordering.IntegrationTests`.
- Mover puertos en Inventory e Identity **NO** debe cambiar comportamiento en runtime — solo reubicación de interfaces.
- Las constantes de estado deben mapear exactamente a los mismos strings que ya existen en la base de datos.
- Los namespaces de las clases `Infrastructure` que implementan los puertos reubicados deben actualizarse para usar el nuevo `using`.

## Requirements

### Functional Requirements

- **FR-001**: `Inventory.Application` NO DEBE tener `<ProjectReference>` a `Inventory.Infrastructure`.
- **FR-002**: `IRedisLock` e `IKafkaProducer` DEBEN estar en `Inventory.Application.Ports`.
- **FR-003**: `IPasswordHasher`, `ITokenGenerator` e `IUserRepository` DEBEN estar en `Identity.Application.Ports`.
- **FR-004**: `IEventPublisher` DEBE estar en `Fulfillment.Application.Ports`.
- **FR-005**: `CreateReservationCommandHandler` DEBE inyectar `ISeatRepository` e `IReservationRepository` — no `InventoryDbContext`.
- **FR-006**: `Order` DEBE tener factory method `Order.Create()` y métodos `AddItem()`, `Checkout()`.
- **FR-007**: Los setters de `Order.State`, `Order.TotalAmount` e `Order.Items` DEBEN ser privados o `init`-only.
- **FR-008**: `AddToCartHandler` NO DEBE contener `new Order { ... }` ni `new OrderItem { ... }` con object initializer.
- **FR-009**: `CheckoutOrderHandler` NO DEBE asignar `order.State = "pending"` directamente.
- **FR-010**: `Reservation` DEBE tener factory method `Reservation.Create(seatId, customerId, ttlMinutes)`.
- **FR-011**: `Seat` DEBE tener método `Reserve()` con validación de estado previo.
- **FR-012**: `Order`, `Reservation` y `Payment` DEBEN expresar sus estados como constantes en la entidad.
- **FR-013**: `ProcessPaymentHandler` DEBE usar `Payment.StatusSucceeded` en la verificación de idempotencia.
- **FR-014**: `AddToCartHandler` y `CheckoutOrderHandler` NO DEBEN tener `catch (Exception ex)` genérico.
- **FR-015**: `OrdersController.GetOrderDetails` DEBE retornar `OrderEnrichmentDto` tipado, no anonymous type.
- **FR-016**: Ningún email hardcodeado DEBE aparecer en capas API o Application.

### Key Entities

- **Order**: Agregado raíz de Ordering. Comportamiento encapsulado en métodos, no setters públicos.
- **OrderItem**: Entidad hija. Construcción controlada por `Order.AddItem()`.
- **Reservation**: Entidad de Inventory. Creación solo a través de `Reservation.Create()`.
- **Seat**: Entidad de Inventory. Expone `Reserve()` como único punto de transición de estado.

## Success Criteria

### Measurable Outcomes

- **SC-01**: `grep -r "InventoryDbContext" services/inventory/src/Inventory.Application` retorna cero resultados.
- **SC-02**: `grep -r "guest@example.com" services/` retorna cero resultados.
- **SC-03**: `grep -r '"completed"' services/payment/src` en contexto de comparación de estado retorna cero resultados.
- **SC-04**: Todos los tests unitarios de `*.Domain.UnitTests` y `*.Application.UnitTests` pasan.
- **SC-05**: El proyecto compila sin errores (`dotnet build` exitoso en todos los servicios afectados).
- **SC-06**: Los handlers `AddToCartHandler` y `CheckoutOrderHandler` no contienen `catch (Exception)` genérico.
