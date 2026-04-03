# Tasks: Corrección de Arquitectura — Audit Fix

**Feature**: `002-arch-audit-fix`
**Total tasks**: 36
**Generated**: 2026-04-02

## Phase 1 — Setup (verificación de estado inicial)

- [x] T001 Verificar que los servicios compilan antes de iniciar cambios: `dotnet build services/inventory/src/Inventory.Application/Inventory.Application.csproj`
- [x] T002 Verificar que todos los tests de Ordering pasan: `dotnet test services/ordering/tests/unit/Ordering.Domain.UnitTests/`

## Phase 2 — [US1] Reubicar puertos de Inventory.Domain → Inventory.Application

- [ ] T003 [US1] Crear directorio `services/inventory/src/Inventory.Application/Ports/` y crear `IRedisLock.cs` con namespace `Inventory.Application.Ports` (copiar contrato desde Domain)
- [ ] T004 [US1] Crear `services/inventory/src/Inventory.Application/Ports/IKafkaProducer.cs` con namespace `Inventory.Application.Ports`
- [ ] T005 [US1] Eliminar `services/inventory/src/Inventory.Domain/Ports/IRedisLock.cs`
- [ ] T006 [US1] Eliminar `services/inventory/src/Inventory.Domain/Ports/IKafkaProducer.cs`
- [ ] T007 [US1] Crear `services/inventory/src/Inventory.Application/Ports/ISeatRepository.cs` con namespace `Inventory.Application.Ports`: métodos `GetByIdAsync(Guid, CancellationToken)` y `UpdateAsync(Seat, CancellationToken)`
- [ ] T008 [US1] Crear `services/inventory/src/Inventory.Application/Ports/IReservationRepository.cs` con namespace `Inventory.Application.Ports`: método `CreateAsync(Reservation, CancellationToken)`
- [ ] T009 [US1] Crear `services/inventory/src/Inventory.Infrastructure/Persistence/SeatRepository.cs` implementando `ISeatRepository` usando `InventoryDbContext`
- [ ] T010 [US1] Crear `services/inventory/src/Inventory.Infrastructure/Persistence/ReservationRepository.cs` implementando `IReservationRepository` usando `InventoryDbContext`
- [ ] T011 [US1] Actualizar `services/inventory/src/Inventory.Infrastructure/ServiceCollectionExtensions.cs`: registrar `ISeatRepository` → `SeatRepository` e `IReservationRepository` → `ReservationRepository`
- [ ] T012 [US1] Refactorizar `services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommandHandler.cs`: reemplazar `InventoryDbContext` por `ISeatRepository` e `IReservationRepository`; actualizar usings a `Inventory.Application.Ports`
- [ ] T013 [US1] Actualizar cualquier archivo en `Inventory.Infrastructure` que usaba `Inventory.Domain.Ports.IRedisLock` o `IKafkaProducer` — cambiar using a `Inventory.Application.Ports`
- [ ] T014 [US1] Verificar que `Inventory.Application.csproj` NO tiene `<ProjectReference>` a `Inventory.Infrastructure` — si existe, eliminarlo

## Phase 3 — [US1] Reubicar puertos de Identity.Domain → Identity.Application

- [ ] T015 [P] [US1] Crear directorio `services/identity/src/Identity.Application/Ports/` y crear `IPasswordHasher.cs` con namespace `Identity.Application.Ports`
- [ ] T016 [P] [US1] Crear `services/identity/src/Identity.Application/Ports/ITokenGenerator.cs` con namespace `Identity.Application.Ports`
- [ ] T017 [P] [US1] Crear `services/identity/src/Identity.Application/Ports/IUserRepository.cs` con namespace `Identity.Application.Ports`
- [ ] T018 [US1] Eliminar `services/identity/src/Identity.Domain/Ports/IPasswordHasher.cs`
- [ ] T019 [US1] Eliminar `services/identity/src/Identity.Domain/Ports/ITokenGenerator.cs`
- [ ] T020 [US1] Eliminar `services/identity/src/Identity.Domain/Ports/IUserRepository.cs`
- [ ] T021 [US1] Actualizar todos los `using Identity.Domain.Ports` → `Identity.Application.Ports` en archivos de Application e Infrastructure de Identity: `CreateUserHandler.cs`, `IssueTokenHandler.cs`, `UserRepository.cs`, `BcryptPasswordHasher.cs`, `JwtTokenGenerator.cs`, `ServiceCollectionExtensions.cs`

## Phase 4 — [US1] Reubicar IEventPublisher de Fulfillment.Infrastructure → Fulfillment.Application

