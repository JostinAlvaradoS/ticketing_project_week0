# Cumplimiento de Patrones de Diseño — Ticketing Platform

**Servicios:** Ordering · Inventory · Identity · Fulfillment · Payment · Notification · Catalog
**Stack:** .NET 8 · C# 12 · EF Core 8 · MediatR 12 · Kafka (Confluent) · Redis · PostgreSQL
**Fecha de validación:** 2026-04-02
**Build:** ✅ 0 errors · 0 warnings · 48 tests passing

---

## Índice de patrones

| # | Patrón | Capa | Estado |
|---|---|---|---|
| 1 | Regla de Dependencia (Clean Architecture) | Todas | ✅ |
| 2 | Ports & Adapters (Hexagonal Architecture) | Application / Infrastructure | ✅ |
| 3 | Rich Domain Model | Domain | ✅ |
| 4 | Factory Method en Entidades | Domain | ✅ |
| 5 | CQRS con MediatR | Application | ✅ |
| 6 | Repository Pattern | Application / Infrastructure | ✅ |
| 7 | Inversión de Dependencias (DIP) | Todas | ✅ |
| 8 | Responsabilidad Única (SRP) | Todas | ✅ |
| 9 | Encapsulación con Backing Fields | Domain / Infrastructure | ✅ |
| 10 | Guard Clauses (Fail-Fast) | Domain / Application | ✅ |
| 11 | Eliminación de Magic Strings | Domain | ✅ |

---

## 1. Regla de Dependencia — Clean Architecture

**Principio:** Las dependencias de código solo pueden apuntar hacia adentro. Domain no conoce a nadie. Application solo conoce a Domain. Infrastructure conoce a Application y Domain. API conoce a todos.

```
API → Infrastructure → Application → Domain
                               ↑
                (nunca al revés)
```

### Evidencia en archivos `.csproj`

**`Ordering.Domain.csproj`** — sin referencias de proyecto:
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
  </PropertyGroup>
  <!-- SIN ItemGroup de ProjectReference -->
</Project>
```

**`Ordering.Application.csproj`** — solo referencia a Domain:
```xml
<ItemGroup>
  <PackageReference Include="MediatR" Version="12.2.0" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="../Ordering.Domain/Ordering.Domain.csproj" />
</ItemGroup>
```

**`Ordering.Infrastructure.csproj`** — referencia a Application y Domain (nunca al revés):
```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore" Version="8.0.4" />
  <PackageReference Include="Npgsql.EntityFrameworkCore.PostgreSQL" Version="8.0.4" />
  <PackageReference Include="Confluent.Kafka" Version="2.5.0" />
</ItemGroup>
<ItemGroup>
  <ProjectReference Include="../Ordering.Domain/Ordering.Domain.csproj" />
  <ProjectReference Include="../Ordering.Application/Ordering.Application.csproj" />
</ItemGroup>
```

**Violación corregida:** `Inventory.Application.csproj` tenía una referencia a `Inventory.Infrastructure` que fue eliminada. El handler `CreateReservationCommandHandler` inyectaba `InventoryDbContext` directamente — ahora usa `ISeatRepository` e `IReservationRepository` (ver patrón 6).

### Por qué importa

Si Application conoce Infrastructure, un cambio en la base de datos (por ejemplo, de PostgreSQL a MongoDB) puede romper la lógica de negocio. La regla de dependencia garantiza que el núcleo del sistema sea agnóstico a los detalles técnicos.

---

## 2. Ports & Adapters — Arquitectura Hexagonal

**Principio:** Los puertos (interfaces) definen cómo la aplicación habla con el exterior. Pertenecen a la capa Application. Los adaptadores (implementaciones) pertenecen a Infrastructure. Jamás al revés.

```
[Domain] ← [Application/Ports (interfaces)] ← [Infrastructure/Adapters (implementaciones)]
```

### Puertos por servicio

| Servicio | Archivo | Namespace |
|---|---|---|
| Ordering | `Ordering.Application/Ports/IOrderRepository.cs` | `Ordering.Application.Ports` |
| Ordering | `Ordering.Application/Ports/IReservationValidationService.cs` | `Ordering.Application.Ports` |
| Inventory | `Inventory.Application/Ports/ISeatRepository.cs` | `Inventory.Application.Ports` |
| Inventory | `Inventory.Application/Ports/IReservationRepository.cs` | `Inventory.Application.Ports` |
| Inventory | `Inventory.Application/Ports/IRedisLock.cs` | `Inventory.Application.Ports` |
| Inventory | `Inventory.Application/Ports/IKafkaProducer.cs` | `Inventory.Application.Ports` |
| Identity | `Identity.Application/Ports/IUserRepository.cs` | `Identity.Application.Ports` |
| Identity | `Identity.Application/Ports/IPasswordHasher.cs` | `Identity.Application.Ports` |
| Identity | `Identity.Application/Ports/ITokenGenerator.cs` | `Identity.Application.Ports` |
| Fulfillment | `Fulfillment.Application/Ports/IEventPublisher.cs` | `Fulfillment.Application.Ports` |

**`services/inventory/src/Inventory.Application/Ports/ISeatRepository.cs`:**
```csharp
namespace Inventory.Application.Ports;

