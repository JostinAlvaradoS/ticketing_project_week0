# Implementation Plan: Corrección de Arquitectura — Audit Fix

**Branch**: `002-arch-audit-fix` | **Date**: 2026-04-02 | **Spec**: [spec.md](spec.md)

## Summary

Corrección de 15 violaciones de Clean Architecture + Hexagonal + Clean Code detectadas en auditoría. El trabajo se divide en:
1. Reubicar puertos mal ubicados (Domain→Application, Infrastructure→Application)
2. Crear repositorios de puerto en Inventory y actualizar su handler para no usar DbContext directamente
3. Enriquecer el Domain Model de Ordering (Order con factory method y métodos de dominio)
4. Agregar factory methods y métodos de comportamiento en Reservation y Seat (Inventory)
5. Reemplazar magic strings por constantes en Order, Reservation y Payment
6. Corregir swallowing de excepciones en handlers de Ordering
7. Extraer DTO tipado en OrdersController y eliminar hardcodes

## Technical Context

**Language/Version**: C# 12 / .NET 8
**Primary Dependencies**: MediatR 12.2.0, EF Core 8, Confluent.Kafka 2.5.0, StackExchange.Redis
**Storage**: PostgreSQL 17 (schema `bc_ordering`, `bc_inventory`, `bc_identity`, `bc_fulfillment`)
**Testing**: xUnit + FluentAssertions + Moq (Application UnitTests), WebApplicationFactory (Integration)
**Target Platform**: Docker Compose (microservices)
**Project Type**: Microservices — refactoring interno, sin cambios de contrato externo
**Constraints**: Contratos HTTP y eventos Kafka no cambian; valores string de estados en DB no cambian

## Constitution Check

| Principio | Estado | Notas |
|---|---|---|
| Hexagonal Architecture | ✅ PASS | El objetivo de esta feature ES corregir violaciones hexagonales |
| Base de datos (shared PostgreSQL) | ✅ PASS | Sin cambios de schema ni migraciones — solo refactoring de capas |
| DbContext / Migrations | ✅ PASS | Se elimina DbContext de Application; queda solo en Infrastructure |
| Comunicación (HTTP + Kafka) | ✅ PASS | Contratos no cambian — refactoring interno |
| Transacciones | ✅ PASS | Sin cambios de modelo transaccional |
| Local Dev Topology | ✅ PASS | Sin cambios en docker-compose |
| Security | ✅ PASS | Sin cambios en JWT ni flujos de auth |
| Testing | ✅ PASS | Tests unitarios existentes deben seguir pasando; se actualizan para cubrir nuevos métodos de dominio |

## Project Structure

### Archivos afectados por servicio

```text
services/
├── inventory/
│   ├── src/Inventory.Application/
│   │   ├── Ports/
│   │   │   ├── IRedisLock.cs              [MOVER desde Domain]
│   │   │   ├── IKafkaProducer.cs          [MOVER desde Domain]
│   │   │   ├── ISeatRepository.cs         [NUEVO]
│   │   │   └── IReservationRepository.cs  [NUEVO]
│   │   └── UseCases/CreateReservation/
│   │       └── CreateReservationCommandHandler.cs  [REFACTOR — eliminar DbContext]
│   ├── src/Inventory.Domain/
│   │   ├── Ports/
│   │   │   ├── IRedisLock.cs              [ELIMINAR — movido]
│   │   │   └── IKafkaProducer.cs          [ELIMINAR — movido]
│   │   └── Entities/
│   │       ├── Reservation.cs             [ENRIQUECER — factory method]
│   │       └── Seat.cs                    [ENRIQUECER — Reserve()]
│   └── src/Inventory.Infrastructure/
│       └── Persistence/
│           ├── SeatRepository.cs          [NUEVO — implementa ISeatRepository]
│           └── ReservationRepository.cs   [NUEVO — implementa IReservationRepository]
│
├── identity/
│   ├── src/Identity.Application/
│   │   └── Ports/                         [NUEVO directorio]
│   │       ├── IPasswordHasher.cs         [MOVER desde Domain]
│   │       ├── ITokenGenerator.cs         [MOVER desde Domain]
│   │       └── IUserRepository.cs         [MOVER desde Domain]
│   └── src/Identity.Domain/
│       └── Ports/
│           ├── IPasswordHasher.cs         [ELIMINAR — movido]
│           ├── ITokenGenerator.cs         [ELIMINAR — movido]
│           └── IUserRepository.cs         [ELIMINAR — movido]
│
├── fulfillment/
│   ├── src/Fulfillment.Application/
│   │   └── Ports/
│   │       └── IEventPublisher.cs         [NUEVO — mover desde Infrastructure]
│   └── src/Fulfillment.Infrastructure/
│       └── Events/
│           └── IEventPublisher.cs         [ELIMINAR — movido]
│
└── ordering/
    ├── src/Ordering.Domain/
    │   └── Entities/
    │       └── Order.cs                   [ENRIQUECER — factory, AddItem(), Checkout(), constantes]
    ├── src/Ordering.Application/
    │   ├── DTOs/
    │   │   └── OrderEnrichmentDto.cs      [NUEVO]
    │   └── UseCases/
    │       ├── AddToCart/
    │       │   └── AddToCartHandler.cs    [REFACTOR — usar domain methods, quitar catch genérico]
    │       └── CheckoutOrder/
    │           └── CheckoutOrderHandler.cs [REFACTOR — usar order.Checkout(), quitar catch genérico]
    └── src/Ordering.Api/
        └── Controllers/
            └── OrdersController.cs        [REFACTOR — DTO tipado, eliminar hardcode]
```

