# 🔬 Escaneo de Código Generado por IA — Payment Service

**Servicio**: `paymentService/MsPaymentService.Worker`  
**Archivos analizados**: 30  
**Fecha**: 12 de febrero de 2026  

---

## 📊 Resumen de Hallazgos

| Categoría | Hallazgos | Severidad |
|-----------|-----------|-----------|
| Código redundante / Dead code | 4 | 🔴🟡🟡🟡 |
| Estructuras innecesariamente complejas | 2 | 🟡🟡 |
| Repetición de lógica | 3 | 🔴🟡🟡 |
| Manejo incorrecto de excepciones | 2 | 🔴🔴 |
| Recursos abiertos sin cerrar | 1 | 🔴 |
| Ineficiencia en conexiones RabbitMQ | 2 | 🔴🔴 |
| **Señales típicas de IA** | **3** | 🔴🔴🔴 |
| **TOTAL** | **17** | |

**Patrón de IA detectado**: El código muestra señales clásicas de generación asistida: sobre-abstracción prematura, clases huérfanas que nadie usa, lógica duplicada con mínimas variaciones, y "safety nets" redundantes que se contradicen entre sí.

---

## 🔴 HALLAZGO 1: `HandleResult` tiene código muerto — todos los mensajes se ACKean

**Archivo**: `Messaging/TicketPaymentConsumer.cs` líneas 96-114  
**Tipo**: Manejo incorrecto de excepciones + Dead code  
**Señal IA**: Generó todas las ramas posibles sin analizar el flujo de datos real

### Bloque original:
```csharp
private static void HandleResult(
    ValidationResult result,
    IModel channel,
    BasicDeliverEventArgs args)
{
    if (result.IsSuccess || result.IsAlreadyProcessed)
    {
        channel.BasicAck(args.DeliveryTag, false);
        return;
    }

    if (!string.IsNullOrEmpty(result.FailureReason))
    {
        channel.BasicAck(args.DeliveryTag, false);  // ← ⚠️ ACK en FALLOS también
        return;
    }

    // ❌ CÓDIGO MUERTO: este BasicNack NUNCA se ejecuta
    channel.BasicNack(
        deliveryTag: args.DeliveryTag,
        multiple: false,
        requeue: false);
}
```

### Análisis del flujo:
```
ValidationResult.Success()         → IsSuccess=true  → rama 1 → ACK ✅
ValidationResult.AlreadyProcessed() → IsAlreadyProcessed=true → rama 1 → ACK ✅  
ValidationResult.Failure("reason") → FailureReason="reason" → rama 2 → ACK ⚠️
                                                                        (debería ser NACK)
Llegar a BasicNack → requiere: IsSuccess=false, IsAlreadyProcessed=false, 
                                FailureReason=null/empty
                   → IMPOSIBLE con los factory methods existentes
```

**Impacto**: Los mensajes con fallo de negocio (`Failure("Ticket not found")`, `Failure("TTL exceeded")`) se ACKean silenciosamente. Nunca se envían a una Dead Letter Queue. Se pierden eventos de error sin posibilidad de reprocesamiento.

### Versión optimizada:
```csharp
private static void HandleResult(
    ValidationResult result,
    IModel channel,
    BasicDeliverEventArgs args)
{
    // Éxito o idempotencia → ACK: mensaje procesado correctamente
    if (result.IsSuccess || result.IsAlreadyProcessed)
    {
        channel.BasicAck(args.DeliveryTag, false);
        return;
    }

    // Fallo de negocio → NACK sin requeue: irá a DLQ para análisis
    channel.BasicNack(
        deliveryTag: args.DeliveryTag,
        multiple: false,
        requeue: false);
}
```

**Impacto técnico**: Mensajes fallidos ahora van a DLQ. Se pueden diagnosticar, monitorear y reprocesar. Elimina código muerto.

---

## 🔴 HALLAZGO 2: Canal único compartido entre dos consumers

**Archivo**: `Messaging/RabbitMQConnection.cs` líneas 33-40  
**Tipo**: Ineficiencia en conexiones RabbitMQ + recurso compartido problemático  
**Señal IA**: La IA generó un patrón singleton genérico sin considerar el contexto multi-consumer

### Bloque original:
```csharp
// RabbitMQConnection.cs
public IModel GetChannel()
{
    var connection = GetConnection();
    if (_channel == null || _channel.IsClosed)
    {
        _channel = connection.CreateModel();  // ← Siempre retorna EL MISMO canal
    }
    return _channel;
}

// Worker.cs — llama GetChannel() dos veces, obtiene el mismo canal
_consumer.Start(_rabbitSettings.ApprovedQueueName);   // canal X
_consumer.Start(_rabbitSettings.RejectedQueueName);   // canal X (el mismo)

// TicketPaymentConsumer.cs
public void Start(string queueName)
{
    var channel = _connection.GetChannel();         // ← Mismo canal siempre
    channel.BasicQos(0, _settings.PrefetchCount, false); // ← Se reescribe PrefetchCount
    // ...
}
```

