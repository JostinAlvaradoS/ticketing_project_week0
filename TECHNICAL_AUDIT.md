# 🔬 Auditoría Técnica - Sistema de Ticketing Distribuido

**Fecha**: 12 de febrero de 2026  
**Auditor**: Arquitecto Senior de Microservicios  
**Alcance**: Análisis completo de arquitectura, código y configuración  
**Metodología**: Revisión de código, análisis de patrones, evaluación de riesgos

---

## 📊 Resumen Ejecutivo

**Contexto**: Este es un **MVP (Producto Mínimo Viable)** para demostración y validación de concepto.

| Categoría | Hallazgos | MVP-Críticos | Producción-Alta | Mejoras-Futuras | Info |
|-----------|-----------|--------------|-----------------|-----------------|------|
| Seguridad | 8 | 1 | 2 | 4 | 1 |
| Arquitectura | 6 | 0 | 1 | 4 | 1 |
| RabbitMQ | 7 | 0 | 2 | 4 | 1 |
| Concurrencia | 4 | 0 | 1 | 2 | 1 |
| Performance | 5 | 0 | 0 | 4 | 1 |
| Resiliencia | 6 | 0 | 1 | 4 | 1 |
| Observabilidad | 4 | 0 | 0 | 3 | 1 |
| **TOTAL** | **40** | **1** | **7** | **25** | **7** |


**Estado General**: 🟢 **EXCELENTE para MVP, sólida base para evolución**

---

## 🚨 Hallazgos Críticos para MVP (1)

### MVP-CRIT-001: CORS Abierto en Demo Pública

**Severidad para MVP**: 🟡 **MEDIA** (⚠️ Solo si se expone públicamente)
**Severidad para Producción**: 🔴 **CRÍTICA**
**Archivo**: `producer/Producer/Program.cs` líneas 21-38
**Contexto MVP**: Aceptable para desarrollo local y demos internas

**Código Actual**:
```csharp
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowAll", policy =>
  {
    policy.AllowAnyOrigin()
        .AllowAnyMethod()
        .AllowAnyHeader();
  });
});
```

**Ventajas en MVP**:
- ✅ Permite desarrollo rápido sin configuración compleja
- ✅ Facilita testing desde múltiples orígenes
- ⚠️ No exponer a internet público con esta configuración
- ⚠️ Solo para ambientes de desarrollo/staging controlados

**Impacto en Producción**:
- ❌ Cualquier sitio web puede hacer requests a tu API
- ❌ Vulnerable a CSRF (Cross-Site Request Forgery)
- ❌ No hay control de orígenes permitidos
- ❌ Incumple políticas de seguridad corporativas

**Solución Propuesta**:
```csharp
builder.Services.AddCors(options =>
{
  options.AddPolicy("AllowFrontend", policy =>
  {
    var allowedOrigins = builder.Configuration
      .GetSection("Cors:AllowedOrigins")
      .Get<string[]>() ?? new[] { "http://localhost:3000" };
    policy.WithOrigins(allowedOrigins)
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .WithHeaders("Content-Type", "Authorization")
        .AllowCredentials()
        .SetIsOriginAllowedToAllowWildcardSubdomains();
  });
});
```

// En appsettings.json:
```json
{
  "Cors": {
  "AllowedOrigins": [
    "http://localhost:3000",
    "https://ticketing.ejemplo.com"
  ]
  }
}
```

**Acción para MVP**:
- ✅ Mantener como está para desarrollo
- ⚠️ Si subes a GitHub público: usar configuración restrictiva
- ⚡ Implementar ANTES de producción

**Solución rápida para demo público**:
```csharp
// Agregar solo esto si expones públicamente:
var allowedOrigins = new[] {
  "http://localhost:3000",
  Environment.GetEnvironmentVariable("ALLOWED_ORIGIN") ?? "http://localhost:3000"
};
policy.WithOrigins(allowedOrigins).AllowAnyMethod().AllowAnyHeader();
```

---

## 🔄 Hallazgos para Transición a Producción (7)

### PROD-001: Credenciales de RabbitMQ en Texto Plano

**Severidad para MVP**: 🟢 **ACEPTABLE** (ambiente local)  
**Severidad para Producción**: 🔴 **CRÍTICA**  
**Por qué es aceptable para MVP**:
- ✅ Proyecto corre solo en Docker local
- ✅ No expuesto a internet público
- ✅ Facilita replicación del ambiente
- ✅ Estándar en proyectos de desarrollo

**Riesgos en Producción**:
- ❌ Credenciales "guest" en RabbitMQ (usuario por defecto)
- ❌ Passwords sin encriptación

**Evidencia**:
```env
# .env - Credenciales por defecto expuestas
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
POSTGRES_PASSWORD=ticketing_password
```

**Problemas**:
- ❌ Credenciales "guest" en RabbitMQ (usuario por defecto)
- ❌ No hay rotación de credenciales
- ❌ Passwords sin encriptación

**Solución Propuesta**:

**Usar Docker Secrets**:
```yaml
# compose.yml
services:
  rabbitmq:
    secrets:
      - rabbitmq_user
      - rabbitmq_pass

secrets:
  rabbitmq_user:
    file: ./secrets/rabbitmq_user.txt
  rabbitmq_pass:
    file: ./secrets/rabbitmq_pass.txt
```