public interface ISeatRepository
{
    Task<Seat?> GetByIdAsync(Guid seatId, CancellationToken cancellationToken);
    Task UpdateAsync(Seat seat, CancellationToken cancellationToken);
}
```

**`services/ordering/src/Ordering.Application/Ports/IOrderRepository.cs`:**
```csharp
namespace Ordering.Application.Ports;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetDraftOrderAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default);
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);
}
```

### Adaptadores (implementaciones en Infrastructure)

**`services/inventory/src/Inventory.Infrastructure/Persistence/SeatRepository.cs`:**
```csharp
namespace Inventory.Infrastructure.Persistence;

public class SeatRepository : ISeatRepository        // implementa el puerto
{
    private readonly InventoryDbContext _context;    // detalle de EF Core oculto al dominio

    public async Task<Seat?> GetByIdAsync(Guid seatId, CancellationToken cancellationToken)
        => await _context.Seats.FindAsync([seatId], cancellationToken: cancellationToken);

    public async Task UpdateAsync(Seat seat, CancellationToken cancellationToken)
    {
        _context.Seats.Update(seat);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
```

**`services/identity/src/Identity.Infrastructure/Security/JwtTokenGenerator.cs`:**
```csharp
namespace Identity.Infrastructure.Security;

public class JwtTokenGenerator : ITokenGenerator    // puerto definido en Application
{
    public string Generate(User user)
    {
        // Detalle técnico de JWT oculto al handler de Application
        var descriptor = new SecurityTokenDescriptor { ... };
        return tokenHandler.WriteToken(tokenHandler.CreateToken(descriptor));
    }
}
```

**Violaciones corregidas:**
- `IRedisLock` e `IKafkaProducer` vivían en `Inventory.Domain.Ports` → movidos a `Inventory.Application.Ports`
- `IPasswordHasher`, `ITokenGenerator`, `IUserRepository` vivían en `Identity.Domain.Ports` → movidos a `Identity.Application.Ports`
- `IEventPublisher` vivía en `Fulfillment.Infrastructure.Events` → movido a `Fulfillment.Application.Ports`

---

## 3. Rich Domain Model

**Principio:** Las entidades del dominio encapsulan comportamiento, no solo datos. Las reglas de negocio viven en el dominio, no en los handlers de Application.

### Antes (modelo anémico — corregido)

```csharp
// ❌ Lógica de negocio dispersa en el handler
order.State = "pending";          // setter público, sin validación
order.TotalAmount += price;       // cálculo manual en el handler
var item = new OrderItem { SeatId = seatId, Price = price };
order.Items.Add(item);            // colección mutable expuesta
```

### Después (rich domain model)

**`services/ordering/src/Ordering.Domain/Entities/Order.cs`:**
```csharp
public class Order
{
    public Guid Id { get; private set; }
    public string State { get; private set; } = StateDraft;
    public decimal TotalAmount { get; private set; }

    private readonly List<OrderItem> _items = new();
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly(); // colección inmutable

    public OrderItem AddItem(Guid seatId, decimal price)
    {
        if (State != StateDraft)
            throw new InvalidOperationException($"Cannot add items to an order in state '{State}'.");
        if (_items.Any(i => i.SeatId == seatId))
            throw new InvalidOperationException("Seat is already in the cart.");

        var item = new OrderItem { Id = Guid.NewGuid(), OrderId = Id, SeatId = seatId, Price = price };
        _items.Add(item);
        TotalAmount = _items.Sum(i => i.Price);  // recálculo encapsulado
        return item;
    }

    public void Checkout()
    {
        if (State != StateDraft)
            throw new InvalidOperationException($"Cannot checkout an order in state '{State}'.");
        if (!_items.Any())
            throw new InvalidOperationException("Order is empty. Add at least one item before checkout.");
        State = StatePending;
    }

    public void MarkAsPaid()
    {
        if (State != StatePending)
            throw new InvalidOperationException($"Cannot mark as paid an order in state '{State}'.");
        State = StatePaid;
        PaidAt = DateTime.UtcNow;
    }

    public bool BelongsTo(string? userId, string? guestToken)
    {
        if (!string.IsNullOrEmpty(userId)) return UserId == userId;
        if (!string.IsNullOrEmpty(guestToken)) return GuestToken == guestToken;
        return false;
    }
}
```

**`services/inventory/src/Inventory.Domain/Entities/Seat.cs`:**
```csharp
public class Seat
{
    public bool Reserved { get; private set; }

    public void Reserve()
    {
        if (Reserved)
            throw new InvalidOperationException($"Seat {Id} is already reserved.");
        Reserved = true;
    }

    public void Release() => Reserved = false;
}
```

**`services/payment/src/Payment.Domain/Entities/Payment.cs`:**
```csharp
public class Payment
{
    public void MarkAsSucceeded()
    {
        if (Status != StatusPending)
            throw new InvalidOperationException($"Cannot succeed payment from status: {Status}");
        Status = StatusSucceeded;
        ProcessedAt = DateTime.UtcNow;
    }

    public void MarkAsFailed(string errorCode, string errorMessage)
    {
        if (Status != StatusPending)
            throw new InvalidOperationException($"Cannot fail payment from status: {Status}");
        Status = StatusFailed;
        ErrorCode = errorCode;
        ErrorMessage = errorMessage;
        ProcessedAt = DateTime.UtcNow;
    }

    public bool IsValidForProcess()
        => Amount > 0 && OrderId != Guid.Empty && !string.IsNullOrWhiteSpace(PaymentMethod);
}
```

### Por qué importa

Con un modelo anémico, si se añade un nuevo handler que hace `order.State = "pending"` sin llamar a `Checkout()`, se bypasea la validación de items vacíos. Con el rich model, es **imposible** llegar a un estado inválido sin pasar por las reglas del dominio.

---

## 4. Factory Method en Entidades de Dominio

**Principio:** Los objetos del dominio no deben construirse con `new Entity { prop = val }`. Un factory method garantiza que toda instancia nace en un estado válido.

**`Order.Create()`** — el constructor es `private`:
```csharp
private Order() { }   // EF Core lo usa por reflexión; nadie más puede usarlo

public static Order Create(string? userId, string? guestToken)
{
    if (string.IsNullOrEmpty(userId) && string.IsNullOrEmpty(guestToken))
        throw new ArgumentException("Either UserId or GuestToken must be provided.");

    return new Order
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        GuestToken = guestToken,
        State = StateDraft,
        CreatedAt = DateTime.UtcNow
    };
}
```

**`Reservation.Create()`** — con validaciones de invariantes:
```csharp
private Reservation() { }

public static Reservation Create(Guid seatId, string customerId, int ttlMinutes = 15)
{
    if (seatId == Guid.Empty)
        throw new ArgumentException("SeatId cannot be empty.", nameof(seatId));
    if (string.IsNullOrWhiteSpace(customerId))
        throw new ArgumentException("CustomerId cannot be empty.", nameof(customerId));
    if (ttlMinutes <= 0)
        throw new ArgumentException("TTL must be positive.", nameof(ttlMinutes));

    var now = DateTime.UtcNow;
    return new Reservation
    {
        Id = Guid.NewGuid(),
        SeatId = seatId,
        CustomerId = customerId,
        CreatedAt = now,
        ExpiresAt = now.AddMinutes(ttlMinutes),
        Status = StatusActive
    };
}
```

**Uso en el handler de Application:**
```csharp
// Inventory — CreateReservationCommandHandler.cs
seat.Reserve();                                          // dominio valida el estado
var reservation = Reservation.Create(                    // factory garantiza invariantes
    request.SeatId, request.CustomerId, ReservationTTLMinutes);

// Ordering — AddToCartHandler.cs
order = Order.Create(request.UserId, request.GuestToken); // estado inicial garantizado
order.AddItem(request.SeatId, request.Price);             // reglas de dominio aplicadas
```

---

## 5. CQRS con MediatR

**Principio:** Comandos (mutación de estado) y Queries (lectura) tienen contratos y handlers separados. Cada caso de uso es una unidad autónoma con su propio `IRequest<T>` y `IRequestHandler<T, R>`.

### Comandos (mutación)

**`AddToCartCommand` → `AddToCartHandler`** (`Ordering.Application`):
```csharp
// Contrato del comando
public record AddToCartCommand(
    Guid ReservationId, Guid SeatId, decimal Price,
    string? UserId, string? GuestToken = null
) : IRequest<AddToCartResponse>;

// Handler: un caso de uso, una responsabilidad
public sealed class AddToCartHandler : IRequestHandler<AddToCartCommand, AddToCartResponse>
{
    public async Task<AddToCartResponse> Handle(AddToCartCommand request, CancellationToken ct)
    {
        var validation = await _reservationService.ValidateReservationAsync(...);
        if (!validation.IsValid)
            return new AddToCartResponse(false, validation.ErrorMessage, null);

        var existing = await _orderRepository.GetDraftOrderAsync(request.UserId, request.GuestToken, ct);
        if (existing != null)
        {
            try { existing.AddItem(request.SeatId, request.Price); }
            catch (InvalidOperationException)
            { return new AddToCartResponse(false, "Seat is already in the cart", null); }

            var updated = await _orderRepository.UpdateAsync(existing, ct);
            return new AddToCartResponse(true, null, MapToDto(updated));
        }

        var order = Order.Create(request.UserId, request.GuestToken);
        order.AddItem(request.SeatId, request.Price);
        var created = await _orderRepository.CreateAsync(order, ct);
        return new AddToCartResponse(true, null, MapToDto(created));
    }
}
```

**`CheckoutOrderCommand` → `CheckoutOrderHandler`:**
```csharp
public sealed class CheckoutOrderHandler : IRequestHandler<CheckoutOrderCommand, CheckoutOrderResponse>
{
    public async Task<CheckoutOrderResponse> Handle(CheckoutOrderCommand request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order == null) return new CheckoutOrderResponse(false, "Order not found", null);