### Problemas concretos:
1. **PrefetchCount=10 se aplica al canal, no por cola**. Con 2 colas en 1 canal, el límite real es 10 mensajes COMBINADOS, no 10 por cola
2. **Error en un consumer mata ambos**. Si el canal cierra por error de protocolo, ambos dejan de recibir
3. **BasicQos se invoca dos veces sobre el mismo canal**, la segunda llamada sobreescribe la primera (sin efecto neto, pero confuso)
4. **No es thread-safe**: `GetChannel()` puede causar race conditions si se llama concurrentemente

### Versión optimizada:
```csharp
public class RabbitMQConnection : IDisposable
{
    private readonly RabbitMQSettings _settings;
    private readonly ILogger<RabbitMQConnection> _logger;
    private IConnection? _connection;
    private readonly object _lock = new();
    private bool _disposed;

    public RabbitMQConnection(IOptions<RabbitMQSettings> settings, ILogger<RabbitMQConnection> logger)
    {
        _settings = settings.Value;
        _logger = logger;
    }

    public IConnection GetConnection()
    {
        if (_connection is { IsOpen: true })
            return _connection;

        lock (_lock)
        {
            if (_connection is { IsOpen: true })
                return _connection;

            _connection = CreateConnection();
            return _connection;
        }
    }

    /// <summary>
    /// Crea un canal independiente. Cada consumer debe tener su propio canal.
    /// </summary>
    public IModel CreateChannel()
    {
        var connection = GetConnection();
        return connection.CreateModel();
    }

    private IConnection CreateConnection()
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password,
            VirtualHost = _settings.VirtualHost,
            AutomaticRecoveryEnabled = true,
            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
            DispatchConsumersAsync = true
        };

        var conn = factory.CreateConnection();
        _logger.LogInformation("Connected to RabbitMQ at {Host}:{Port}", _settings.HostName, _settings.Port);
        return conn;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _connection?.Close();
        _connection?.Dispose();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}
```

Y en el consumer, cada cola obtiene su propio canal:
```csharp
public void Start(string queueName)
{
    var channel = _connection.CreateChannel();  // Canal independiente por cola
    channel.BasicQos(0, _settings.PrefetchCount, false);

    var consumer = new AsyncEventingBasicConsumer(channel);
    consumer.Received += async (sender, args) => await OnMessageReceivedAsync(channel, args);

    channel.BasicConsume(queue: queueName, autoAck: false, consumer: consumer);

    _logger.LogInformation("Consuming from {Queue} on dedicated channel", queueName);
}
```

**Impacto técnico**: Aislamiento de fallos entre consumers. PrefetchCount real de 10 por cola. Eliminación de race conditions en el canal.

---

## 🔴 HALLAZGO 3: `RabbitMQSettings` tiene doble fuente de configuración conflictiva

**Archivo**: `Configurations/RabbitMQSettings.cs`  
**Tipo**: Señal de IA — contradicción interna  
**Señal IA**: La IA intentó cubrir "todos los casos" sin entender el pipeline de configuración de .NET

### Bloque original:
```csharp
public class RabbitMQSettings
{
    // ❌ Lee de Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME")
    public string HostName { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_HOSTNAME") ?? "localhost";
    
    // ❌ Lee de Environment.GetEnvironmentVariable("RABBITMQ_PORT")  
    public int Port { get; set; } = int.TryParse(Environment.GetEnvironmentVariable("RABBITMQ_PORT"), out var port) ? port : 5672;
    
    // ❌ Lee de Environment.GetEnvironmentVariable("RABBITMQ_USERNAME")
    public string UserName { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_USERNAME") ?? "guest";
    
    // ❌ Lee de Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD")
    public string Password { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_PASSWORD") ?? "guest";
    
    // ❌ Lee de Environment.GetEnvironmentVariable("RABBITMQ_VHOST")
    public string VirtualHost { get; set; } = Environment.GetEnvironmentVariable("RABBITMQ_VHOST") ?? "/";
    
    // ...
}
```

Pero en `ConsumerExtensions.cs`:
```csharp
services.Configure<RabbitMQSettings>(configuration.GetSection("RabbitMQ"));
// ↑ Esto bindea RabbitMQ__HostName, RabbitMQ__Port, etc.
```

Y en `compose.yml`:
```yaml
- RabbitMQ__HostName=${RABBITMQ_HOST}  # Env var: RabbitMQ__HostName
```

### Conflicto de nombres:
| Fuente | Variable leída | Valor esperado |
|--------|---------------|----------------|
| Default del setter | `RABBITMQ_HOSTNAME` | No existe → "localhost" |
| IConfiguration bind | `RabbitMQ__HostName` | `${RABBITMQ_HOST}` → posiblemente vacío |
| compose.yml env | `RABBITMQ_HOST` | No definida en .env |

**Resultado**: El HostName termina siendo `""` (string vacío del env) o `"localhost"` (default), **nunca** `"rabbitmq"`.

