# Análisis de Arquitectura - Ticketing Project Week0

## 1. Estructura del Proyecto

```
ticketing_project_week0/
├── crud_service/           # Servicio de CRUD (API REST)
│   └── src/
│       ├── CrudService.Api/
│       ├── CrudService.Application/
│       ├── CrudService.Domain/
│       └── CrudService.Infrastructure/
├── producer/              # Servicio de producción de eventos
├── paymentService/       # Worker de procesamiento de pagos
├── ReservationService/   # Worker de reservas
├── frontend/             # Next.js frontend
├── scripts/              # Scripts de infraestructura
└── compose.yml           # Docker Compose
```

---

## 2. Patrones de Diseño Implementados

### 2.1 Repository Pattern ✅

**Implementado en:**
- `crud_service/src/CrudService.Infrastructure/Persistence/Repositories.cs`
- `ReservationService/src/ReservationService.Infrastructure/Persistence/TicketRepository.cs`
- `paymentService/src/PaymentService.Infrastructure/Persistence/TicketRepository.cs`

**Descripción:** Abstrae la capa de datos detrás de interfaces (`ITicketRepository`, `IEventRepository`, etc.), permitiendo cambiar la implementación sin afectar la capa de aplicación.

```csharp
// Interfaz (Puerto outbound)
public interface ITicketRepository
{
    Task<Ticket?> GetByIdAsync(long id);
    Task<Ticket> AddAsync(Ticket ticket);
    // ...
}

// Implementación
public class TicketRepository : ITicketRepository
{
    private readonly TicketingDbContext _context;
    // ...
}
```

---

### 2.2 Ports & Adapters (Hexagonal) ✅

**Estructura de capas:**
```
┌─────────────────────────────────────────┐
│           API Layer (Controllers)       │  ← Adaptador primario (Driving)
├─────────────────────────────────────────┤
│         Application Layer               │  ← Casos de uso / UseCases
│    (UseCases: Commands & Queries)       │
├─────────────────────────────────────────┤
│     Ports (Inbound & Outbound)          │  ← Interfaces
├─────────────────────────────────────────┤
│         Infrastructure Layer             │  ← Adaptador secundario (Driven)
│   (Repositories, Message Publishers)     │
├─────────────────────────────────────────┤
│              Domain Layer                │  ← Entidades, Enums, Value Objects
└─────────────────────────────────────────┘
```

**Directorios:**
- `Ports/Inbound/` - Interfaces de Use Cases
- `Ports/Outbound/` - Interfaces de Repositorios y Servicios externos

---

### 2.3 Dependency Injection ✅

**Implementación:** .NET Core DI nativa con inversión de control.

```csharp
// Program.cs
builder.Services.AddScoped<ITicketRepository, TicketRepository>();
builder.Services.AddScoped<ITicketCommands, TicketCommands>();
```

---

### 2.4 Event-Driven Architecture ✅

**Productores (Publishers):**
- `Producer.Api` - Publica eventos a RabbitMQ
- `RabbitMQTicketPublisher` - Implementa `ITicketEventPublisher`
- `RabbitMQPaymentPublisher` - Implementa `IPaymentEventPublisher`

**Consumidores (Consumers):**
- `TicketReservationConsumer` - Escucha `q.ticket.reserved`
- `TicketPaymentConsumer` - Escucha `q.ticket.payments.approved` y `q.ticket.payments.rejected`

**Eventos:**
- `TicketReservedEvent`
- `PaymentApprovedEvent`
- `PaymentRejectedEvent`

---

### 2.5 Optimistic Locking ✅

**Implementación:** Campo `Version` en entidades + check en SQL.

```csharp
// TicketRepository.cs (ReservationService)
var sql = @"
    UPDATE tickets 
    SET status = 'reserved',
        version = version + 1
    WHERE id = {4} 
      AND version = {5} 
      AND status = 'available'";
```

Previene race conditions en reservas concurrentes.

---

### 2.6 CQRS (Partial) ⚠️

**Separación explícita de Commands y Queries:**
- `ITicketCommands` / `TicketCommands`
- `ITicketQueries` / `TicketQueries`
- `IEventCommands` / `EventCommands`
- `IEventQueries` / `EventQueries`

**Nota:** No usa un mediator ni un framework CQRS dedicado, pero la separación lógica existe.

---

### 2.7 Background Service ✅

**Implementación:** .NET `BackgroundService` para consumidores RabbitMQ.

```csharp
public class TicketReservationConsumer : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Escucha cola RabbitMQ
    }
}
```

---

## 3. Principios SOLID

### 3.1 Single Responsibility Principle (SRP) ✅

| Clase | Responsabilidad |
|-------|-----------------|
| `TicketCommands` | Solo comandos de tickets |
| `TicketQueries` | Solo consultas de tickets |
| `TicketRepository` | Solo acceso a datos de tickets |

**Cumple:** Cada clase tiene una única razón para cambiar.

---

### 3.2 Open/Closed Principle (OCP) ⚠️

**Cumple parcialmente:**
- Los UseCases son extensibles mediante herencia
- Los puertos (interfaces) permiten nuevas implementaciones