---

### PROD-002: Canales RabbitMQ Sin Pool (Optimización)

**Severidad para MVP**: 🟢 **NO CRÍTICO** (bajo volumen)  
**Severidad para Producción**: 🟠 **MEDIA-ALTA** (alta concurrencia)  
**Archivo**: `producer/Producer/Services/RabbitMQPaymentPublisher.cs`  
**Contexto MVP**: Funciona bien para demos y pruebas con < 100 usuarios concurrentes.
---

### CRIT-003: Canales RabbitMQ Creados Sin Gestión de Recursos

**Severidad**: 🔴 **CRÍTICA**  
**Por qué está bien para MVP**:
- ✅ Código más simple y directo
- ✅ Funciona perfectamente con carga baja/media
- ✅ RabbitMQ maneja bien hasta ~1000 canales
- ✅ MVP no requiere optimización prematura

**Cuándo se vuelve problema** (Producción):
- ⚠️ > 100 requests/segundo
- ⚠️ Múltiples instancias del servicio
- ⚠️ Operación 24/7 con alta concurrencia

**Acción para MVP**:
✅ **NINGUNA** - El código actual es correcto y suficiente

**Optimización para Producción** (cuando sea necesario)
    using var channel = _connection.CreateModel();  // ⚠️ PROBLEMA
    
    channel.ExchangeDeclare(
        exchange: _options.ExchangeName,
        type: ExchangeType.Topic,
        durable: true,
        autoDelete: false);
    
    // ... resto del código
}
```

**Problemas**:
- ❌ Se declara exchange en CADA publicación (innecesario)
- ❌ CreateModel() crea un canal que consume recursos
- ❌ No hay validación de que el canal esté abierto
- ❌ En alta carga puede agotar canales disponibles

**Solución Propuesta**:

```csharp
// Nuevo servicio: RabbitMQChannelPool.cs
public interface IRabbitMQChannelPool
{
    IModel GetChannel();
    void ReturnChannel(IModel channel);
}

public class RabbitMQChannelPool : IRabbitMQChannelPool, IDisposable
{
    private readonly IConnection _connection;
    private readonly ConcurrentBag<IModel> _channels;
    private readonly int _maxChannels;
    private int _currentChannels;
    private readonly ILogger<RabbitMQChannelPool> _logger;

    public RabbitMQChannelPool(
        IConnection connection, 
        IOptions<RabbitMQOptions> options,
        ILogger<RabbitMQChannelPool> logger)
    {
        _connection = connection;
        _channels = new ConcurrentBag<IModel>();
        _maxChannels = options.Value.MaxChannels ?? 10;
        _currentChannels = 0;
        _logger = logger;
        
        // Pre-crear exchange una sola vez al iniciar
        InitializeExchange(options.Value);
    }

    private void InitializeExchange(RabbitMQOptions options)
    {
        using var channel = _connection.CreateModel();
        channel.ExchangeDeclare(
            exchange: options.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false);
        
        _logger.LogInformation("Exchange {Exchange} inicializado", options.ExchangeName);
    }

    public IModel GetChannel()
    {
        if (_channels.TryTake(out var channel) && channel.IsOpen)
        {
            return channel;
        }

        if (_currentChannels < _maxChannels)
        {
            Interlocked.Increment(ref _currentChannels);
            return _connection.CreateModel();
        }

        // Esperar a que se libere un canal
        SpinWait.SpinUntil(() => _channels.TryTake(out channel), TimeSpan.FromSeconds(5));
        return channel ?? throw new InvalidOperationException("No channels available");
    }

    public void ReturnChannel(IModel channel)
    {
        if (channel.IsOpen)
        {
            _channels.Add(channel);
        }
        else
        {
            Interlocked.Decrement(ref _currentChannels);
        }
    }

    public void Dispose()
    {
        while (_channels.TryTake(out var channel))
        {
            channel?.Close();
            channel?.Dispose();
        }
    }
}

// RabbitMQPaymentPublisher refactorizado:
public class RabbitMQPaymentPublisher : IPaymentPublisher
{
    private readonly IRabbitMQChannelPool _channelPool;
    private readonly RabbitMQOptions _options;
    private readonly ILogger<RabbitMQPaymentPublisher> _logger;