### Versión optimizada:
```csharp
public class RabbitMQSettings
{
    // Valores default simples. IConfiguration los sobreescribe via Options pattern.
    // NO leer Environment.GetEnvironmentVariable — .NET ya lo hace via IConfiguration.
    public string HostName { get; set; } = "localhost";
    public int Port { get; set; } = 5672;
    public string UserName { get; set; } = "guest";
    public string Password { get; set; } = "guest";
    public string VirtualHost { get; set; } = "/";
    public string ApprovedQueueName { get; set; } = string.Empty;
    public string RejectedQueueName { get; set; } = string.Empty;
    public ushort PrefetchCount { get; set; } = 10;
}
```

Y en `compose.yml`, fijar el valor correcto:
```yaml
- RabbitMQ__HostName=rabbitmq  # Directo, sin variable intermedia
```

**Impacto técnico**: Elimina la ambigüedad de cuál fuente de configuración gana. Corrige el bug silencioso donde HostName podía ser vacío.

---

## 🔴 HALLAZGO 4: `TransitionToPaidAsync` y `TransitionToReleasedAsync` — Lógica duplicada copy-paste

**Archivo**: `Services/TicketStateService.cs`  
**Tipo**: Repetición de lógica  
**Señal IA**: Copy-paste con variaciones mínimas, patrón clásico de generación iterativa

### Bloques originales (código duplicado resaltado):

**TransitionToPaidAsync** (~50 líneas):
```csharp
public async Task<bool> TransitionToPaidAsync(long ticketId, string providerRef)
{
    using var transaction = await _dbContext.Database.BeginTransactionAsync();    // ← DUPLICADO
    try
    {
        var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);    // ← DUPLICADO
        if (ticket == null || ticket.Status != TicketStatus.reserved)            // ← SIMILAR
        {
            _logger.LogWarning(...);                                              // ← DUPLICADO
            return false;
        }

        var oldStatus = ticket.Status;                                           // ← DUPLICADO
        ticket.Status = TicketStatus.paid;                                       // ← VARÍA
        ticket.PaidAt = DateTime.UtcNow;

        var updated = await _ticketRepository.UpdateAsync(ticket);               // ← DUPLICADO

        if (!updated)                                                            // ← BLOQUE IDÉNTICO ↓
        {
            var current = await _ticketRepository.GetByIdAsync(ticketId);
            if (current != null && current.Status == TicketStatus.released)
            {
                _logger.LogInformation("Ticket {TicketId} already released...", ticketId);
                return true;
            }
            _logger.LogWarning("Failed to update ticket {TicketId}...", ticketId);
            return false;
        }                                                                        // ← BLOQUE IDÉNTICO ↑

        // payment update + history + commit                                     // ← DUPLICADO estructura
        await RecordHistoryAsync(ticketId, oldStatus, TicketStatus.paid, "..."); // ← DUPLICADO
        await transaction.CommitAsync();                                         // ← DUPLICADO
        return true;
    }
    catch (Exception ex)
    {
        await transaction.RollbackAsync();                                       // ← DUPLICADO
        _logger.LogError(ex, "Error transitioning ticket {TicketId}...");        // ← DUPLICADO
        throw;
    }
}
```

**TransitionToReleasedAsync** (~50 líneas): Estructura IDÉNTICA con diferencias mínimas.

**Porcentaje de duplicación**: ~75% del código es copiar-pegar.