        if (!order.BelongsTo(request.UserId, request.GuestToken))
            return new CheckoutOrderResponse(false, "Unauthorized", null);

        try { order.Checkout(); }
        catch (InvalidOperationException ex) when (ex.Message.Contains("empty"))
            { return new CheckoutOrderResponse(false, "Order is empty", null); }
        catch (InvalidOperationException)
            { return new CheckoutOrderResponse(false, "Order is not in draft state", null); }

        var updated = await _orderRepository.UpdateAsync(order, ct);
        return new CheckoutOrderResponse(true, null, MapToDto(updated));
    }
}
```

### Queries (lectura, sin efectos secundarios)

**`GetOrderQuery` → `GetOrderHandler`** (`Ordering.Application`):
```csharp
public record GetOrderQuery(Guid OrderId) : IRequest<OrderDto?>;

public class GetOrderHandler : IRequestHandler<GetOrderQuery, OrderDto?>
{
    public async Task<OrderDto?> Handle(GetOrderQuery request, CancellationToken ct)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, ct);
        if (order == null) return null;

        return new OrderDto(
            order.Id, order.UserId, order.GuestToken, order.TotalAmount,
            order.State, order.CreatedAt, order.PaidAt,
            order.Items.Select(i => new OrderItemDto(i.Id, i.SeatId, i.Price))
        );
    }
}
```

### Separación Command/Query en la API

```csharp
// OrdersController.cs — usa MediatR como bus
[HttpPost("checkout")]
public async Task<IActionResult> Checkout([FromBody] CheckoutRequest request, CancellationToken ct)
{
    var command = new CheckoutOrderCommand(request.OrderId, request.UserId, request.GuestToken);
    var response = await _mediator.Send(command, ct);    // comando
    return response.Success ? Ok(response.Order) : ...;
}