    public async Task PublishPaymentApprovedAsync(PaymentApprovedEvent paymentEvent, ...)
    {
        var channel = _channelPool.GetChannel();
        try
        {
            var message = JsonSerializer.Serialize(paymentEvent);
            var body = Encoding.UTF8.GetBytes(message);

            var properties = channel.CreateBasicProperties();
            properties.Persistent = true;
            properties.ContentType = "application/json";

            channel.BasicPublish(
                exchange: _options.ExchangeName,
                routingKey: _options.PaymentApprovedRoutingKey,
                basicProperties: properties,
                body: body);

            _logger.LogInformation("Evento publicado: {TicketId}", paymentEvent.TicketId);
        }

      **Cuándo implementar**:
- MVP: ❌ NO necesario
- Producción: ✅ Cuando pruebas de carga muestren degradación

---

### PROD-003: No Hay Autenticación ni Autorización

**Severidad para MVP**: 🟢 **ACEPTABLE** (demo controlada)  
**Severidad para Producción**: 🔴 **CRÍTICA**  
**Archivo**: Todos los controladores  
**Contexto MVP**: APIs internas, sin exposición pública, usuarios de confianza.

**Evidencia**:
- Endpoints administrativos (crear/eliminar eventos, crear tickets) no están protegidos por autenticación/autorización.

**Por qué es aceptable para MVP**:
- ✅ Simplifica pruebas y desarrollo
- ✅ Reduce complejidad del demo
- ✅ Ambiente controlado (no internet público)

**Riesgo en Producción**:
- ❌ Cualquiera puede crear/eliminar eventos y tickets
- ❌ No hay diferenciación de roles (admin vs buyer)
- ❌ Sin trazabilidad de quién ejecutó acciones (audit trail)

**Acción recomendada para demo pública**:
- Agregar nota explícita en README: “Este MVP no incluye autenticación. No exponer a internet público.”

**Solución para Producción (baseline)**:
- Implementar JWT Bearer + policies por rol (AdminOnly / BuyerOrAdmin) y proteger endpoints sensibles.

**Prioridad**: ⚡ Implementar antes de cualquier exposición pública o stress testing

---

### PROD-004: Frontend Polling Excesivo

**Severidad para MVP**: 🟢 **ACEPTABLE** (pocos usuarios)  
**Severidad para Producción**: 🟠 **MEDIA**  
**Archivo**: `frontend/hooks/use-payment-status.ts`  
**Contexto MVP**: 500ms está bien para 10-50 usuarios concurrentes

**Por qué está bien para MVP**:
- ✅ Simple y funciona
- ✅ Feedback rápido para el usuario
- ✅ No sobrecarga el backend con poco tráfico
- ✅ Fácil de entender y mantener

**Acción MVP**: ✅ Mantener como está

**Mejora para Producción**: Backoff exponencial o WebSockets (ver implementación en sección de optimizaciones)

---

### PROD-005 a PROD-007: Otros Hallazgos de Producción

- **PROD-005**: Sin Circuit Breaker → Implementar cuando haya múltiples servicios dependientes
- **PROD-006**: Sin Rate Limiting → Implementar antes de exposición pública
- **PROD-007**: Falta Dead Letter Queue → Agregar cuando operación 24/7

---

## 📈 Mejoras Futuras (25)

> Estas son optimizaciones para cuando el MVP evolucione. **NO implementar ahora**.

### FUT-001: Connection Pooling Explícito en Entity Framework

**Severidad para MVP**: ℹ️ **INFORMATIVA**  
**Contexto**: EF Core ya hace pooling por defecto. Configuración explícita es optimización prematura.
    public string CreatedBy { get; set; }  // User ID o email
    public DateTime CreatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
```

**Prioridad**: ⚡ Implementar ANTES de producción

---

## ⚠️ Hallazgos de Severidad Alta (14)

### HIGH-001: Connection Pooling Inadecuado en Entity Framework

**Severidad**: 🟠 **ALTA**  
**Archivo**: Configuración de DbContext en todos los servicios  
**Cuándo considerar**: 
- Cuando métricas muestren > 1000 conexiones simultáneas a BD
- Cuando aparezcan errores de "too many connections"

**Implementación**: Ver código completo en sección de anexos técnicos

---

### FUT-002: RabbitMQ Alta Disponibilidad
```csharp
// crud_service/Extensions/ServiceExtensions.cs
services.AddDbContext<TicketingDbContext>(options =>
    options para MVP**: ℹ️ **NO NECESARIO**  
**Contexto**: Single instance de RabbitMQ es suficiente para MVP
**Solución**:
```csharp
services.AddDbContext<TicketingDbContext>(options =>
{
    var connectionString = configuration.GetConnectionString("DefaultConnection");
    
    // Configurar connection pooling explícitamente
    var builder = new NpgsqlConnectionStringBuilder(connectionString)
    {
        Pooling = true,
        MinPoolSize = 5,
        MaxPoolSize = 100,
        ConnectionIdleLifetime = 300,  // 5 minutos
        ConnectionPruningInterval = 10,
        CommandTimeout = 30,
        Timeout = 15
    };
    
    options.UseNpgsql(
        builder.ConnectionString,
        npgsqlOptions =>
        {
            npgsqlOptions.EnableRetryOnFailure(
                maxRetryCount: 3,
                maxRetryDelay: TimeSpan.FromSeconds(5),
                errorCodesToAdd: null);
            
            npgsqlOptions.CommandTimeout(30);
        });
    
    // Logging en desarrollo
    if (env.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});
```

---

### HIGH-002: Frontend Polling Excesivo (500ms)

**Severidad**: 🟠 **ALTA**  
**Archivo**: `frontend/hooks/use-payment-status.ts`  
**Riesgo**: Carga innecesaria en backend, mala UX

**Código Actual**:
```typescript
pollInterval = setInterval(pollPaymentStatus, 500) // ⚠️ Cada 500ms
```

**Impacto**:
- ❌ 120 requests por minuto por usuario
- ❌ Carga innecesaria en CRUD Service
- ❌ Consume ancho de banda
- ❌ No escala con muchos usuarios

**Soluciones Propuestas**:

**Opción 1: Backoff Exponencial**
```typescript
export function usePaymentStatus({
  ticketId,
  onPaymentConfirmed,
  onPaymentRejected,
  maxDuration = 10,
}: UsePaymentStatusOptions) {
  const [isPolling, setIsPolling] = useState(false)
  const [pollInterval, setPollInterval] = useState(1000) // Empezar en 1s

  useEffect(() => {
    if (!ticketId || !isPolling) return

    let attempts = 0
    let currentInterval = 1000
    
    const pollPaymentStatus = async () => {
      try {
        const ticket = await api.getTicket(ticketId)
        
        if (ticket.status === "paid") {
          setIsPolling(false)
          onPaymentConfirmed?.()
          return
        }
        
        // Exponential backoff: 1s, 2s, 3s, 5s, 8s...
        attempts++
        currentInterval = Math.min(
          1000 * Math.min(attempts, 8),  // Cap at 8 seconds
          8000
        )
        
        setTimeout(pollPaymentStatus, currentInterval)
      } catch (error) {
        console.error("Error polling:", error)
        setIsPolling(false)
        onPaymentRejected?.("Error verificando estado")
      }
    }
    
    pollPaymentStatus()
  }, [ticketId, isPolling])
  
  return { isPolling, startPolling: () => setIsPolling(true) }
}
```

**Opción 2: WebSockets con SignalR (Recomendado para producción)**
```csharp
// Backend: Agregar SignalR Hub
public class TicketHub : Hub
{
    public async Task SubscribeToTicket(long ticketId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"ticket-{ticketId}");
    }
}

// Notificar cuando cambie el status
public class TicketStateService
{
    private readonly IHubContext<TicketHub> _hubContext;
    
    public async Task TransitionToPaidAsync(long ticketId, string txnRef)
    {
        // ... actualizar BD ...
        
        // Notificar a través de SignalR
        await _hubContext.Clients
            .Group($"ticket-{ticketId}")
            .SendAsync("TicketStatusChanged", new { ticketId, status = "paid" });
    }
}

// Frontend: Conectar con SignalR
import * as signalR from "@microsoft/signalr"

const connection = new signalR.HubConnectionBuilder()
  .withUrl("http://localhost:8002/ticketHub")
  .withAutomaticReconnect()
  .build()

connection.on("TicketStatusChanged", (data) => {
  if (data.status === "paid") {
    onPaymentConfirmed()
  }
})

await connection.start()
await connection.invoke("SubscribeToTicket", ticketId)
```

---

### HIGH-003: Sin Circuit Breaker Entre Servicios

**Severidad**: 🟠 **ALTA**  
**Riesgo**: Cascada de fallos si un servicio se cae

**Solución con Polly**:
```csharp
// dotnet add package Polly
// dotnet add package Polly.Extensions.Http

// Program.cs
builder.Services.AddHttpClient("CrudService", client =>
{
    client.BaseAddress = new Uri("http://crud-service:8080");
    client.Timeout = TimeSpan.FromSeconds(30);
})
.AddTransientHttpErrorPolicy(policyBuilder =>
    policyBuilder.CircuitBreakerAsync(
        handledEventsAllowedBeforeBreaking: 5,
        durationOfBreak: TimeSpan.FromSeconds(30),
        onBreak: (outcome, duration) =>
        {
            // Log circuit breaker opened
        },
        onReset: () =>
        {
            // Log circuit breaker reset
        }))
.AddTransientHttpErrorPolicy(policyBuilder =>
    policyBuilder.WaitAndRetryAsync(
        3,
        retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
        onRetry: (outcome, timespan, retryCount, context) =>
        {
            // Log retry attempt
        }));
```

---

### HIGH-004: Falta Dead Letter Queue para Mensajes Fallidos

**Severidad**: 🟠 **ALTA**  
**Archivo**: `scripts/setup-rabbitmq.sh`  
**Riesgo**: Pérdida de mensajes que fallan repetidamente

**Solución**:
```bash
# Crear DLX (Dead Letter Exchange)
echo "Creando Dead Letter Exchange"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"type":"topic","durable":true}' \
  "$RABBIT_URL/exchanges/$VHOST/dlx.tickets"

# Crear DLQ (Dead Letter Queue)
echo "Creando Dead Letter Queue"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{"durable":true}' \
  "$RABBIT_URL/queues/$VHOST/dlq.tickets.all"

# Binding DLQ al DLX
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X POST \
  -H "content-type:application/json" \
  -d '{"routing_key":"#"}' \
  "$RABBIT_URL/bindings/$VHOST/e/dlx.tickets/q/dlq.tickets.all"

# Modificar colas existentes para usar DLX
echo "Configurando DLX en q.ticket.payments.approved"
curl -s -u "$RABBIT_USER:$RABBIT_PASS" -X PUT \
  -H "content-type:application/json" \
  -d '{
    "durable":true,
    "arguments":{
      "x-dead-letter-exchange":"dlx.tickets",
      "x-dead-letter-routing-key":"dlq.payments.approved"
    }
  }' \
  "$RABBIT_URL/queues/$VHOST/q.ticket.payments.approved"
```

**Consumer debe hacer NACK con requeue=false**:
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

    // Si falla después de reintentos, enviar a DLQ
    channel.BasicNack(
        deliveryTag: args.DeliveryTag,
        multiple: false,
        requeue: false);  // ⚠️ IMPORTANTE: false = envía a DLX
}
```

---

### HIGH-005: RabbitMQ Sin Alta Disponibilidad

**Severidad**: 🟠 **ALTA**  
**Archivo**: `compose.yml`  
**Riesgo**: Single Point of Failure

**Configuración Actual**:
```yaml
rabbitmq:
  image: rabbitmq:3.12-management-alpine
  # ⚠️ Sin réplicas, sin clustering
```

**Solución (RabbitMQ Cluster)**:
```yaml
# compose.ha.yml
services:
  rabbitmq-1:
    image: rabbitmq:3.12-management-alpine
    hostname: rabbitmq-1
    environment:
      RABBITMQ_ERLANG_COOKIE: "super_secret_cookie_change_me"
      RABBITMQ_DEFAULT_USER: ${RABBITMQ_DEFAULT_USER}
      RABBITMQ_DEFAULT_PASS: ${RABBITMQ_DEFAULT_PASS}
    volumes:
      - rabbitmq1_data:/var/lib/rabbitmq
    networks:
      - ticketing_network

  rabbitmq-2:
    image: rabbitmq:3.12-management-alpine
    hostname: rabbitmq-2
    environment:
      RABBITMQ_ERLANG_COOKIE: "super_secret_cookie_change_me"
    depends_on:
      - rabbitmq-1
    volumes:
      - rabbitmq2_data:/var/lib/rabbitmq
      - ./scripts/join-cluster.sh:/join-cluster.sh
    entrypoint: /join-cluster.sh rabbitmq-1
    networks:
      - ticketing_network

  haproxy:
    image: haproxy:2.8-alpine
    ports:
      - "5672:5672"
      - "15672:15672"
    volumes:
      - ./config/haproxy.cfg:/usr/local/etc/haproxy/haproxy.cfg:ro
    depends_on:
      - rabbitmq-1
      - rabbitmq-2
    networks:
      - ticketing_network

volumes:
  rabbitmq1_data:
  rabbitmq2_data:
```

---

### HIGH-006: Falta Rate Limiting en APIs

**Severidad**: 🟠 **ALTA**  
**Riesgo**: Ataques DoS, abuso de recursos

**Solución con AspNetCoreRateLimit**:
```csharp
// dotnet add package AspNetCoreRateLimit

// Program.cs
builder.Services.AddMemoryCache();
builder.Services.Configure<IpRateLimitOptions>(options =>
{
    options.EnableEndpointRateLimiting = true;
    options.StackBlockedRequests = false;
    options.HttpStatusCode = 429;
    options.RealIpHeader = "X-Real-IP";
    options.GeneralRules = new List<RateLimitRule>
    {
        new RateLimitRule
        {
            Endpoint = "*",
            Period = "1m",
            Limit = 60  // 60 requests por minuto
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/tickets/reserve",
            Period = "1m",
            Limit = 10  // Más restrictivo para reservas
        },
        new RateLimitRule
        {
            Endpoint = "POST:/api/payments/process",
            Period = "1m",
            Limit = 5   // Muy restrictivo para pagos
        }
    };
});

builder.Services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
builder.Services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
builder.Services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
builder.Services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();

app.UseIpRateLimiting();
```

---

### HIGH-007: Sin Índices en Queries Comunes

**Severidad**: 🟠 **ALTA**  
**Archivo**: `scripts/schema.sql`  
**Riesgo**: Performance degradada con muchos datos

**Índices Faltantes**:
```sql
-- Agregar índices adicionales para queries comunes

-- Búsqueda de tickets por status y event_id (filtros combinados)
CREATE INDEX idx_tickets_event_status 
ON tickets(event_id, status);

-- Búsqueda de pagos por status (monitoreo de pagos pendientes)
CREATE INDEX idx_payments_status_created 
ON payments(status, created_at DESC);

-- Búsqueda de tickets por reserved_by (historial de compras de usuario)
CREATE INDEX idx_tickets_reserved_by 
ON tickets(reserved_by) 
WHERE reserved_by IS NOT NULL;

-- Búsqueda de historial reciente
CREATE INDEX idx_ticket_history_recent 
ON ticket_history(ticket_id, changed_at DESC);

-- Búsqueda de eventos próximos
CREATE INDEX idx_events_upcoming 
ON events(starts_at) 
WHERE starts_at > NOW();

-- Partial index para tickets expirados (query común del consumer)
CREATE INDEX idx_tickets_expired_reserved 
ON tickets(expires_at) 
WHERE status = 'reserved' AND expires_at < NOW();
```

---

### HIGH-008: Logs Pueden Exponer Información Sensible

**Severidad**: 🟠 **ALTA**  
**Archivos**: Múltiples servicios

**Código Problemático**:
```csharp
_logger.LogInformation(
    "Pago aprobado: TicketId={TicketId}, TransactionRef={TransactionRef}",
    paymentEvent.TicketId,
    paymentEvent.TransactionRef);  // ⚠️ Podría contener datos sensibles
```

**Solución con Log Redaction**:
```csharp
// LoggingExtensions.cs
public static class LoggingExtensions
{
    public static string RedactSensitive(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;
        
        // Redactar PII (emails)
        if (value.Contains("@"))
        {
            var parts = value.Split('@');
            return $"{parts[0].Substring(0, Math.Min(3, parts[0].Length))}***@{parts[1]}";
        }
        
        // Redactar números de tarjeta (si los hay)
        if (value.Length > 10 && value.All(char.IsDigit))
        {
            return $"****{value.Substring(value.Length - 4)}";
        }
        
        // Redactar transaction refs largos
        if (value.Length > 20)
        {
            return $"{value.Substring(0, 8)}***{value.Substring(value.Length - 4)}";
        }
        
        return value;
    }
}

// Uso:
_logger.LogInformation(
    "Pago aprobado: TicketId={TicketId}, User={User}, TxnRef={TxnRef}",
    paymentEvent.TicketId,
    paymentEvent.PaymentBy.RedactSensitive(),  // user***@example.com
    paymentEvent.TransactionRef.RedactSensitive());  // txn_abc***xyz
```

---

### HIGH-009: Simulación de Pago Muy Simple

**Severidad**: 🟠 **ALTA**  
**Archivo**: `producer/Producer/Controllers/PaymentsController.cs`  
**Problema**: No simula casos reales (timeouts, errores intermitentes)

**Código Actual**:
```csharp
private async Task<bool> SimulatePaymentProcessing(...)
{
    await Task.Delay(Random.Shared.Next(100, 500));
    return Random.Shared.Next(100) < 80;  // ⚠️ Muy simple
}
```

**Mejora**:
```csharp
private async Task<PaymentSimulationResult> SimulatePaymentProcessing(
    ProcessPaymentRequest request,
    CancellationToken cancellationToken)
{
    // Simular latencia realista (100-2000ms)
    var latency = Random.Shared.Next(100, 2000);
    await Task.Delay(latency, cancellationToken);
    
    // Casos de fallo realistas
    var scenario = Random.Shared.Next(100);
    
    return scenario switch
    {
        < 70 => new PaymentSimulationResult  // 70% éxito
        {
            IsApproved = true,
            Reason = "Approved",
            ProcessingTimeMs = latency
        },
        < 85 => new PaymentSimulationResult  // 15% fondos insuficientes
        {
            IsApproved = false,
            Reason = "Insufficient funds",
            ProcessingTimeMs = latency
        },
        < 95 => new PaymentSimulationResult  // 10% tarjeta rechazada
        {
            IsApproved = false,
            Reason = "Card declined by issuer",
            ProcessingTimeMs = latency
        },
        < 98 => throw new TimeoutException(  // 3% timeout
            "Payment gateway timeout"),
        _ => throw new HttpRequestException(  // 2% error de red
            "Payment gateway unreachable")
    };
}

public class PaymentSimulationResult
{
    public bool IsApproved { get; set; }
    public string Reason { get; set; }
    public int ProcessingTimeMs { get; set; }
}
```

---

### HIGH-010: Falta Validación de Business Rules en Eventos

**Severidad**: 🟠 **ALTA**  
**Archivo**: `paymentService/MsPaymentService.Worker/Services/PaymentValidationService.cs`

**Mejorar Validación**:
```csharp
public async Task<ValidationResult> ValidateAndProcessApprovedPaymentAsync(
    PaymentApprovedEvent paymentEvent)
{
    // Validaciones adicionales
    
    // 1. Validar que el amount coincida con el precio del evento
    var ticket = await _ticketRepository.GetByIdAsync(paymentEvent.TicketId);
    var eventPrice = await _eventRepository.GetPriceAsync(ticket.EventId);
    
    if (paymentEvent.AmountCents != eventPrice.AmountCents)
    {
        _logger.LogWarning(
            "Amount mismatch: Expected {Expected}, Got {Actual}",
            eventPrice.AmountCents,
            paymentEvent.AmountCents);
        return ValidationResult.Failure("Amount does not match event price");
    }
    
    // 2. Validar que el evento no haya sido cancelado
    var @event = await _eventRepository.GetByIdAsync(ticket.EventId);
    if (@event.Status == EventStatus.Cancelled)
    {
        _logger.LogWarning("Attempted payment for cancelled event {EventId}", @event.Id);
        return ValidationResult.Failure("Event has been cancelled");
    }
    
    // 3. Validar que no haya pasado el evento
    if (@event.StartsAt < DateTime.UtcNow)
    {
        _logger.LogWarning("Attempted payment for past event {EventId}", @event.Id);
        return ValidationResult.Failure("Event has already occurred");
    }
    
    // ... resto de validaciones existentes
}
```

---

## 📊 Hallazgos de Severidad Media (16)

### MED-001: Falta Health Checks Detallados

**Severidad**: 🟡 **MEDIA**

**Solución**:
```csharp
// dotnet add package AspNetCore.HealthChecks.Npgsql
// dotnet add package AspNetCore.HealthChecks.RabbitMQ

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString,
        name: "postgresql",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "db", "sql", "postgresql" })
    .AddRabbitMQ(
        rabbitConnectionString,
        name: "rabbitmq",
        failureStatus: HealthStatus.Unhealthy,
        tags: new[] { "messaging", "rabbitmq" })
    .AddUrlGroup(
        new Uri("http://crud-service:8080/health"),
        name: "crud-service",
        failureStatus: HealthStatus.Degraded,
        tags: new[] { "service" });