### Versión optimizada:
```csharp
public class TicketStateService : ITicketStateService
{
    private readonly PaymentDbContext _dbContext;
    private readonly ITicketRepository _ticketRepository;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ITicketHistoryRepository _historyRepository;
    private readonly ILogger<TicketStateService> _logger;

    // Constructor sin cambios...

    public Task<bool> TransitionToPaidAsync(long ticketId, string providerRef)
    {
        return ExecuteTransitionAsync(ticketId, new TransitionContext
        {
            ExpectedStatus = TicketStatus.reserved,
            NewStatus = TicketStatus.paid,
            Reason = "Payment approved",
            ApplyChanges = (ticket) =>
            {
                ticket.PaidAt = DateTime.UtcNow;
            },
            UpdatePayment = async (payment) =>
            {
                if (payment == null) return;
                payment.Status = PaymentStatus.approved;
                payment.ProviderRef = providerRef;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }
        });
    }

    public Task<bool> TransitionToReleasedAsync(long ticketId, string reason)
    {
        return ExecuteTransitionAsync(ticketId, new TransitionContext
        {
            ExpectedStatus = null, // Acepta cualquier estado (released es terminal)
            NewStatus = TicketStatus.released,
            Reason = reason,
            ApplyChanges = (_) => { },
            UpdatePayment = async (payment) =>
            {
                if (payment is not { Status: PaymentStatus.pending }) return;
                payment.Status = reason.Contains("TTL") ? PaymentStatus.expired : PaymentStatus.failed;
                payment.UpdatedAt = DateTime.UtcNow;
                await _paymentRepository.UpdateAsync(payment);
            }
        });
    }

    private async Task<bool> ExecuteTransitionAsync(long ticketId, TransitionContext ctx)
    {
        await using var transaction = await _dbContext.Database.BeginTransactionAsync();

        try
        {
            var ticket = await _ticketRepository.GetByIdForUpdateAsync(ticketId);

            if (ticket == null)
            {
                _logger.LogWarning("Ticket {TicketId} not found for transition", ticketId);
                return false;
            }

            if (ctx.ExpectedStatus.HasValue && ticket.Status != ctx.ExpectedStatus.Value)
            {
                _logger.LogWarning(
                    "Invalid state for transition. TicketId: {TicketId}, Current: {Current}, Expected: {Expected}",
                    ticketId, ticket.Status, ctx.ExpectedStatus);
                return false;
            }

            var oldStatus = ticket.Status;
            ticket.Status = ctx.NewStatus;
            ctx.ApplyChanges(ticket);

            if (!await _ticketRepository.UpdateAsync(ticket))
            {
                return await HandleConcurrencyConflict(ticketId, ctx.NewStatus);
            }

            var payment = await _paymentRepository.GetByTicketIdAsync(ticketId);
            await ctx.UpdatePayment(payment);

            await RecordHistoryAsync(ticketId, oldStatus, ctx.NewStatus, ctx.Reason);
            await transaction.CommitAsync();

            _logger.LogInformation(
                "Ticket {TicketId}: {Old} → {New}. Reason: {Reason}",
                ticketId, oldStatus, ctx.NewStatus, ctx.Reason);

            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Failed transition for ticket {TicketId}", ticketId);
            throw;
        }
    }

    private async Task<bool> HandleConcurrencyConflict(long ticketId, TicketStatus targetStatus)
    {
        var current = await _ticketRepository.GetByIdAsync(ticketId);

        if (current?.Status == targetStatus)
        {
            _logger.LogInformation("Ticket {TicketId} already in {Status} (idempotent)", ticketId, targetStatus);
            return true;
        }

        _logger.LogWarning("Concurrent modification on ticket {TicketId}", ticketId);
        return false;
    }

    // RecordHistoryAsync sin cambios...

    private sealed record TransitionContext
    {
        public TicketStatus? ExpectedStatus { get; init; }
        public TicketStatus NewStatus { get; init; }
        public string Reason { get; init; } = string.Empty;
        public Action<Ticket> ApplyChanges { get; init; } = _ => { };
        public Func<Payment?, Task> UpdatePayment { get; init; } = _ => Task.CompletedTask;
    }
}
```

**Impacto técnico**: De ~100 líneas duplicadas a ~60 líneas con lógica centralizada. Un solo punto de mantenimiento para transacciones, concurrencia y logging.

---

## 🔴 HALLAZGO 5: Lectura doble del ticket sin necesidad (query desperdiciada)

**Archivos**: `Services/PaymentValidationService.cs` + `Services/TicketStateService.cs`  
**Tipo**: Ineficiencia + Race condition  
**Señal IA**: Separación de responsabilidades sobre-interpretada — la IA creó capas que desperdician queries

### Flujo actual (Payment Approved):
```
PaymentValidationService.ValidateAndProcessApprovedPaymentAsync()
│
├── 1️⃣ SELECT * FROM tickets WHERE id = X          ← SIN LOCK
│   (lee ticket, valida status == reserved)
│   (valida TTL)
│
└── TicketStateService.TransitionToPaidAsync()
    │
    └── 2️⃣ SELECT * FROM tickets WHERE id = X FOR UPDATE  ← CON LOCK
        (lee ticket DE NUEVO, re-valida status == reserved)
```

**Problemas**:
- **Query redundante**: El ticket se lee 2 veces. La primera lectura (sin lock) es **inútil** porque el estado puede cambiar entre la lectura 1 y la lectura 2.
- **Race condition**: Entre la query 1 (sin lock) y la query 2 (con lock), otro consumer puede haber cambiado el status. La validación en PaymentValidationService es una ilusión de seguridad.
- **Roundtrip extra**: +1 query por cada mensaje procesado.

### Versión optimizada:
Mover la validación completa DENTRO de la transacción con lock:

```csharp
public class PaymentValidationService : IPaymentValidationService
{
    private readonly ITicketStateService _stateService;
    private readonly IPaymentRepository _paymentRepository;
    private readonly ILogger<PaymentValidationService> _logger;

    // Constructor simplificado (eliminar ITicketRepository — ya no se necesita aquí)

    public async Task<ValidationResult> ValidateAndProcessApprovedPaymentAsync(PaymentApprovedEvent paymentEvent)
    {
        try
        {
            // Delegar directamente. La validación de estado ocurre dentro de la
            // transacción con FOR UPDATE (única fuente de verdad).
            var payment = await EnsurePaymentExistsAsync(paymentEvent);

            var success = await _stateService.TransitionToPaidAsync(
                paymentEvent.TicketId,
                paymentEvent.TransactionRef,
                paymentEvent.ApprovedAt);  // Pasar ApprovedAt para validar TTL dentro del lock

            return success
                ? ValidationResult.Success()
                : ValidationResult.Failure("Transition failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing approved payment for ticket {TicketId}", paymentEvent.TicketId);
            throw;
        }
    }

    private async Task<Payment> EnsurePaymentExistsAsync(PaymentApprovedEvent evt)
    {
        var payment = await _paymentRepository.GetByTicketIdAsync(evt.TicketId);
        return payment ?? await _paymentRepository.CreateAsync(new Payment
        {
            TicketId = evt.TicketId,
            Status = PaymentStatus.pending,
            AmountCents = evt.AmountCents,
            Currency = evt.Currency,
            ProviderRef = evt.TransactionRef
        });
    }
}
```