**Problema:**
- Los comandos tienen lógica condicional que podría extraerse a estrategias
- Los enums de estado podrían beneficiarse de State Pattern

---

### 3.3 Liskov Substitution Principle (LSP) ✅

**Cumple:** Todas las implementaciones de repositorios cumplen el contrato de sus interfaces. No hay comportamiento inesperado.

---

### 3.4 Interface Segregation Principle (ISP) ✅

**Interfaces pequeñas y específicas:**
- `IReserveTicketUseCase` - Una responsabilidad
- `ITicketRepository` - Solo operaciones de tickets
- `IPaymentEventPublisher` - Solo publicación de pagos

**No hay interfaces monolíticas.**

---

### 3.5 Dependency Inversion Principle (DIP) ✅

**Cumple:**
- Módulos de alto nivel no dependen de módulos de bajo nivel
- Ambas dependen de abstracciones
- Las dependencias van hacia dentro (Domain es el centro)

```csharp
// UseCase depende de interfaz, no de implementación
public class ReserveTicketUseCase : IReserveTicketUseCase
{
    private readonly ITicketRepository _ticketRepository; // Abstracción
}
```

---

## 4. Arquitectura Hexagonal - Cumplimiento

### 4.1 Estructura de Capas ✅

| Capa | Proyecto | Contiene |
|------|----------|----------|
| **Domain** | `CrudService.Domain` | Entities, Enums |
| **Application** | `CrudService.Application` | UseCases, Ports, DTOs |
| **Infrastructure** | `CrudService.Infrastructure` | Repositories, Messaging, DbContext |
| **API** | `CrudService.Api` | Controllers |

### 4.2 Dirección de Dependencias ✅

```
Domain (ninguna dependencia)
    ↑
Application (depende de Domain)
    ↑
Infrastructure (depende de Application y Domain)
    ↑
API (depende de todas)
```

**Verificación:**
- `Domain.csproj` → Sin dependencias
- `Application.csproj` → Solo `Domain`
- `Infrastructure.csproj` → `Domain` + `Application`
- `Api.csproj` → Todos los anteriores

### 4.3 Puertos (Ports) ✅

**Inbound Ports (Interfaces de Use Cases):**
- `IReserveTicketUseCase`
- `ITicketCommands`
- `ITicketQueries`
- `IProcessPaymentUseCase`

**Outbound Ports (Interfaces de Infraestructura):**
- `ITicketRepository`
- `IEventRepository`
- `ITicketEventPublisher`
- `IPaymentEventPublisher`

### 4.4 Adaptadores ✅

**Adaptadores Primarios (Driving):**
- `TicketsController`
- `PaymentsController`
- `EventsController`

**Adaptadores Secundarios (Driven):**
- `TicketRepository` (PostgreSQL)
- `RabbitMQTicketPublisher` (RabbitMQ)
- `RabbitMQTicketConsumer` (RabbitMQ)

---

## 5. Deuda Técnica Identificada

### 5.1 Código Duplicado ⚠️

**Problema:** Múltiples implementaciones de las mismas entidades y lógica en diferentes servicios.

| Código Duplicado | Servicios Afectados |
|------------------|---------------------|
| Entidades (`Ticket`, `Event`, `Payment`) | crud_service, paymentService, ReservationService |
| Enums (`TicketStatus`, `PaymentStatus`) | crud_service, paymentService, ReservationService |
| Lógica de transición de estados | paymentService, ReservationService |

**Impacto:** Mantenimiento difícil, inconsistencias potenciales.

---

### 5.2 Inconsistencia en Enums ⚠️

**Problema:** Los enums tienen nomenclatura mixta.

```csharp
// PascalCase en algunos servicios
TicketStatus.Available
TicketStatus.Reserved

// lowercase en otros servicios
TicketStatus.available
TicketStatus.reserved
```

**Archivos afectados:**
- `crud_service`: PascalCase
- `paymentService`: lowercase
- `ReservationService`: PascalCase

**Causa:** Mapping diferente a PostgreSQL enums (que usan lowercase).

---

### 5.3 Múltiples DbContext Duplicados ❌

**Problema:** 6 implementaciones diferentes de DbContext para las mismas entidades.

| DbContext | Ubicación |
|-----------|-----------|
| `TicketingDbContext` | crud_service/src/CrudService.Infrastructure/Persistence/ |
| `TicketingDbContext` | crud_service/Data/ |
| `TicketingDbContext` | ReservationService/src/ReservationService.Infrastructure/Persistence/ |
| `TicketingDbContext` | ReservationService/src/ReservationService.Worker/Data/ |
| `PaymentDbContext` | paymentService/src/PaymentService.Infrastructure/Persistence/ |
| `PaymentDbContext` | paymentService/MsPaymentService.Worker/Data/ |

**Solución sugerida:** Un único DbContext compartido o biblioteca de dominio con entidades.

---

### 5.4 Transacciones Manuales ⚠️

**Problema:** Algunas operaciones usan transacciones explícitas, otras no.