- [ ] T022 [US1] Crear `services/fulfillment/src/Fulfillment.Application/Ports/IEventPublisher.cs` con namespace `Fulfillment.Application.Ports` (mismo contrato: `Task<bool> PublishAsync<T>(string, string, T)`)
- [ ] T023 [US1] Eliminar `services/fulfillment/src/Fulfillment.Infrastructure/Events/IEventPublisher.cs`
- [ ] T024 [US1] Actualizar `services/fulfillment/src/Fulfillment.Infrastructure/Events/KafkaEventPublisher.cs`: cambiar `using Fulfillment.Infrastructure.Events` → `using Fulfillment.Application.Ports` en la implementación de `IEventPublisher`
- [ ] T025 [US1] Actualizar `services/fulfillment/src/Fulfillment.Application/UseCases/ProcessPaymentSucceeded/ProcessPaymentSucceededHandler.cs`: cambiar using a `Fulfillment.Application.Ports`

## Phase 5 — [US2] Enriquecer Order (rich domain model)

- [ ] T026 [US2] Reescribir `services/ordering/src/Ordering.Domain/Entities/Order.cs`: agregar constantes de estado, factory method `Order.Create()`, método `AddItem(seatId, price)`, método `Checkout()`, método `MarkAsPaid()`, hacer privados los setters de `State`, `TotalAmount` e `Items`
- [ ] T027 [US2] Actualizar `services/ordering/src/Ordering.Application/UseCases/AddToCart/AddToCartHandler.cs`: usar `Order.Create()` y `order.AddItem()` en lugar de object initializers; eliminar `catch (Exception ex)` genérico
- [ ] T028 [US2] Actualizar `services/ordering/src/Ordering.Application/UseCases/CheckoutOrder/CheckoutOrderHandler.cs`: usar `order.Checkout()` en lugar de `order.State = "pending"`; eliminar `catch (Exception ex)` genérico; eliminar validación de estado (ahora en dominio)
- [ ] T029 [US2] Actualizar tests `services/ordering/tests/unit/Ordering.Domain.UnitTests/OrderTests.cs`: reemplazar tests de property setters por tests de comportamiento (Checkout sin items lanza excepción, AddItem recalcula total, etc.)

## Phase 6 — [US3] Enriquecer Reservation y Seat (Inventory domain)

- [ ] T030 [US3] Actualizar `services/inventory/src/Inventory.Domain/Entities/Reservation.cs`: agregar constantes `StatusActive`, `StatusExpired`, `StatusConfirmed`; agregar factory method `Reservation.Create(seatId, customerId, ttlMinutes)`; hacer setter de `Status` privado
- [ ] T031 [US3] Actualizar `services/inventory/src/Inventory.Domain/Entities/Seat.cs`: agregar método `Reserve()` con guard `if (Reserved) throw new InvalidOperationException(...)` y método `Release()`
- [ ] T032 [US3] Actualizar `services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommandHandler.cs`: usar `Reservation.Create(...)` y `seat.Reserve()` en lugar de object initializers con magic strings

## Phase 7 — [US4] Reemplazar magic strings con constantes en Payment y corregir bug

- [ ] T033 [US4] Actualizar `services/payment/src/Payment.Application/UseCases/ProcessPayment/ProcessPaymentHandler.cs`: reemplazar `p.Status == "succeeded" || p.Status == "completed"` por `p.Status == Payment.StatusSucceeded`; reemplazar `Status = "pending"` por `Payment.StatusPending`

## Phase 8 — [US6] DTO tipado y eliminar hardcodes en OrdersController

- [ ] T034 [US6] Crear `services/ordering/src/Ordering.Application/DTOs/OrderEnrichmentDto.cs` con record `OrderEnrichmentDto(Guid OrderId, string? CustomerEmail, Guid? EventId, string? EventName, string? SeatNumber, decimal Price, string Currency)`
- [ ] T035 [US6] Actualizar `services/ordering/src/Ordering.Api/Controllers/OrdersController.cs`: reemplazar anonymous type por `OrderEnrichmentDto`; reemplazar `"guest@example.com"` por `null`

## Phase 9 — Validación final

- [ ] T036 Ejecutar `dotnet build` en todos los servicios afectados y confirmar cero errores: `dotnet build services/inventory/src/ && dotnet build services/identity/src/ && dotnet build services/fulfillment/src/ && dotnet build services/ordering/src/`

## Dependencies

```
T003-T008 → T009-T011 → T012 → T013 → T014
T015-T017 → T018-T020 → T021
T022 → T023 → T024 → T025
T026 → T027 → T028 → T029
T030 → T031 → T032
T033 (independiente)
T034 → T035
T036 (depende de todos los anteriores)
```

## Parallel Opportunities

- T015, T016, T017 pueden ejecutarse en paralelo (archivos distintos)
- T003, T004 pueden ejecutarse en paralelo
- T018, T019, T020 pueden ejecutarse en paralelo
- T030, T031 pueden ejecutarse en paralelo (entidades distintas)
- T033, T034 pueden ejecutarse en paralelo (servicios distintos)