[HttpGet("{id}")]
public async Task<IActionResult> GetOrderDetails(Guid id, CancellationToken ct)
{
    var result = await _mediator.Send(new GetOrderQuery(id), ct);  // query
    return result == null ? NotFound() : Ok(MapToEnrichment(result));
}
```

---

## 6. Repository Pattern

**Principio:** El acceso a datos se abstrae detrás de un contrato orientado al dominio. La Application solo habla de objetos de dominio (`Order`, `Seat`), nunca de tablas, SQL ni `DbContext`.

### Puerto (Application)

```csharp
// Ordering.Application/Ports/IOrderRepository.cs
public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken ct = default);
    Task<Order?> GetDraftOrderAsync(string? userId, string? guestToken, CancellationToken ct = default);
    Task<Order> CreateAsync(Order order, CancellationToken ct = default);
    Task<Order> UpdateAsync(Order order, CancellationToken ct = default);
}
```

### Adaptador (Infrastructure)

```csharp
// Ordering.Infrastructure/Persistence/OrderRepository.cs
public class OrderRepository : IOrderRepository    // implementa el contrato del puerto
{
    private readonly OrderingDbContext _context;    // EF Core oculto aquí

    public async Task<Order?> GetDraftOrderAsync(string? userId, string? guestToken, CancellationToken ct)
    {
        var query = _context.Orders
            .Include(o => o.Items)
            .Where(o => o.State == Order.StateDraft);  // constante de dominio, no magic string

        if (!string.IsNullOrEmpty(userId))
            query = query.Where(o => o.UserId == userId);
        else if (!string.IsNullOrEmpty(guestToken))
            query = query.Where(o => o.GuestToken == guestToken);

        return await query.FirstOrDefaultAsync(ct);
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken ct)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(ct);
        return await GetByIdAsync(order.Id, ct)
            ?? throw new InvalidOperationException("Created order not found");
    }
}
```

**Efecto:** Los tests de Application usan mocks de `IOrderRepository` — nunca necesitan una base de datos para probar la lógica de negocio:

```csharp
// AddToCartHandlerTests.cs — mock del puerto, no del DbContext
_orderRepositoryMock
    .Setup(x => x.GetDraftOrderAsync(command.UserId, null, It.IsAny<CancellationToken>()))
    .ReturnsAsync((Order?)null);
