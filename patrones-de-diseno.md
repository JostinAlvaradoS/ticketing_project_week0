# Patrones de Diseño Implementados en Speckit Ticketing

## Resumen

Este documento describe los patrones de diseño implementados en el proyecto Speckit Ticketing, clasificados en las tres categorías principales:

- **Patrones Creacionales**: Encargados de la creación de objetos
- **Patrones Estructurales**: Definen la estructura de clases y objetos
- **Patrones de Comportamiento**: Definen el comportamiento de objetos y clases

---

# PATRONES CREACIONALES

## 1. Dependency Injection (Inyección de Dependencias)

### Descripción
Patrón fundamental en .NET que permite invertir el control de dependencias. El contenedor IoC gestiona la creación y生命周期 de objetos.

### Implementación

**Archivo**: `services/inventory/src/Inventory.Infrastructure/ServiceCollectionExtensions.cs`

```csharp
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // DbContext con scope por request
    services.AddDbContext<InventoryDbContext>(options =>
    {
        options.UseNpgsql(configuration.GetConnectionString("Default"), 
            npgsqlOptions => npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory", "bc_inventory"));
    });

    // Singleton para conexión Redis
    services.AddSingleton<IConnectionMultiplexer>(multiplexer);
    
    // Scoped para repositorios y handlers
    services.AddScoped<IDbInitializer, DbInitializer>();
    services.AddScoped<IRedisLock, RedisLock>();
    
    // Singleton para productores Kafka
    services.AddSingleton<IKafkaProducer, KafkaProducer>();

    return services;
}
```

### Registro en Program.cs

```csharp
// services/inventory/src/Inventory.Api/Program.cs
var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddMediatR(typeof(CreateReservationCommand).Assembly);
```

### Vida Útil de los Servicios

| Servicio | Lifetime | Razón |
|----------|----------|-------|
| `IConnectionMultiplexer` | Singleton | Una conexión Redis compartida |
| `IKafkaProducer` | Singleton | Productor reutilizable |
| `DbContext` | Scoped | Un contexto por request HTTP |
| `IRequestHandler<T>` | Scoped | Un handler por request |
| `IDbInitializer` | Scoped | Inicialización por request |

---

## 2. Factory (Fábrica)

### Descripción
Patrón que encapsula la creación de objetos, delegando la instanciación a métodos factory o interfaces especializadas.

### Implementación

**Archivo**: `services/inventory/src/Domain/Ports/IDbInitializer.cs`

```csharp
namespace Inventory.Domain.Ports;

public interface IDbInitializer
{
    Task InitializeAsync();
}
```

**Implementación**: `services/inventory/src/Inventory.Infrastructure/Persistence/DbInitializer.cs`

```csharp
public class DbInitializer : IDbInitializer
{
    private readonly InventoryDbContext _db;

    public DbInitializer(InventoryDbContext db) => _db = db;

    public async Task InitializeAsync()
    {
        // 1. Crear schema
        await CreateSchemaAsync();
        
        // 2. Aplicar migraciones
        await _db.Database.MigrateAsync();
    }

    private async Task CreateSchemaAsync()
    {
        var connection = _db.Database.GetDbConnection();
        await connection.OpenAsync();
        
        using var createCommand = connection.CreateCommand();
        createCommand.CommandText = @"
            CREATE SCHEMA IF NOT EXISTS bc_inventory;
            ALTER SCHEMA bc_inventory OWNER TO postgres;
        ";
        await createCommand.ExecuteNonQueryAsync();
    }
}
```

**Uso en Program.cs**:

```csharp
using (var scope = app.Services.CreateScope())
{
    var dbInitializer = scope.ServiceProvider.GetRequiredService<IDbInitializer>();
    await dbInitializer.InitializeAsync();
}
```

---

## 3. Builder (Constructor)

### Descripción
Patrón que permite construir objetos complejos paso a paso, especialmente útil para APIs de configuración.

### Implementación

**Archivo**: `services/inventory/src/Inventory.Infrastructure/Messaging/KafkaProducer.cs`