app.MapHealthChecks("/health", new HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var response = new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(x => new
            {
                name = x.Key,
                status = x.Value.Status.ToString(),
                description = x.Value.Description,
                duration = x.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        };
        await context.Response.WriteAsJsonAsync(response);
    }
});

app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = _ => false  // Solo verifica que la app esté viva
});
```

---

### MED-003: Frontend Sin Manejo de Errores Centralizado

**Severidad**: 🟡 **MEDIA**

**Solución**:
```typescript
// lib/error-handler.ts
import { toast } from "@/hooks/use-toast"
import { ApiError } from "./api"

export class ErrorHandler {
  static handle(error: unknown, context?: string) {
    console.error(`Error in ${context}:`, error)
    
    if (error instanceof ApiError) {
      // Errores de API específicos
      switch (error.status) {
        case 400:
          toast({
            title: "Datos inválidos",
            description: error.message,
            variant: "destructive"
          })
          break
        case 401:
          toast({
            title: "No autenticado",
            description: "Por favor inicia sesión",
            variant: "destructive"
          })
          // Redirect to login
          window.location.href = "/login"
          break
        case 403:
          toast({
            title: "Acceso denegado",
            description: "No tienes permisos para esta acción",
            variant: "destructive"
          })
          break
        case 404:
          toast({
            title: "No encontrado",
            description: error.message,
            variant: "destructive"
          })
          break
        case 409:
          toast({
            title: "Conflicto",
            description: error.message || "El recurso ya existe o está en uso",
            variant: "destructive"
          })
          break
        case 429:
          toast({
            title: "Demasiadas solicitudes",
            description: "Por favor espera un momento antes de reintentar",
            variant: "destructive"
          })
          break
        case 500:
        case 502:
        case 503:
          toast({
            title: "Error del servidor",
            description: "Estamos experimentando problemas. Intenta de nuevo más tarde.",
            variant: "destructive"
          })
          break
        default:
          toast({
            title: "Error inesperado",
            description: error.message,
            variant: "destructive"
          })
      }
    } else if (error instanceof Error) {
      // Errores de JavaScript genéricos
      toast({
        title: "Error",
        description: error.message,
        variant: "destructive"
      })
    } else {
      // Error desconocido
      toast({
        title: "Error desconocido",
        description: "Algo salió mal. Por favor intenta de nuevo.",
        variant: "destructive"
      })
    }
  }
}