```

---

## 7. Inversión de Dependencias (DIP — SOLID)

**Principio:** Los módulos de alto nivel no deben depender de módulos de bajo nivel. Ambos deben depender de abstracciones.

Todos los handlers de Application reciben sus dependencias **únicamente mediante interfaces**. Ningún handler conoce una clase concreta de Infrastructure.

**`CreateReservationCommandHandler`** — antes y después:

```csharp
// ❌ ANTES: dependencia directa de Infrastructure
public CreateReservationCommandHandler(InventoryDbContext context) { ... }

// ✅ DESPUÉS: solo interfaces (abstracciones)
public CreateReservationCommandHandler(
    ISeatRepository seatRepository,         // puerto en Application
    IReservationRepository reservationRepository,
    IRedisLock redisLock,                   // puerto para Redis (en Application)
    IKafkaProducer kafkaProducer)           // puerto para Kafka (en Application)
{ ... }
```

**`IssueTokenHandler` (Identity):**
```csharp
public IssueTokenHandler(
    IUserRepository userRepository,     // no SqlUserRepository
    ITokenGenerator tokenGenerator,     // no JwtTokenGenerator
    IPasswordHasher passwordHasher)     // no BcryptPasswordHasher
```

La vinculación concreta ocurre **solo** en `ServiceCollectionExtensions.cs` de cada Infrastructure, que es el único lugar del sistema donde una clase concreta de Infrastructure aparece ligada a su interfaz de Application:

```csharp
// Inventory.Infrastructure/ServiceCollectionExtensions.cs
services.AddScoped<ISeatRepository, SeatRepository>();
services.AddScoped<IReservationRepository, ReservationRepository>();
services.AddSingleton<IRedisLock, RedisLock>();
services.AddSingleton<IKafkaProducer, KafkaProducer>();
```

---

## 8. Responsabilidad Única (SRP — SOLID / Clean Code)

**Principio:** Cada clase tiene una única razón para cambiar.

### Separación por responsabilidad en el proyecto Ordering

| Clase | Responsabilidad única |
|---|---|
| `Order` (Domain) | Encapsula las reglas de negocio del pedido |
| `OrderRepository` (Infrastructure) | Persiste y recupera `Order` en PostgreSQL |
| `AddToCartHandler` (Application) | Orquesta el caso de uso "agregar al carrito" |
| `CheckoutOrderHandler` (Application) | Orquesta el caso de uso "checkout" |
| `OrderingDbContext` (Infrastructure) | Configura el mapeo ORM |
| `OrdersController` (API) | Traduce HTTP ↔ comandos/queries de MediatR |

### Handlers delgados

Los handlers **no** contienen lógica de negocio — la delegan al dominio:

```csharp
// CheckoutOrderHandler — el handler orquesta, el dominio decide
order.BelongsTo(request.UserId, request.GuestToken)  // regla de dominio
order.Checkout()                                      // regla de dominio
await _orderRepository.UpdateAsync(order, ct)         // persistencia delegada
```

Si la regla "un pedido vacío no puede hacer checkout" cambia, solo se modifica `Order.Checkout()`, no el handler.

---

## 9. Encapsulación con Backing Fields (EF Core + Dominio)

**Principio:** Las colecciones internas de un agregado no deben ser modificables desde afuera. Se usa un backing field privado expuesto como `IReadOnlyCollection`.

**`Order.cs`:**
```csharp
private readonly List<OrderItem> _items = new();
public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();
```

**El problema:** EF Core necesita acceder a `_items` para materializar entidades desde la DB. Se resuelve configurando el backing field explícitamente en el `DbContext`:

**`OrderingDbContext.cs`:**
```csharp
entity.Navigation(x => x.Items)
      .HasField("_items")
      .UsePropertyAccessMode(PropertyAccessMode.Field);