```csharp
public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string?, string> _producer;

    public KafkaProducer(IProducer<string?, string> producer)
    {
        _producer = producer ?? throw new ArgumentNullException(nameof(producer));
    }

    public async Task ProduceAsync(string topicName, string message, string? key = null)
    {
        var deliveryReport = await _producer.ProduceAsync(
            topicName,
            new Message<string?, string>
            {
                Key = key,
                Value = message
            },
            cts.Token);
    }
}
```

**Configuración del Builder en ServiceCollectionExtensions**:

```csharp
var kafkaConfig = new ProducerConfig
{
    BootstrapServers = kafkaBootstrapServers,
    AllowAutoCreateTopics = true,
    Acks = Acks.All
};

var producer = new ProducerBuilder<string?, string>(kafkaConfig).Build();
services.AddSingleton(producer);
services.AddSingleton<IKafkaProducer, KafkaProducer>();
```

**Consumer Builder**: `services/ordering/src/Infrastructure/Events/ReservationEventConsumer.cs`

```csharp
var config = new ConsumerConfig
{
    BootstrapServers = _kafkaOptions.BootstrapServers,
    GroupId = _kafkaOptions.ConsumerGroupId,
    AutoOffsetReset = AutoOffsetReset.Earliest,
    EnableAutoCommit = true
};

using var consumer = new ConsumerBuilder<string, string>(config)
    .SetErrorHandler((_, e) => _logger.LogError("Kafka error: {Error}", e.Reason))
    .Build();
```

---

## 4. Singleton

### Descripción
Patrón que garantiza que una clase tenga una única instancia y proporcione un punto de acceso global a ella.

### Implementación

**Redis Connection Multiplexer**:

```csharp
// services/inventory/src/Inventory.Infrastructure/ServiceCollectionExtensions.cs
var redisConn = configuration.GetConnectionString("Redis") ?? "localhost:6379";
var multiplexer = ConnectionMultiplexer.Connect(redisConn);
services.AddSingleton<IConnectionMultiplexer>(multiplexer);
```

**Kafka Producer**:

```csharp
var producer = new ProducerBuilder<string?, string>(kafkaConfig).Build();
services.AddSingleton(producer);
services.AddSingleton<IKafkaProducer, KafkaProducer>();
```

---

# PATRONES ESTRUCTURALES

## 1. Repository (Repositorio)

### Descripción
Patrón que abstrae la capa de acceso a datos, proporcionando una colección-like interface para las entidades del dominio.

### Implementación

**Interfaz del Puerto**: `services/ordering/src/Application/Ports/IOrderRepository.cs`

```csharp
namespace Ordering.Application.Ports;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default);
    Task<Order?> GetDraftOrderAsync(string? userId, string? guestToken, CancellationToken cancellationToken = default);
    Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default);
    Task<Order> UpdateAsync(Order order, CancellationToken cancellationToken = default);
    Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId, CancellationToken cancellationToken = default);
}
```

**Implementación Concreta**: `services/ordering/src/Infrastructure/Persistence/OrderRepository.cs`

```csharp
public class OrderRepository : IOrderRepository
{
    private readonly OrderingDbContext _context;

    public OrderRepository(OrderingDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<Order?> GetByIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
    }

    public async Task<Order> CreateAsync(Order order, CancellationToken cancellationToken = default)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync(cancellationToken);
        return await GetByIdAsync(order.Id, cancellationToken);
    }
}
```

**Otros Repositorios en el Proyecto**:

| Servicio | Interfaz | Implementación |
|----------|----------|---------------|
| Catalog | `ICatalogRepository` | `CatalogRepository` |
| Identity | `IUserRepository` | `UserRepository` |
| Inventory | `IReservationRepository` | `ReservationRepository` |
| Fulfillment | `ITicketRepository` | `TicketRepository` |

---

## 2. Adapter (Adaptador)

### Descripción
Patrón que permite que interfaces incompatibles trabajen juntas. Convierte la interfaz de una clase en otra interfaz que los clientes esperan.

### Implementación