// Uso en componentes:
try {
  await api.reserveTicket(payload)
} catch (error) {
  ErrorHandler.handle(error, "reserveTicket")
}
```

---

### MED-004: Falta Monitoreo y Métricas (Observabilidad)

**Severidad**: 🟡 **MEDIA**

**Solución con OpenTelemetry**:
```csharp
// dotnet add package OpenTelemetry.Extensions.Hosting
// dotnet add package OpenTelemetry.Instrumentation.AspNetCore
// dotnet add package OpenTelemetry.Instrumentation.Http
// dotnet add package OpenTelemetry.Exporter.Prometheus.AspNetCore

builder.Services.AddOpenTelemetry()
    .WithMetrics(metrics =>
    {
        metrics
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddRuntimeInstrumentation()
            .AddProcessInstrumentation()
            .AddPrometheusExporter();
    })
    .WithTracing(tracing =>
    {
        tracing
            .AddAspNetCoreInstrumentation()
            .AddHttpClientInstrumentation()
            .AddNpgsql()
            .AddSource("RabbitMQ")
            .AddJaegerExporter();
    });

// Custom metrics
public class TicketingMetrics
{
    private readonly Counter<long> _ticketsReserved;
    private readonly Counter<long> _paymentsProcessed;
    private readonly Histogram<double> _reservationDuration;