Y en `TicketStateService.TransitionToPaidAsync`:
```csharp
// Validar TTL DENTRO de la transacción con lock
if (ticket.ReservedAt == null || approvedAt > ticket.ReservedAt.Value.AddMinutes(ttlMinutes))
{
    // TTL expirado → transicionar a released en la misma transacción
    ticket.Status = TicketStatus.released;
    // ...
}
```

**Impacto técnico**: Elimina 1 query por mensaje (miles/hora bajo carga). Elimina race condition entre lectura sin lock y transacción con lock.

---

## 🔴 HALLAZGO 6: TTL hardcodeado a 5 minutos ignorando `PaymentSettings`

**Archivo**: `Services/PaymentValidationService.cs` línea 152  
**Tipo**: Redundancia — la configuración existe pero no se usa  
**Señal IA**: La IA generó `PaymentSettings` con `ReservationTtlMinutes=5` pero luego hardcodeó el valor

### Bloque original:
```csharp
// PaymentSettings.cs — configuración que EXISTE pero NO SE USA
public class PaymentSettings
{
    public int ReservationTtlMinutes { get; set; } = 5;  // ← configurable
    public int MaxRetryAttempts { get; set; } = 3;       // ← NUNCA REFERENCIADO
    public int RetryDelaySeconds { get; set; } = 5;      // ← NUNCA REFERENCIADO
}

// PaymentValidationService.cs — IGNORA PaymentSettings, hardcodea 5
public bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt)
{
    var expirationTime = reservedAt.AddMinutes(5);  // ❌ Magic number
    return paymentReceivedAt <= expirationTime;
}
```

### Versión optimizada:
```csharp
public class PaymentValidationService : IPaymentValidationService
{
    private readonly PaymentSettings _paymentSettings;
    // ... otros campos

    public PaymentValidationService(
        // ... otros parámetros
        IOptions<PaymentSettings> paymentSettings)
    {
        _paymentSettings = paymentSettings.Value;
    }

    public bool IsWithinTimeLimit(DateTime reservedAt, DateTime paymentReceivedAt)
    {
        var expirationTime = reservedAt.AddMinutes(_paymentSettings.ReservationTtlMinutes);
        return paymentReceivedAt <= expirationTime;
    }
}
```

Y eliminar de la interfaz pública — es un detalle de implementación:
```csharp
public interface IPaymentValidationService
{
    Task<ValidationResult> ValidateAndProcessApprovedPaymentAsync(PaymentApprovedEvent paymentEvent);
    Task<ValidationResult> ValidateAndProcessRejectedPaymentAsync(PaymentRejectedEvent paymentEvent);
    // ❌ ELIMINAR: bool IsWithinTimeLimit(...) — no debería ser público
}
```

**Impacto técnico**: TTL configurable sin recompilar. Elimina 2 propiedades huérfanas. Reduce superficie pública de la interfaz.

---

## 🟡 HALLAZGO 7: 3 clases completamente huérfanas (Dead Code)

**Tipo**: Código redundante generado por IA  
**Señal IA**: La IA generó clases "por si acaso" que nunca se conectaron

### Clase 1: `DatabaseConfiguration.cs`
```csharp
// ❌ NUNCA REFERENCIADA en todo el proyecto
public class DatabaseConfiguration
{
    public string ConnectionString { get; set; } = string.Empty;
    public bool EnableSensitiveDataLogging { get; set; } = false;
    public bool EnableDetailedErrors { get; set; } = false;
    public int CommandTimeout { get; set; } = 30;
}
```
La conexión se configura directamente en `DatabaseExtensions.cs` leyendo de `IConfiguration.GetConnectionString()`.

### Clase 2: `PaymentResponse.cs`
```csharp
// ❌ NUNCA USADA — este es un Worker, no tiene API HTTP
public class PaymentResponse
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public PaymentStatus Status { get; set; }
    // ...
}
```
Un Worker/Consumer no expone endpoints HTTP. No hay controladores. No hay ningún uso de este DTO.

### Clase 3: `TicketPaymentEvent.cs`
```csharp
// ❌ NUNCA DESERIALIZADA ni referenciada
public class TicketPaymentEvent
{
    public long TicketId { get; set; }
    public long EventId { get; set; }
    public string OrderId { get; set; } = default!;
    // ...
}
```
Los eventos reales son `PaymentApprovedEvent` y `PaymentRejectedEvent`. Este modelo fue generado pero nunca conectado.

### Versión optimizada:
```bash
# Eliminar los 3 archivos:
rm Configurations/DatabaseConfiguration.cs
rm Models/DTOs/PaymentResponse.cs
rm Models/Events/TicketPaymentEvent.cs
```

**Impacto técnico**: -3 archivos de mantenimiento muerto. Reduce confusión al onboardear nuevos devs. Reducción del assembly compilado.

---

## 🟡 HALLAZGO 8: Handlers duplicados con `JsonSerializerOptions` repetido

