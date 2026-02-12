# FEEDBACK_BACKEND.md

**Proyecto**: Ticketing System (TicketRush MVP)  
**Rol**: Backend Developer  
**Fecha**: 12 de febrero de 2026  
**Servicio Revisado**: crud_service (.NET 8)

---

## Resumen de Hallazgos

| Categoria | Encontrados | Corregidos | Pendientes |
|-----------|-------------|------------|------------|
| Bugs criticos | 2 | 2 | 0 |
| Bugs medios | 2 | 0 | 2 |
| Bugs menores | 1 | 0 | 1 |
| Seguridad | 1 | 0 | 1 |
| Mejoras | 1 | 0 | 1 |
| **TOTAL** | **7** | **2** | **5** |

---

## Hallazgos Detallados

### [CRIT-001] Creacion de tickets extremadamente ineficiente

**Severidad**: CRITICA  
**Archivo**: crud_service/Services/TicketService.cs  
**Lineas**: 51-65  
**Estado**: CORREGIDO

**Descripcion**:
El metodo CreateTicketsAsync realiza N llamadas individuales a la base de datos cuando se crean tickets en lote. Si se crean 1000 tickets, se hacen 1000 INSERTs individuales, lo cual:
- Satura conexiones de BD
- Es extremadamente lento
- Puede causar timeouts

**Codigo problematico**:
```csharp
public async Task<IEnumerable<TicketDto>> CreateTicketsAsync(long eventId, int quantity)
{
    var tickets = new List<Ticket>();

    for (int i = 0; i < quantity; i++)
    {
        var ticket = new Ticket
        {
            EventId = eventId,
            Status = TicketStatus.Available
        };

        var created = await _ticketRepository.AddAsync(ticket);  // <-- Una llamada por ticket!
        tickets.Add(created);
    }
    // ...
}
```

**Solucion propuesta**:
Implementar bulk insert usando AddRangeAsync para insertar todos los tickets en una sola operacion.

---

### [CRIT-002] Mapeo incorrecto de ENUMs PostgreSQL (Error 500)

**Severidad**: CRITICA  
**Archivo**: crud_service/Extensions/ServiceExtensions.cs, crud_service/Data/TicketingDbContext.cs  
**Estado**: CORREGIDO  
**Descubierto durante**: Pruebas de CRIT-001

**Descripcion**:
Al probar la creacion de tickets, el servicio retornaba error 500 con el mensaje:
```
column "status" is of type ticket_status but expression is of type text
```

**Por que es un problema**:
- PostgreSQL define tipos ENUM nativos (`ticket_status`, `payment_status`) en schema.sql
- Entity Framework usaba `.HasConversion<string>()` que envia texto plano
- PostgreSQL rechaza el INSERT porque espera el tipo ENUM, no texto
- **Este bug existia antes de nuestra mejora** - el codigo original tampoco funcionaba

**Codigo problematico**:
```csharp
// En TicketingDbContext.cs
modelBuilder.Entity<Ticket>()
    .Property(t => t.Status)
    .HasConversion<string>();  // <-- Envia "Available" como texto

// En ServiceExtensions.cs
services.AddDbContext<TicketingDbContext>(options =>
    options.UseNpgsql(connectionString));  // <-- Sin mapeo de ENUMs
```

**La solucion implementada**:
1. Registrar los ENUMs de PostgreSQL en NpgsqlDataSourceBuilder:
```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
var dataSource = dataSourceBuilder.Build();
```

2. Declarar los ENUMs en OnModelCreating:
```csharp
modelBuilder.HasPostgresEnum<TicketStatus>("ticket_status");
modelBuilder.HasPostgresEnum<PaymentStatus>("payment_status");
```

3. Quitar `.HasConversion<string>()` porque ya no es necesario

**Archivos modificados**:
- crud_service/Extensions/ServiceExtensions.cs
- crud_service/Data/TicketingDbContext.cs

---

### [MED-001] No se valida existencia de evento antes de crear tickets

**Severidad**: MEDIA  
**Archivo**: crud_service/Services/TicketService.cs  
**Lineas**: 51-65  
**Estado**: PENDIENTE

**Descripcion**:
El metodo CreateTicketsAsync no verifica si el evento existe antes de intentar crear tickets. Esto puede causar:
- Errores de FK constraint en la BD
- Mensajes de error confusos para el usuario
- Tickets huerfanos si la validacion de FK no esta activa

**Solucion propuesta**:
Agregar validacion de existencia del evento antes de crear tickets.

---

### [MED-002] Falta transaccion en operaciones de multiples entidades

**Severidad**: MEDIA  
**Archivo**: crud_service/Services/TicketService.cs  
**Lineas**: 70-95, 100-125  
**Estado**: PENDIENTE

**Descripcion**:
Los metodos UpdateTicketStatusAsync y ReleaseTicketAsync modifican el ticket Y crean un registro en historial como operaciones separadas. Si la segunda falla:
- El ticket queda actualizado
- No hay registro de auditoria
- Se pierde consistencia de datos

**Solucion propuesta**:
Usar transaccion explicita para garantizar atomicidad.

---

### [SEC-001] CORS abierto permite cualquier origen

**Severidad**: MEDIA (para MVP aceptable, critico para produccion)  
**Archivo**: crud_service/Program.cs  
**Lineas**: 20-27  
**Estado**: PENDIENTE

**Descripcion**:
La configuracion CORS permite cualquier origen (AllowAnyOrigin()), lo cual en produccion permite que cualquier sitio web haga requests a la API.

**Nota**: Para MVP/demo interno es aceptable. Documentar para produccion.

---

### [MIN-001] Archivo RepositoriesImplementation.cs incompleto

**Severidad**: MENOR  
**Archivo**: crud_service/Data/RepositoriesImplementation.cs  
**Lineas**: 202  
**Estado**: PENDIENTE