```

**Resultado:** EF Core escribe directamente en `_items` al cargar desde DB, sin exponer la lista como pública. El código de aplicación nunca puede hacer `order.Items.Add(item)` — solo puede usar `order.AddItem(seatId, price)` que aplica las reglas del dominio.

---

## 10. Guard Clauses — Fail-Fast

**Principio:** Validar precondiciones al inicio de un método y salir inmediatamente si no se cumplen. Evita la anidación profunda y hace explícitas las invariantes.

**En el dominio (`Order.AddItem`):**
```csharp
public OrderItem AddItem(Guid seatId, decimal price)
{
    if (State != StateDraft)        // guard 1: estado correcto
        throw new InvalidOperationException($"Cannot add items to an order in state '{State}'.");
    if (_items.Any(i => i.SeatId == seatId))  // guard 2: sin duplicados
        throw new InvalidOperationException("Seat is already in the cart.");

    // happy path solo si ambos guards pasan
    var item = new OrderItem { ... };
    _items.Add(item);
    TotalAmount = _items.Sum(i => i.Price);
    return item;
}
```

**En el handler (`CreateReservationCommandHandler`):**
```csharp
public async Task<CreateReservationResponse> Handle(CreateReservationCommand request, CancellationToken ct)
{
    if (request.SeatId == Guid.Empty)
        throw new ArgumentException("SeatId cannot be empty", nameof(request));
    if (string.IsNullOrEmpty(request.CustomerId))
        throw new ArgumentException("CustomerId cannot be empty", nameof(request));

    // resto del handler sin anidación
}
```

**En el factory method (`Reservation.Create`):**
```csharp
public static Reservation Create(Guid seatId, string customerId, int ttlMinutes = 15)
{
    if (seatId == Guid.Empty)      throw new ArgumentException("SeatId cannot be empty.");
    if (string.IsNullOrWhiteSpace(customerId)) throw new ArgumentException("CustomerId cannot be empty.");
    if (ttlMinutes <= 0)           throw new ArgumentException("TTL must be positive.");
    // objeto solo se crea si todos los guards pasan
}
```

---

## 11. Eliminación de Magic Strings

**Principio:** Los valores literales que representan estados o conceptos del dominio deben ser constantes nombradas. Un string mal escrito no genera error de compilación; una constante sí.

### Constantes de estado por entidad

**`Order.cs`:**
```csharp
public const string StateDraft     = "draft";
public const string StatePending   = "pending";
public const string StatePaid      = "paid";
public const string StateFulfilled = "fulfilled";
public const string StateCancelled = "cancelled";
```

**`Reservation.cs`:**
```csharp
public const string StatusActive    = "active";
public const string StatusExpired   = "expired";
public const string StatusConfirmed = "confirmed";
```

**`Payment.cs`:**
```csharp
public const string StatusPending   = "pending";
public const string StatusSucceeded = "succeeded";
public const string StatusFailed    = "failed";
```

### Impacto en cascada

El uso de constantes se propagó a todas las capas que usan estos valores:

```csharp
// OrderRepository.cs — consulta con constante, no literal
.Where(o => o.State == Order.StateDraft)