**Puerto (Interfaz)**: `services/inventory/src/Domain/Ports/IRedisLock.cs`

```csharp
namespace Inventory.Domain.Ports;

public interface IRedisLock
{
    Task<string?> AcquireLockAsync(string key, TimeSpan ttl);
    Task<bool> ReleaseLockAsync(string key, string token);
}
```

**Adaptador Concreto**: `services/inventory/src/Inventory.Infrastructure/Locking/RedisLock.cs`

```csharp
namespace Inventory.Infrastructure.Locking;

public class RedisLock : IRedisLock
{
    private readonly IDatabase _db;

    public RedisLock(IConnectionMultiplexer multiplexer)
    {
        _db = multiplexer.GetDatabase();
    }

    public async Task<string?> AcquireLockAsync(string key, TimeSpan ttl)
    {
        var token = Guid.NewGuid().ToString("N");
        var acquired = await _db.StringSetAsync(key, token, ttl, when: When.NotExists);
        return acquired ? token : null;
    }

    public async Task<bool> ReleaseLockAsync(string key, string token)
    {
        var current = await _db.StringGetAsync(key);
        if (current == token)
        {
            return await _db.KeyDeleteAsync(key);
        }
        return false;
    }
}
```

**Puerto Kafka**: `services/inventory/src/Domain/Ports/IKafkaProducer.cs`

```csharp
namespace Inventory.Domain.Ports;

public interface IKafkaProducer
{
    Task ProduceAsync(string topicName, string message, string? key = null);
}
```

**Adaptador Kafka**: `services/inventory/src/Inventory.Infrastructure/Messaging/KafkaProducer.cs`

```csharp
public class KafkaProducer : IKafkaProducer
{
    private readonly IProducer<string?, string> _producer;

    public async Task ProduceAsync(string topicName, string message, string? key = null)
    {
        var deliveryReport = await _producer.ProduceAsync(
            topicName,
            new Message<string?, string> { Key = key, Value = message },
            cts.Token);
    }
}
```

---

## 3. Facade (Fachada)

### Descripción
Proporciona una interfaz simplificada a un subsistema complejo.

### Implementación

**Entity Framework DbContext**:

```csharp
// services/inventory/src/Inventory.Infrastructure/Persistence/InventoryDbContext.cs
public class InventoryDbContext : DbContext
{
    public DbSet<Reservation> Reservations => Set<Reservation>();
    public DbSet<Seat> Seats => Set<Seat>();

    // El DbContext es una fachada que oculta:
    // - Conexiones a BD
    // - Tracking de cambios
    // - Migraciones
    // - Unit of Work
}
```

**ServiceCollectionExtensions** como Fachada de Configuración:

```csharp
// services/inventory/src/Inventory.Infrastructure/ServiceCollectionExtensions.cs
public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
{
    // Configuración compleja simplificada en una llamada
    services.AddDbContext<InventoryDbContext>(...);
    services.AddSingleton<IConnectionMultiplexer>(...);
    services.AddScoped<IRedisLock, RedisLock>();
    services.AddSingleton<IKafkaProducer, KafkaProducer>();
    services.AddSingleton<IHostedService, ReservationExpiryWorker>(...);
    
    return services;
}
```

---

# PATRONES DE COMPORTAMIENTO

## 1. Mediator (Mediador)

### Descripción
Define un objeto que encapsula cómo interactúan un conjunto de objetos. Reduce el acoplamiento entre componentes.

### Implementación

**Instalación**: MediatR NuGet Package

```xml
<PackageReference Include="MediatR.Extensions.Microsoft.DependencyInjection" Version="11.1.0" />
```

**Registro**: `services/inventory/src/Inventory.Api/Program.cs`

```csharp
builder.Services.AddMediatR(typeof(CreateReservationCommand).Assembly);
```

**Command**: `services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommand.cs`

```csharp
namespace Inventory.Application.UseCases.CreateReservation;

public record CreateReservationCommand(Guid SeatId, string CustomerId) 
    : IRequest<CreateReservationResponse>;
```

