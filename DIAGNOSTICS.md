# Diagnóstico y Soluciones - Producer Unhealthy

## Problemas Identificados

### 1. **Health Check Fallando** ❌
- **Problema**: La ruta del health check en `compose.yml` era incorrecta
- **Causa**: En Producer estaba `/api/tickets/health` pero el endpoint estaba en la raíz `/health`
- **Solución**: Actualizar ambos servicios para usar `/health` en el health check

### 2. **Valores Quemados (Hardcoded) en Código** ❌
- **Problema**: Credenciales de RabbitMQ y configuración BD estaban en `appsettings.json`
- **Ubicaciones**:
  - `producer/Producer/appsettings.json`: usuario="guest", password="guest"
  - `crud_service/appsettings.json`: no existía (se leía desde variables)

### 3. **Falta de Variables de Entorno** ❌
- **Problema**: El código no estaba leyendo variables de entorno correctamente
- **Causas**:
  - `Program.cs` no tenía configuración de `AddEnvironmentVariables()`
  - `appsettings.json` usaba valores quemados en lugar de placeholders

### 4. **Falta de curl en Dockerfiles** ❌
- **Problema**: Los health checks usan `curl` pero no estaba instalado en las imágenes
- **Error**: `curl: not found` cuando Docker intentaba verificar salud

### 5. **Environment Configuration Desorganizada** ❌
- **Problema**: Variables hardcodeadas en compose.yml sin centralización
- **Falta**: `appsettings.Development.json` para valores por defecto

## Soluciones Implementadas

### ✅ 1. Actualizar `compose.yml`
```yaml
# Producer healthcheck
healthcheck:
  test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
  
# CRUD healthcheck  
healthcheck:
  test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
```

**Cambios de variables de entorno:**
- Producer: Usar prefijo `RabbitMQ__` para binding automático
- CRUD: Usar `ConnectionStrings__DefaultConnection` en lugar de variablesindividuales

### ✅ 2. Refactorizar `appsettings.json` (sin valores hardcodeados)
**Producer:**
```json
{
  "RabbitMQ": {
    "Host": "${RabbitMQ__Host}",
    "Port": "${RabbitMQ__Port}",
    "Username": "${RabbitMQ__Username}",
    "Password": "${RabbitMQ__Password}"
  }
}
```

**CRUD:**
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "${ConnectionStrings__DefaultConnection}"
  }
}
```

### ✅ 3. Crear `appsettings.Development.json`
**Producer** - Valores por defecto para desarrollo local:
```json
{
  "RabbitMQ": {
    "Host": "rabbitmq",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest"
  }
}
```

**CRUD** - Valores por defecto para desarrollo local:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=postgres;Port=5432;Database=ticketing_db;Username=ticketing_user;Password=ticketing_password;"
  }
}
```

### ✅ 4. Actualizar `Program.cs` en ambos servicios
```csharp
// Cargar variables de entorno con prioridad:
builder.Configuration
    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", optional: true)
    .AddEnvironmentVariables();  // <- Variables de entorno sobrescriben JSON
```

### ✅ 5. Mejorar RabbitMQ Connection Logging
- Agregar logging detallado de conexión
- Better error handling con try-catch
- Configuración de timeouts: `RequestedConnectionTimeout`, `RequestedHeartbeat`

### ✅ 6. Instalar curl en Dockerfiles
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0

WORKDIR /app

# Instalar curl para healthcheck
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

COPY --from=build /app/publish .
EXPOSE 8080
```

## Flujo de Configuración (Final)

```
1. Docker compose levanta contenedor con variables de entorno
   └─ Variables: RabbitMQ__Host=rabbitmq, ConnectionStrings__DefaultConnection=...

2. Program.cs carga configuración en orden:
   a) appsettings.json (placeholders)
   b) appsettings.Development.json (valores por defecto)
   c) Variables de entorno (SOBRESCRIBEN json)

3. Resultado:
   - Desarrollo (contenedor con ASPNETCORE_ENVIRONMENT=Development): 
     Usa valores de appsettings.Development.json
   - Producción (variables de entorno):
     Usa variables de entorno del compose.yml
```

## Estado Final ✅

```bash
$ docker-compose ps
NAME           IMAGE                          STATUS                PORTS
ticketing_db   postgres:15-alpine             Up (healthy)         5432
ticketing_broker rabbitmq:3.12-...            Up (healthy)         5672, 15672
ticketing_crud ticketing_project_week0-crud   Up (healthy)         8002->8080
ticketing_producer ticketing_project_week0-producer Up (healthy)    8001->8080
```

## Beneficios

✅ **Seguridad**: No hay credenciales hardcodeadas en el repositorio
✅ **Flexibilidad**: Mismo código funciona en dev, staging y prod
✅ **Clarity**: Configuración centralizada en `.env`
✅ **Debugging**: Mejor logging de inicialización RabbitMQ
✅ **Reliability**: Health checks funcionales para orquestación

## Variables de Entorno (.env)

```dotenv
# Database
POSTGRES_USER=ticketing_user
POSTGRES_PASSWORD=ticketing_password
POSTGRES_DB=ticketing_db
POSTGRES_PORT=5432

# RabbitMQ
RABBITMQ_DEFAULT_USER=guest
RABBITMQ_DEFAULT_PASS=guest
RABBITMQ_AMQP_PORT=5672
RABBITMQ_MGMT_PORT=15672

# Services
PRODUCER_PORT=8001
CRUD_PORT=8002
```

## Próximos Pasos (Recomendados)

1. Crear `.env.production` con valores seguros
2. Actualizar documentación en `READM E.md` sobre variables de entorno
3. Considerar usar HashiCorp Vault o Azure Key Vault para secrets
4. Agregar secrets en CI/CD pipeline (GitHub Actions / Azure Pipelines)