## Phase 0: Research

### Decisiones técnicas

**D1: Constantes vs Enum para estados de dominio**
- Decisión: Constantes `public const string` en la entidad (no enum de C#)
- Rationale: EF Core mapea strings directamente; usar enum requeriría value converters y cambios de migración. Las constantes logran el mismo beneficio de evitar typos sin complejidad adicional.
- Alternativas: `enum OrderState` con `[Column(TypeName = "varchar")]` — descartado por complejidad de migración.

**D2: ISeatRepository vs mantener acceso a DbSet en handler**
- Decisión: Crear `ISeatRepository` e `IReservationRepository` en Application.Ports
- Rationale: Permite testear `CreateReservationCommandHandler` con mocks sin EF Core.
- Alternativas: Patrón Unit of Work genérico — descartado, más complejidad de la necesaria.

**D3: Scope del enriquecimiento del dominio de Order**
- Decisión: Agregar `Order.Create()`, `Order.AddItem()`, `Order.Checkout()` con setters privados en State/TotalAmount
- Items existente en EF Core: ICollection debe seguir siendo `List<OrderItem>` para EF navigation, pero el setter de la propiedad se hace `private set`.
- No se cambia el mapeo de EF Core (sin migraciones necesarias).

**D4: Manejo de excepciones en handlers**
- Decisión: Eliminar `catch (Exception)` genérico; manejar solo excepciones de negocio conocidas si las hay.
- Rationale: Los middlewares de ASP.NET Core (ProblemDetails, ExceptionHandler) deben manejar excepciones de sistema devolviendo 500.
- Implicación: Los controladores ya tienen manejo de `response.Success == false` para casos de negocio; las excepciones de sistema subirán como 500.

## Phase 1: Design & Contracts

### Data Model (cambios de comportamiento, no de schema)

#### Order — Enriquecido

```csharp
// ANTES: data bag
public class Order {
    public string State { get; set; } = "draft";
    public decimal TotalAmount { get; set; }
    public ICollection<OrderItem> Items { get; set; }
}

// DESPUÉS: rich domain model
public class Order {
    public const string StateDraft = "draft";
    public const string StatePending = "pending";
    public const string StatePaid = "paid";
    public const string StateFulfilled = "fulfilled";
    public const string StateCancelled = "cancelled";

    public Guid Id { get; private set; }
    public string State { get; private set; } = StateDraft;
    public decimal TotalAmount { get; private set; }
    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    public static Order Create(string? userId, string? guestToken) { ... }
    public void AddItem(Guid seatId, decimal price) { ... }
    public void Checkout() { ... }   // draft -> pending
    public void MarkAsPaid() { ... } // pending -> paid
}
```

#### Reservation — Con factory method

```csharp
public class Reservation {
    public const string StatusActive = "active";
    public const string StatusExpired = "expired";
    public const string StatusConfirmed = "confirmed";

    public static Reservation Create(Guid seatId, string customerId, int ttlMinutes = 15) { ... }
}
```

#### Seat — Con método Reserve()

```csharp
public class Seat {
    public void Reserve() {
        if (Reserved) throw new InvalidOperationException("Seat is already reserved");
        Reserved = true;
    }
    public void Release() { Reserved = false; }
}
```

### Contratos externos (sin cambios)

Los contratos HTTP REST y los eventos Kafka no cambian. Esta feature es refactoring interno puro.

### Nuevas interfaces de puerto (Inventory.Application.Ports)

```csharp
public interface ISeatRepository {
    Task<Seat?> GetByIdAsync(Guid seatId, CancellationToken ct);
    Task UpdateAsync(Seat seat, CancellationToken ct);
}

public interface IReservationRepository {
    Task<Reservation> CreateAsync(Reservation reservation, CancellationToken ct);
}
```