**Handler**: `services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommandHandler.cs`

```csharp
public class CreateReservationCommandHandler : IRequestHandler<CreateReservationCommand, CreateReservationResponse>
{
    private readonly InventoryDbContext _context;
    private readonly IRedisLock _redisLock;
    private readonly IKafkaProducer _kafkaProducer;

    public CreateReservationCommandHandler(
        InventoryDbContext context,
        IRedisLock redisLock,
        IKafkaProducer kafkaProducer)
    {
        _context = context;
        _redisLock = redisLock;
        _kafkaProducer = kafkaProducer;
    }

    public async Task<CreateReservationResponse> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
    {
        // Lógica de negocio
    }
}
```

---

## 2. CQRS (Command Query Responsibility Segregation)

### Descripción
Separa las operaciones de lectura (queries) de las operaciones de escritura (commands).

### Implementación

**Commands** (Escritura):

| Servicio | Command | Handler |
|----------|---------|---------|
| Inventory | `CreateReservationCommand` | `CreateReservationCommandHandler` |
| Ordering | `AddToCartCommand` | `AddToCartHandler` |
| Ordering | `CheckoutOrderCommand` | `CheckoutOrderHandler` |
| Identity | `CreateUserCommand` | `CreateUserHandler` |
| Identity | `IssueTokenCommand` | `IssueTokenHandler` |

**Queries** (Lectura):

| Servicio | Query | Handler |
|----------|-------|---------|
| Catalog | `GetAllEventsQuery` | `GetAllEventsHandler` |
| Catalog | `GetEventQuery` | `GetEventHandler` |
| Catalog | `GetEventSeatmapQuery` | `GetEventSeatmapHandler` |

**Ejemplo de Query**:

```csharp
// services/catalog/src/Application/UseCases/GetAllEvents/GetAllEventsQuery.cs
namespace Catalog.Application.UseCases.GetAllEvents;

public record GetAllEventsQuery : IRequest<GetAllEventsResponse>;

// services/catalog/src/Application/UseCases/GetAllEvents/GetAllEventsHandler.cs
public class GetAllEventsHandler : IRequestHandler<GetAllEventsQuery, GetAllEventsResponse>
{
    private readonly ICatalogRepository _repository;

    public async Task<GetAllEventsResponse> Handle(GetAllEventsQuery request, CancellationToken cancellationToken)
    {
        var events = await _repository.GetAllEventsAsync(cancellationToken);
        return new GetAllEventsResponse(events.Select(e => ...));
    }
}
```

---

## 3. Observer / Pub-Sub (Publicador-Suscriptor)

### Descripción
Define una dependencia uno-a-muchos entre objetos, donde cuando un objeto cambia de estado, todos sus dependientes son notificados.

### Implementación

**Productor de Eventos (Publisher)**:

```csharp
// services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommandHandler.cs
private async Task PublishReservationCreatedEvent(Reservation reservation, Seat seat, CancellationToken cancellationToken)
{
    var @event = new ReservationCreatedEvent(
        EventId = Guid.NewGuid().ToString("D"),
        ReservationId = reservation.Id.ToString("D"),
        CustomerId = reservation.CustomerId,
        SeatId = reservation.SeatId.ToString("D"),
        Section = seat.Section,
        CreatedAt = reservation.CreatedAt.ToString("O"),
        ExpiresAt = reservation.ExpiresAt.ToString("O")
    );

    var json = JsonSerializer.Serialize(@event);
    await _kafkaProducer.ProduceAsync("reservation-created", json, reservation.SeatId.ToString("N"));
}
```

**Consumidor de Eventos (Subscriber)**:

```csharp
// services/ordering/src/Infrastructure/Events/ReservationEventConsumer.cs
public class ReservationEventConsumer : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig { ... };
        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        
        consumer.Subscribe(new[] { "reservation-created", "reservation-expired" });

        while (!stoppingToken.IsCancellationRequested)
        {
            var consumeResult = consumer.Consume(stoppingToken);
            await ProcessMessage(consumeResult.Topic, consumeResult.Message.Value, stoppingToken);
        }
    }

    private async Task ProcessMessage(string topic, string messageValue, CancellationToken cancellationToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var reservationStore = scope.ServiceProvider.GetRequiredService<ReservationStore>();

        switch (topic)
        {
            case "reservation-created":
                var createdEvent = JsonSerializer.Deserialize<ReservationCreatedEvent>(messageValue);
                reservationStore.AddReservation(createdEvent);
                break;

            case "reservation-expired":
                var expiredEvent = JsonSerializer.Deserialize<ReservationExpiredEvent>(messageValue);
                reservationStore.RemoveReservation(expiredEvent);
                break;
        }
    }
}
```

**Topics Kafka**:

| Topic | Productor | Consumidor |
|-------|-----------|------------|
| `reservation-created` | Inventory | Ordering |
| `reservation-expired` | Inventory | Ordering |
| `payment-succeeded` | Payment (simulado) | Fulfillment |
| `payment-failed` | Payment (simulado) | Ordering |
| `ticket-issued` | Fulfillment | Notification |

---

## 4. Strategy (Estrategia)

### Descripción
Define una familia de algoritmos, encapsula cada uno y los hace intercambiables.

### Implementación

**Interfaz de Estrategia**:

```csharp
// services/inventory/src/Domain/Ports/IRedisLock.cs
public interface IRedisLock
{
    Task<string?> AcquireLockAsync(string key, TimeSpan ttl);
    Task<bool> ReleaseLockAsync(string key, string token);
}
```

**Múltiples Implementaciones**:

| Estrategia | Implementación | Uso |
|-----------|---------------|-----|
| `IRedisLock` | `RedisLock` | Producción (Redis real) |
| `IRedisLock` | `MockRedisLock` | Testing |
| `ICatalogRepository` | `CatalogRepository` | PostgreSQL |
| `IOrderRepository` | `OrderRepository` | PostgreSQL |
| `IKafkaProducer` | `KafkaProducer` | Producción |
| `IKafkaProducer` | `MockKafkaProducer` | Testing |

**Selección en Testing**:

```csharp
// services/inventory/tests/Inventory.Integration.Tests/CreateReservationIntegrationTests.cs
var mockRedisLock = new MockRedisLock(acquireResult: "token-123");
var mockKafkaProducer = new MockKafkaProducer();

var handler = new CreateReservationCommandHandler(context, mockRedisLock, mockKafkaProducer);
```

---

## 5. Template Method (Método Plantilla)

### Descripción
Define el esqueleto de un algoritmo en una operación, postergando algunos pasos a las subclases.

### Implementación

**BackgroundService de .NET**:

```csharp
// services/inventory/src/Inventory.Infrastructure/Workers/ReservationExpiryWorker.cs
public class ReservationExpiryWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Template: ejecutar hasta que se detenga el servicio
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Worker error: {ex.Message}");
            }

            await Task.Delay(_pollInterval, stoppingToken);
        }
    }

    // Paso abstracto implementado por nosotros
    public async Task ProcessExpiredReservationsAsync(CancellationToken cancellationToken = default)
    {
        // Lógica específica del worker
    }
}
```

**Otro ejemplo**: `ReservationEventConsumer`

```csharp
public class ReservationEventConsumer : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Template: ciclo de vida del consumidor
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeMessagesAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                // Manejo de errores
            }
        }
    }
}
```

---

## 6. Unit of Work (Unidad de Trabajo)

### Descripción
Mantiene una lista de objetos afectados por una transacción y coordina la escritura en la base de datos.

### Implementación

**Entity Framework DbContext**:

```csharp
// services/inventory/src/Inventory.Application/UseCases/CreateReservation/CreateReservationCommandHandler.cs
public async Task<CreateReservationResponse> Handle(CreateReservationCommand request, CancellationToken cancellationToken)
{
    // Múltiples operaciones en una transacción
    var seat = await _context.Seats.FindAsync([request.SeatId], cancellationToken);
    
    seat.Reserved = true;
    
    _context.Reservations.Add(reservation);
    _context.Seats.Update(seat);
    
    // Unit of Work: SaveChanges confirma todas las operaciones
    await _context.SaveChangesAsync(cancellationToken);
}
```