**Archivos**: `Handlers/PaymentApprovedEventHandler.cs` + `Handlers/PaymentRejectedEventHandler.cs`  
**Tipo**: Repetición de lógica  
**Señal IA**: La IA generó un handler copiando el anterior y cambiando el tipo

### Bloques originales (lado a lado):
```
PaymentApprovedEventHandler              PaymentRejectedEventHandler
─────────────────────────────            ─────────────────────────────
IPaymentValidationService ✓              IPaymentValidationService ✓     ← IGUAL
RabbitMQSettings ✓                       RabbitMQSettings ✓              ← IGUAL
static JsonOptions = new() {...}         static JsonOptions = new() {...} ← DUPLICADO
QueueName => ApprovedQueueName           QueueName => RejectedQueueName   ← VARÍA
Deserialize<PaymentApprovedEvent>        Deserialize<PaymentRejectedEvent>← VARÍA tipo
ValidateAndProcess*Approved*Async        ValidateAndProcess*Rejected*Async← VARÍA método
```

**90% del código es idéntico**. Solo difiere el tipo de evento y el método a llamar.

### Versión optimizada — handler base genérico:
```csharp
public abstract class PaymentEventHandlerBase<TEvent> : IPaymentEventHandler
    where TEvent : class
{
    private readonly IPaymentValidationService _validationService;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    protected PaymentEventHandlerBase(IPaymentValidationService validationService)
    {
        _validationService = validationService;
    }

    public abstract string QueueName { get; }

    protected abstract Task<ValidationResult> ProcessAsync(
        IPaymentValidationService service, TEvent evt);

    public async Task<ValidationResult> HandleAsync(string json, CancellationToken ct = default)
    {
        var evt = JsonSerializer.Deserialize<TEvent>(json, JsonOptions);
        if (evt == null)
            return ValidationResult.Failure($"Invalid JSON for {typeof(TEvent).Name}");

        return await ProcessAsync(_validationService, evt);
    }
}

public class PaymentApprovedEventHandler : PaymentEventHandlerBase<PaymentApprovedEvent>
{
    private readonly string _queueName;

    public PaymentApprovedEventHandler(
        IPaymentValidationService validationService,
        IOptions<RabbitMQSettings> settings) : base(validationService)
    {
        _queueName = settings.Value.ApprovedQueueName;
    }

    public override string QueueName => _queueName;

    protected override Task<ValidationResult> ProcessAsync(
        IPaymentValidationService service, PaymentApprovedEvent evt)
        => service.ValidateAndProcessApprovedPaymentAsync(evt);
}

public class PaymentRejectedEventHandler : PaymentEventHandlerBase<PaymentRejectedEvent>
{
    private readonly string _queueName;

    public PaymentRejectedEventHandler(
        IPaymentValidationService validationService,
        IOptions<RabbitMQSettings> settings) : base(validationService)
    {
        _queueName = settings.Value.RejectedQueueName;
    }

    public override string QueueName => _queueName;

    protected override Task<ValidationResult> ProcessAsync(
        IPaymentValidationService service, PaymentRejectedEvent evt)
        => service.ValidateAndProcessRejectedPaymentAsync(evt);
}
```

**Impacto técnico**: Elimina duplicación de JsonOptions (2 instancias → 1). Agregar un nuevo tipo de evento requiere solo una clase de 15 líneas en vez de 40. DRY.

---

## 🟡 HALLAZGO 9: Dispatcher hace matching invertido y frágil

**Archivo**: `Handlers/PaymentEventDispatcherImpl.cs`  
**Tipo**: Estructura innecesariamente compleja + Bug potencial  
**Señal IA**: La IA confundió routing key con queue name

### Bloque original:
```csharp
public async Task<ValidationResult?> DispatchAsync(string queueName, string json, CancellationToken ct = default)
{
    // El consumer pasa args.RoutingKey (ej: "ticket.payments.approved")
    // El handler tiene QueueName (ej: "q.ticket.payments.approved")
    
    var handler = _handlers.FirstOrDefault(h =>
        h.QueueName.EndsWith(queueName, StringComparison.Ordinal));
        //          ↑ "q.ticket.payments.approved".EndsWith("ticket.payments.approved")
        //          = TRUE (funciona por coincidencia)
}
```

**Problemas**:
1. `EndsWith` es un matching **accidental**. Si se agrega una cola `q.vip.ticket.payments.approved`, matchea también
2. El parámetro se llama `queueName` pero recibe una **routing key** — naming incorrecto
3. Búsqueda lineal O(n) en cada mensaje (menor importancia, pero innecesario)

### Versión optimizada:
```csharp
public class PaymentEventDispatcherImpl : IPaymentEventDispatcher
{
    private readonly Dictionary<string, IPaymentEventHandler> _handlerMap;

    public PaymentEventDispatcherImpl(IEnumerable<IPaymentEventHandler> handlers)
    {
        // Construir mapa indexado una sola vez al crear el dispatcher
        _handlerMap = handlers.ToDictionary(
            h => h.QueueName,
            h => h,
            StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ValidationResult?> DispatchAsync(
        string routingKey, string json, CancellationToken ct = default)
    {
        // Buscar por queue name exacto que corresponde al routing key
        // Si el consumer pasa el queue name directamente (corregido), es O(1)
        return _handlerMap.TryGetValue(routingKey, out var handler)
            ? await handler.HandleAsync(json, ct)
            : null;
    }
}
```