```csharp
// paymentService/MsPaymentService.Worker usa transacciones
using var transaction = await _dbContext.Database.BeginTransactionAsync();

// ReservationService no usa transacciones
var reserved = await _ticketRepository.TryReserveAsync(...);
```

**Riesgo:** Inconsistencia en manejo de atomicidad.

---

### 5.5 Falta de Unit of Work ❌

**Problema:** No hay abstracción de Unit of Work, cada repository maneja su propio DbContext.

```csharp
// Actualmente: múltiples SaveChanges
await _ticketRepository.UpdateAsync(ticket);
await _historyRepository.AddAsync(history); // Transacción separada
```

**Impacto:** No hay garantía de atomicidad en operaciones que tocan múltiples tablas.

---

### 5.6 Excepciones Genéricas ⚠️

**Problema:** Uso de `KeyNotFoundException`, `ArgumentException` sin mapeo a errores de dominio.

```csharp
// TicketCommands.cs
throw new KeyNotFoundException($"Ticket {id} no encontrado");
throw new ArgumentException($"Estado inválido: {newStatus}");
```

**Solución sugerida:** Excepciones de dominio específicas o Result pattern.

---

### 5.7 Falta de Logging Estructurado Consistente ⚠️

**Problema:** Logging inconsistente entre servicios.

```csharp
// Producer
_logger.LogInformation("Ticket reserved: {TicketId}", id);

// PaymentService
_logger.LogDebug("[Consumer] Payload: {Json}", json);
```

**Solución sugerida:** Estandarizar formato de logs.

---

### 5.8 Configuración Duplicada ⚠️

**Problema:** Múltiples archivos de configuración de RabbitMQ.

| Archivo | Ubicación |
|---------|-----------|
| `RabbitMQSettings.cs` | producer/src/Producer.Infrastructure/Messaging/ |
| `RabbitMQSettings.cs` | paymentService/src/PaymentService.Infrastructure/Messaging/ |
| `RabbitMQSettings.cs` | ReservationService/src/ReservationService.Infrastructure/Messaging/ |
| `RabbitMQSettings.cs` | ReservationService/src/ReservationService.Worker/Configurations/ |

---

### 5.9 Ausencia de API Contracts Compartidos ❌

**Problema:** Los DTOs de eventos están duplicados en Producer y Consumers.

| DTO | Ubicaciones |
|-----|-------------|
| `TicketReservedEvent` | producer, ReservationService |
| `PaymentApprovedEvent` | producer, paymentService |
| `PaymentRejectedEvent` | producer, paymentService |

**Solución sugerida:** Biblioteca compartida de contratos/mensajes.

---

### 5.10 Falta de Health Checks en Workers ❌

**Problema:** Los BackgroundServices no exponen endpoints de health check.

---

### 5.11 Consumer sin Dead Letter Queue ❌

**Problema:** Si un mensaje falla, se hace ACK de todas formas (actualmente).

```csharp
// TicketReservationConsumer.cs
catch (Exception ex)
{
    _logger.LogError(ex, "Error processing message");
    await _channel.BasicAckAsync(...); // Hace ACK incluso en error
}
```

**Riesgo:** Mensajes perdidos permanentemente en caso de error.

---

## 6. Resumen de Cumplimiento

| Aspecto | Nivel de Cumplimiento |
|---------|----------------------|
| **Patrones de Diseño** | 80% - Repository, Hexagonal, DI, Event-Driven implementados |
| **SOLID** | 95% - Muy buen cumplimiento general |
| **Arquitectura Hexagonal** | 85% - Estructura correcta, hay duplicación |
| **Clean Architecture** | 75% - Capas bien definidas, pero con deuda |

---

## 7. Recomendaciones Prioritarias

### Alta Prioridad
1. **Extraer biblioteca compartida** - DTOs de eventos, entidades base, enums
2. **Unificar DbContext** - Reducir de 6 a 1-2 implementaciones
3. **Implementar Unit of Work** - Para operaciones atómicas
4. **Dead Letter Queue** - Manejo de mensajes fallidos

### Media Prioridad
5. **Estandarizar enums** -统一ificar nomenclatura (PascalCase)
6. **API Contracts** - Extraer a biblioteca compartida
7. **Excepciones de dominio** - Crear excepciones específicas
8. **Configuración centralizada** - Settings compartidos

### Baja Prioridad
9. **State Pattern** - Para transiciones de estado de tickets
10. **Domain Events** - Para auditoría y notificaciones
11. **Logging estructurado** - Estandarizar formato

---

## 8. Conclusión

El proyecto demuestra una **buena comprensión de arquitectura hexagonal y patrones de diseño**. La estructura de capas es correcta, las dependencias fluyen en la dirección adecuada, y hay un uso apropiado de inversión de dependencias.

La **deuda técnica principal** proviene de:
1. Duplicación de código entre servicios (entidades, enums, configuración)
2. Múltiples DbContext para las mismas entidades
3. Falta de abstracciones como Unit of Work
4. Manejo inconsistente de errores y transacciones

La migración a MongoDB (como mencionaste) sería más sencilla si se aborda primero la deuda de duplicación de entidades.