---

# DIAGRAMA DE ARQUITECTURA

```
┌─────────────────────────────────────────────────────────────────────────┐
│                           FRONTEND (Next.js)                             │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │   Hooks     │  │   Context   │  │  Components │  │  API Layer  │    │
│  │ useSeatmap  │  │  AuthContext│  │ Seatmap     │  │ catalog.ts  │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
                                    │ HTTP
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    MICROSERVICIOS (.NET 8)                              │
│                                                                         │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐   │
│  │  Identity   │  │   Catalog    │  │  Inventory  │  │  Ordering   │   │
│  │   :5000     │  │   :5001     │  │   :5002     │  │   :5003     │   │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘   │
│        │               │               │                │              │
│        ▼               ▼               ▼                ▼              │
│  ┌─────────────────────────────────────────────────────────────┐      │
│  │                    CLEAN ARCHITECTURE                         │      │
│  │  ┌─────────────┐  ┌─────────────┐  ┌─────────────────────┐   │      │
│  │  │   Domain    │  │ Application  │  │    Infrastructure   │   │      │
│  │  │  Entities   │  │  UseCases   │  │  Repository Impl    │   │      │
│  │  │   Ports     │  │   Handlers  │  │  RedisLock Impl     │   │      │
│  │  │ (Interfaces)│  │    DTOs     │  │  KafkaProducer Impl │   │      │
│  │  └─────────────┘  └─────────────┘  └─────────────────────┘   │      │
│  └─────────────────────────────────────────────────────────────┘      │
└─────────────────────────────────────────────────────────────────────────┘
                                    │ Kafka Events
                                    ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    INFRAESTRUCTURA                                      │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐    │
│  │ PostgreSQL  │  │    Redis    │  │    Kafka    │  │    Docker   │    │
│  │  (Datos)    │  │  (Locks)    │  │  (Events)   │  │ (Container) │    │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘    │
└─────────────────────────────────────────────────────────────────────────┘
```

---

# RESUMEN DE PATRONES

| Categoría | Patrón | Archivo(s) Clave(s) |
|-----------|--------|---------------------|
| **Creacional** | Dependency Injection | `ServiceCollectionExtensions.cs` |
| **Creacional** | Factory | `IDbInitializer.cs`, `DbInitializer.cs` |
| **Creacional** | Builder | `KafkaProducer.cs`, `ReservationEventConsumer.cs` |
| **Creacional** | Singleton | `IConnectionMultiplexer`, `IKafkaProducer` |
| **Estructural** | Repository | `IOrderRepository.cs`, `OrderRepository.cs` |
| **Estructural** | Adapter | `IRedisLock.cs` → `RedisLock.cs` |
| **Estructural** | Facade | `InventoryDbContext`, `ServiceCollectionExtensions` |
| **Comportamiento** | Mediator | MediatR + Handlers |
| **Comportamiento** | CQRS | Commands/Queries separados |
| **Comportamiento** | Observer/Pub-Sub | Kafka Producers/Consumers |
| **Comportamiento** | Strategy | Múltiples implementaciones de puertos |
| **Comportamiento** | Template Method | `BackgroundService` |
| **Comportamiento** | Unit of Work | `DbContext.SaveChangesAsync()` |

---

# RECOMENDACIONES

1. **Consistencia en Patrones**: El proyecto ya utiliza los patrones de manera consistente. Mantener las convenciones establecidas.

2. **Testing**: Los patrones facilitan el testing:
   - Usar **Mocks** de los puertos (Repository, RedisLock, KafkaProducer)
   - MediatR permite testear Handlers sin HTTP

3. **Futuras Expansiones**:
   - Considerar **Specification Pattern** para queries complejas
   - **Decorator Pattern** para cross-cutting concerns (logging, caching)
   - **Saga Pattern** para transacciones distribuidas entre servicios