    public TicketingMetrics(IMeterFactory meterFactory)
    {
        var meter = meterFactory.Create("Ticketing.Metrics");
        
        _ticketsReserved = meter.CreateCounter<long>(
            "tickets_reserved_total",
            description: "Total number of ticket reservations");
        
        _paymentsProcessed = meter.CreateCounter<long>(
            "payments_processed_total",
            description: "Total number of payments processed");
        
        _reservationDuration = meter.CreateHistogram<double>(
            "reservation_duration_seconds",
            description: "Duration of reservation process");
    }

    public void RecordTicketReserved(string status) =>
        _ticketsReserved.Add(1, new KeyValuePair<string, object>("status", status));

    public void RecordPayment(string status) =>
        _paymentsProcessed.Add(1, new KeyValuePair<string, object>("status", status));

    public void RecordReservationDuration(double seconds) =>
        _reservationDuration.Record(seconds);
}
```

---

### MED-005: Docker Compose Sin Resource Limits

**Severidad**: 🟡 **MEDIA**

**Solución**:
```yaml
services:
  postgres:
    # ... configuración existente ...
    deploy:
      resources:
        limits:
          cpus: '1.0'
          memory: 1G
        reservations:
          cpus: '0.5'
          memory: 512M
    restart: unless-stopped
    logging:
      driver: "json-file"
      options:
        max-size: "10m"
        max-file: "3"

  rabbitmq:
    # ... configuración existente ...
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
        reservations:
          cpus: '0.25'
          memory: 256M
    ulimits:
      nofile:
        soft: 65536
        hard: 65536

  crud-service:
    # ... configuración existente ...
    deploy:
      resources:
        limits:
          cpus: '0.5'
          memory: 512M
    restart: always
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 30s
      timeout: 10s
      retries: 3
      start_period: 40s