Y en `TicketPaymentConsumer`, pasar el queue name en vez del routing key:
```csharp
// Antes: dispatcher.DispatchAsync(args.RoutingKey, json)
// Después: asociar cada consumer con su queue name
consumer.Received += async (sender, args) => 
    await OnMessageReceivedAsync(channel, args, queueName);

private async Task OnMessageReceivedAsync(IModel channel, BasicDeliverEventArgs args, string queueName)
{
    // ...
    var result = await dispatcher.DispatchAsync(queueName, json);
    // ...
}
```

**Impacto técnico**: Matching exacto O(1) en vez de lineal O(n) con `EndsWith`. Elimina falsos positivos. Naming correcto.

---

## 🟡 HALLAZGO 10: Dockerfile con healthcheck HTTP para un Worker sin HTTP

**Archivo**: `Dockerfile`  
**Tipo**: Estructura innecesaria / Código incoherente  
**Señal IA**: La IA copió un Dockerfile de API y no ajustó para Worker

### Bloque original:
```dockerfile
# Instalar curl para healthcheck ← ¿Por qué un Worker necesita curl?
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Exponer puerto ← ¿Qué puerto? El Worker no tiene Kestrel
EXPOSE 8080

# Health check ← Siempre falla: no hay HTTP listener
HEALTHCHECK --interval=10s --timeout=5s --retries=5 \
  CMD curl -f http://localhost:8080/api/tickets/health || exit 1
```

**Problemas**:
1. `EXPOSE 8080` no hace nada — el Worker no abre ningún socket
2. `curl http://localhost:8080/...` **siempre falla** porque no hay web server
3. Se instala `curl` innecesariamente (20MB+ de paquetes)
4. Docker nunca reporta este container como healthy

### Versión optimizada:
```dockerfile
# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["MsPaymentService.Worker/MsPaymentService.Worker.csproj", "MsPaymentService.Worker/"]
RUN dotnet restore "MsPaymentService.Worker/MsPaymentService.Worker.csproj"
COPY MsPaymentService.Worker/ MsPaymentService.Worker/
RUN dotnet publish "MsPaymentService.Worker/MsPaymentService.Worker.csproj" \
    -c Release -o /app/publish --no-restore

# Stage 2: Runtime (imagen más ligera, sin curl)
FROM mcr.microsoft.com/dotnet/runtime:8.0
WORKDIR /app

# No root
RUN groupadd -r appuser && useradd -r -g appuser appuser
USER appuser

COPY --from=build /app/publish .

# Sin EXPOSE — no es un servicio HTTP
# Sin HEALTHCHECK HTTP — se valida vía docker-compose depends_on o process check

ENTRYPOINT ["dotnet", "MsPaymentService.Worker.dll"]
```

**Impacto técnico**: Imagen ~150MB más ligera (runtime vs aspnet + curl). Healthcheck que no falla perpetuamente. Corre como non-root.

---

## 🟡 HALLAZGO 11: `PaymentRepository.UpdateAsync` traga excepciones silenciosamente

**Archivo**: `Repositories/PaymentRepository.cs` líneas 40-51  
**Tipo**: Manejo incorrecto de excepciones

### Bloque original:
```csharp
public async Task<bool> UpdateAsync(Payment payment)
{
    try
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return true;
    }
    catch  // ← Catch TODO. ¿Constraint violation? ¿Timeout? ¿Conexión caída? TODO → false
    {
        return false;  // ← Se traga la excepción. Sin log. Sin contexto.
    }
}
```

### Versión optimizada:
```csharp
public async Task<bool> UpdateAsync(Payment payment)
{
    try
    {
        _context.Payments.Update(payment);
        await _context.SaveChangesAsync();
        return true;
    }
    catch (DbUpdateConcurrencyException)
    {
        // Conflicto de concurrencia esperado — retornar false para que el caller decida
        return false;
    }
    // Otras excepciones (timeout, connection lost) suben al caller
}
```

**Impacto técnico**: Excepciones de infraestructura (red, timeout) ya no se ocultan. Solo conflictos de concurrencia retornan false.

---

## 🟡 HALLAZGO 12: Múltiples `SaveChangesAsync()` dentro de una transacción

**Archivo**: `Services/TicketStateService.cs`  
**Tipo**: Ineficiencia

### Flujo actual dentro de una transacción:
```csharp
using var transaction = await _dbContext.Database.BeginTransactionAsync();

// Roundtrip 1: TicketRepository.UpdateAsync → ExecuteSqlRawAsync 
await _ticketRepository.UpdateAsync(ticket);

// Roundtrip 2: PaymentRepository.UpdateAsync → SaveChangesAsync
await _paymentRepository.UpdateAsync(payment);

// Roundtrip 3: TicketHistoryRepository.AddAsync → SaveChangesAsync
await _historyRepository.AddAsync(history);

await transaction.CommitAsync();  // Roundtrip 4: COMMIT
```