**Descripcion**:
El archivo no tiene la llave de cierre de la clase TicketHistoryRepository.

---

### [IMP-001] No hay validacion de transiciones de estado validas

**Severidad**: BAJA (mejora)  
**Archivo**: crud_service/Services/TicketService.cs  
**Estado**: PENDIENTE

**Descripcion**:
Se permite cambiar de cualquier estado a cualquier otro sin validar que la transicion sea logica. Por ejemplo:
- De Paid a Available (no deberia ser posible)
- De Cancelled a Reserved (no deberia ser posible)

**Solucion propuesta**:
Implementar una maquina de estados que valide transiciones permitidas.

---

## Correcciones Realizadas

### CRIT-001: Bulk Insert de Tickets

**Fecha**: 12 de febrero de 2026

**Por que era un problema**:
El codigo original usaba un patron generado por IA que hacia N llamadas individuales a la base de datos. Por ejemplo, crear 1000 tickets ejecutaba 1000 sentencias INSERT separadas:
- Cada INSERT abre y cierra una conexion (o usa una del pool)
- Cada INSERT es una transaccion separada
- Con 1000 tickets: ~1000 roundtrips a la BD
- Tiempo estimado: 5-10 segundos (vs <100ms con bulk)
- Riesgo de timeout en cantidades grandes
- Saturacion del connection pool

**Evidencia del patron IA**:
En ServiceExtensions.cs ya habia un comentario similar:
```csharp
// La IA sugirio crear DbContext como Transient (nueva instancia por request)
// eso era demasiado ineficiente porque iba a hacer una saturacion de conexiones
```

**La mejora implementada**:

1. Se agrego metodo `AddRangeAsync` a la interfaz ITicketRepository:
```csharp
Task<IEnumerable<Ticket>> AddRangeAsync(IEnumerable<Ticket> tickets);
```

2. Se implemento en TicketRepository usando EF Core AddRangeAsync:
```csharp
public async Task<IEnumerable<Ticket>> AddRangeAsync(IEnumerable<Ticket> tickets)
{
    var ticketList = tickets.ToList();
    await _context.Tickets.AddRangeAsync(ticketList);
    await SaveChangesAsync();
    return ticketList;
}
```

3. Se refactorizo CreateTicketsAsync para usar bulk insert:
```csharp
public async Task<IEnumerable<TicketDto>> CreateTicketsAsync(long eventId, int quantity)
{
    // Crear todos los tickets en memoria primero
    var tickets = Enumerable.Range(0, quantity)
        .Select(_ => new Ticket
        {
            EventId = eventId,
            Status = TicketStatus.Available
        })
        .ToList();

    // Insertar todos en una sola operacion de BD (bulk insert)
    var created = await _ticketRepository.AddRangeAsync(tickets);
    return created.Select(MapToDto);
}
```

**Por que la mejora es mejor**:
| Aspecto | Antes (N llamadas) | Despues (bulk) |
|---------|-------------------|----------------|
| Llamadas a BD | N | 1 |
| Transacciones | N | 1 |
| Tiempo (1000 tickets) | ~5-10 seg | <100ms |
| Conexiones usadas | N (secuencial) | 1 |
| Atomicidad | NO (fallas parciales) | SI (todo o nada) |

**Archivos modificados**:
- crud_service/Repositories/IRepositories.cs
- crud_service/Data/RepositoriesImplementation.cs
- crud_service/Services/TicketService.cs

---

### CRIT-002: Mapeo de ENUMs PostgreSQL

**Fecha**: 12 de febrero de 2026  
**Descubierto durante**: Pruebas de la correccion CRIT-001

**Por que era un problema**:
Al intentar crear tickets, PostgreSQL rechazaba el INSERT con error:
```
column "status" is of type ticket_status but expression is of type text
```

El schema de la BD (schema.sql) define tipos ENUM nativos:
```sql
CREATE TYPE ticket_status AS ENUM ('available', 'reserved', 'paid', 'released', 'cancelled');
CREATE TYPE payment_status AS ENUM ('pending', 'approved', 'failed', 'expired');
```

Pero Entity Framework enviaba texto plano porque usaba `.HasConversion<string>()`.

**Este bug existia en el codigo original** - no fue introducido por nuestra mejora del bulk insert. El servicio nunca pudo crear tickets correctamente.

**La correccion implementada**:

1. En ServiceExtensions.cs - Registrar ENUMs en el DataSource de Npgsql:
```csharp
var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);
dataSourceBuilder.MapEnum<TicketStatus>("ticket_status");
dataSourceBuilder.MapEnum<PaymentStatus>("payment_status");
var dataSource = dataSourceBuilder.Build();
```

2. En TicketingDbContext.cs - Declarar ENUMs en el modelo:
```csharp
modelBuilder.HasPostgresEnum<TicketStatus>("ticket_status");
modelBuilder.HasPostgresEnum<PaymentStatus>("payment_status");
```

3. Quitar `.HasConversion<string>()` de las propiedades Status.

**Archivos modificados**:
- crud_service/Extensions/ServiceExtensions.cs
- crud_service/Data/TicketingDbContext.cs

---

## Commits

(Se listaran los commits realizados)

---

## Notas del Proceso

### Archivos revisados:
- Program.cs
- Controllers/EventsController.cs
- Controllers/TicketsController.cs
- Services/EventService.cs
- Services/TicketService.cs
- Models/Entities/*.cs
- Models/DTOs/*.cs
- Repositories/IRepositories.cs
- Data/RepositoriesImplementation.cs
- Data/TicketingDbContext.cs
- Extensions/ServiceExtensions.cs

### Herramientas usadas:
- GitHub Copilot para revision de codigo
- Docker Compose para ambiente local