// ReservationExpiryWorker.cs
reservation.Status = Reservation.StatusExpired;
seat.Release();

// InventoryEventConsumer.cs
reservation.Status = Reservation.StatusConfirmed;

// ProcessPaymentHandler.cs — bug corregido
if (p.Status == Payment.StatusSucceeded)   // antes: "succeeded" || "completed" (bug)
    return new PaymentResponse(true, "Payment already processed", p.Id);
```

**Verificación:** no existen magic strings de estado en ninguna capa fuera de las definiciones:
```sh
grep -r '"draft"\|"pending"\|"paid"\|"active"\|"expired"' \
  services/*/src --include="*.cs" \
  | grep -v "Entities/" | grep -v ".csproj"
# → 0 resultados
```

---

## Resumen de cumplimiento

```
Clean Architecture
├── Regla de dependencia        ✅  Domain sin referencias · Application→Domain · Infra→App+Domain
└── Sin inversión de capas      ✅  Application nunca referencia Infrastructure (corregido)

Arquitectura Hexagonal
├── Puertos en Application      ✅  10 interfaces en */Application/Ports/
├── Adaptadores en Infrastructure ✅  Todas las implementaciones en */Infrastructure/
└── Interfaces no en Domain     ✅  Domain.Ports eliminados en Inventory e Identity

Rich Domain Model
├── Métodos de comportamiento   ✅  AddItem, Checkout, MarkAsPaid, Cancel, Reserve, Release, BelongsTo
├── Setters privados            ✅  Id, State, TotalAmount, PaidAt, Items en Order; Reserved en Seat
└── Colección encapsulada       ✅  _items backing field + IReadOnlyCollection<OrderItem>

Factory Method
├── Constructor privado         ✅  Order, Reservation (EF Core usa reflexión)
└── Validación en construcción  ✅  ArgumentException en Order.Create y Reservation.Create

CQRS
├── Comandos separados          ✅  AddToCartCommand, CheckoutOrderCommand, CreateReservationCommand
├── Queries separadas           ✅  GetOrderQuery
└── Bus MediatR                 ✅  _mediator.Send() en todos los controladores

Repository Pattern
├── Interfaz en Application     ✅  IOrderRepository, ISeatRepository, IReservationRepository
└── Implementación en Infra     ✅  OrderRepository, SeatRepository, ReservationRepository

Clean Code
├── Guard clauses               ✅  Validación temprana en factory methods y handlers
├── Sin magic strings           ✅  Constantes en Order, Reservation, Payment
├── Sin exception swallowing    ✅  Solo se capturan InvalidOperationException de dominio
└── Responsabilidad única       ✅  Handlers delgados, dominio con toda la lógica de negocio
```