**4 roundtrips a la BD** cuando podrían ser 2.

### Versión optimizada:
```csharp
// Opción: batch las operaciones EF
_context.Payments.Update(payment);
_context.TicketHistory.Add(history);
await _context.SaveChangesAsync();  // 1 solo roundtrip para payment + history

await transaction.CommitAsync();
```

**Impacto técnico**: -2 roundtrips por mensaje. Bajo carga de 1000 msg/s → 2000 roundtrips/s ahorrados.

---

## 📊 Tabla Resumen: Señales de Código Generado por IA

| # | Señal de IA | Evidencia | Archivo |
|---|-------------|-----------|---------|
| 1 | **"Safety net" que contradicen el flujo** | HandleResult ACKea todo, dead BasicNack | TicketPaymentConsumer.cs |
| 2 | **Singleton genérico sin considerar contexto** | Un canal para N consumers | RabbitMQConnection.cs |
| 3 | **Doble fuente de configuración conflictiva** | `Environment.GetEnvironmentVariable` + `IConfiguration` bind | RabbitMQSettings.cs |
| 4 | **Copy-paste con variaciones mínimas** | TransitionToPaid/ToReleased 75% idénticos | TicketStateService.cs |
| 5 | **Capas que desperdician queries** | Lectura sin lock + lectura con lock | PaymentValidationService.cs |
| 6 | **Configuración generada pero no conectada** | PaymentSettings.ReservationTtlMinutes ignorado | PaymentValidationService.cs |
| 7 | **Clases huérfanas "por si acaso"** | DatabaseConfiguration, PaymentResponse, TicketPaymentEvent | 3 archivos |
| 8 | **Handlers idénticos copiados** | JsonOptions duplicado, misma estructura | Handlers/ |
| 9 | **Matching accidental que funciona por coincidencia** | EndsWith en dispatcher | PaymentEventDispatcherImpl.cs |
| 10 | **Dockerfile de API copiado para Worker** | EXPOSE, curl, healthcheck HTTP en un Worker | Dockerfile |
| 11 | **catch-all que silencia errores** | `catch { return false; }` sin log | PaymentRepository.cs |
| 12 | **XML doc genérico sobre-descriptivo** | `/// <inheritdoc/>` por todos lados sin valor | Múltiples |

---

## 🎯 Prioridad de Corrección

### Inmediato (bugs activos):
| # | Fix | Impacto | Esfuerzo |
|---|-----|---------|----------|
| 1 | HandleResult: NACK en fallos | Mensajes fallidos se pierden silenciosamente | 5 min |
| 3 | RabbitMQSettings: quitar `Environment.GetEnvironmentVariable` | HostName potencialmente vacío → conexión fallida | 10 min |
| 10 | Dockerfile: quitar EXPOSE/curl/healthcheck HTTP | Healthcheck siempre falla, imagen inflada | 10 min |

### Corto plazo (correctness + performance):
| # | Fix | Impacto | Esfuerzo |
|---|-----|---------|----------|
| 2 | Canal por consumer | Aislamiento de fallos, PrefetchCount correcto | 30 min |
| 5 | Eliminar query redundante | -1 roundtrip/mensaje, elimina race condition | 45 min |
| 9 | Dispatcher con matching exacto | Elimina falsos positivos | 20 min |

### Medio plazo (mantenibilidad):
| # | Fix | Impacto | Esfuerzo |
|---|-----|---------|----------|
| 4 | Extraer lógica transaccional común | -40 líneas duplicadas | 1h |
| 6 | Conectar PaymentSettings al TTL | Configuración dinámica | 10 min |
| 7 | Eliminar 3 clases huérfanas | Reduce ruido en codebase | 5 min |
| 8 | Handler base genérico | DRY, extensible | 30 min |
| 11 | Fix catch-all en PaymentRepository | Errores visibles | 5 min |
| 12 | Batch SaveChangesAsync | -2 roundtrips por mensaje | 20 min |

### Tiempo total estimado: ~4 horas

---

## 🔍 Veredicto Final

**Probabilidad de generación por IA**: **ALTA** (85%+)

**Indicadores principales**:
1. Sobre-abstracción prematura (3 capas donde 2 bastan)
2. Código generado "por completitud" que nadie conectó (3 clases huérfanas)
3. Copy-paste con variaciones mínimas (hallmark de generación iterativa)
4. Lógica correcta en lo superficial, bugs sutiles en los bordes (HandleResult, EndsWith)
5. Documentación XML genérica y verbosa que repite el nombre del método
6. Dockerfile copiado de template incorrecto (API → Worker)
7. Configuración dual conflictiva (la IA intentó cubrir "todos los escenarios")

**Lo que sí está bien hecho** (posiblemente humano o IA bien guiada):
- Patrón de dispatcher con OCP
- Concurrencia optimista con version field
- Transacciones con FOR UPDATE para bloqueo pesimista
- NpgsqlDataSourceBuilder con enum mapping correcto
- Entity configurations limpias y correctas
- Separación handler/dispatcher/service/repository

---

**Auditor**: Code Review — Detección de IA  
**Fecha**: 12 de febrero de 2026