```

---

### MED-006: Falta Validación de Datos de Entrada Completa

**Severidad**: 🟡 **MEDIA**

**Solución con FluentValidation**:
```csharp
// dotnet add package FluentValidation.AspNetCore

// Validators/CreateEventRequestValidator.cs
public class CreateEventRequestValidator : AbstractValidator<CreateEventRequest>
{
    public CreateEventRequestValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es requerido")
            .MinimumLength(3).WithMessage("El nombre debe tener al menos 3 caracteres")
            .MaximumLength(200).WithMessage("El nombre no puede exceder 200 caracteres")
            .Matches(@"^[a-zA-Z0-9\s\-]+$").WithMessage("El nombre contiene caracteres inválidos");

        RuleFor(x => x.StartsAt)
            .NotEmpty().WithMessage("La fecha de inicio es requerida")
            .GreaterThan(DateTime.UtcNow).WithMessage("La fecha debe ser futura")
            .LessThan(DateTime.UtcNow.AddYears(2)).WithMessage("La fecha no puede ser más de 2 años en el futuro");
    }
}

public class ReserveTicketRequestValidator : AbstractValidator<ReserveTicketRequest>
{
    public ReserveTicketRequestValidator()
    {
        RuleFor(x => x.EventId)
            .GreaterThan(0).WithMessage("EventId inválido");

        RuleFor(x => x.TicketId)
            .GreaterThan(0).WithMessage("TicketId inválido");

        RuleFor(x => x.OrderId)
            .NotEmpty().WithMessage("OrderId es requerido")
            .Matches(@"^ORD-[A-Z0-9]+$").WithMessage("OrderId debe tener formato ORD-XXXXX");

        RuleFor(x => x.ReservedBy)
            .NotEmpty().WithMessage("Email es requerido")
            .EmailAddress().WithMessage("Email inválido")
            .MaximumLength(120).WithMessage("Email muy largo");

        RuleFor(x => x.ExpiresInSeconds)
            .InclusiveBetween(60, 3600).WithMessage("La expiración debe estar entre 1 minuto y 1 hora");
    }
}

// Program.cs
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<CreateEventRequestValidator>();
```

---

### MED-008: Falta Soft Delete en Eventos

**Severidad**: 🟡 **MEDIA**

**Problema**: DELETE permanente dificulta auditoría

**Solución**:
```csharp
public class Event
{
    public long Id { get; set; }
    public string Name { get; set; }
    public DateTime StartsAt { get; set; }
    
    // Soft delete
    public bool IsDeleted { get; set; } = false;
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
}

// Configuración en DbContext
modelBuilder.Entity<Event>()
    .HasQueryFilter(e => !e.IsDeleted);  // Automáticamente filtra eventos eliminados

// Servicio
public async Task<bool> DeleteEventAsync(long id)
{
    var @event = await _eventRepository.GetByIdAsync(id);
    if (@event == null) return false;
    
    @event.IsDeleted = true;
    @event.DeletedAt = DateTime.UtcNow;
    @event.DeletedBy = _currentUserService.GetUserId();
    
    await _eventRepository.UpdateAsync(@event);
    return true;
}

// Para queries administrativas que necesitan ver eliminados
var allEvents = await _dbContext.Events
    .IgnoreQueryFilters()
    .ToListAsync();
```

---

### MED-010 a MED-016: Resumen de Otros Hallazgos Medios

- **MED-010**: Falta paginación en listados grandes
- **MED-011**: No hay compresión de respuestas (gzip)
- **MED-012**: Falta caché HTTP (ETags, Cache-Control)
- **MED-013**: RabbitMQ messages sin TTL configurado
- **MED-014**: Falta validación de duplicados en OrderId
- **MED-015**: No hay endpoints de estadísticas/analytics
- **MED-016**: Frontend sin loading skeletons en todas las vistas

---

## 🔵 Hallazgos de Severidad Baja (6)

### LOW-001: Magic Numbers en Código
```csharp
// Malo
await Task.Delay(Random.Shared.Next(100, 500));
ticket.Version++;

// Mejor
private const int MIN_SIMULATION_DELAY_MS = 100;
private const int MAX_SIMULATION_DELAY_MS = 500;
private const int VERSION_INCREMENT = 1;

await Task.Delay(Random.Shared.Next(MIN_SIMULATION_DELAY_MS, MAX_SIMULATION_DELAY_MS));
ticket.Version += VERSION_INCREMENT;
```


### LOW-003: Nombres de Variables Inconsistentes
- `@event` vs `evt` vs `eventEntity`
- Estandarizar nomenclatura (nombres consistentes)