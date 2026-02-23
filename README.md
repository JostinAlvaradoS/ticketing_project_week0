# SpecKit Ticketing Platform - Microservices

Plataforma de venta de tickets construida con arquitectura de microservicios usando .NET 8.

## Arquitectura

Cada microservicio es independiente y ejecutable por separado, siguiendo arquitectura hexagonal:
- **Domain**: Entidades de negocio y lógica de dominio
- **Application**: Casos de uso, comandos, queries
- **Infrastructure**: Adaptadores a bases de datos y servicios externos
- **Api**: Puntos de entrada HTTP (Minimal APIs)

## Microservicios

### Identity Service
**Ubicación**: `services/identity/`  
**Puerto**: 5100  
**Propósito**: Autenticación y generación de tokens JWT

```bash
# Ejecutar desde la raíz del repositorio
cd services/identity
dotnet run --project src/Api/Identity.Api.csproj --urls "http://localhost:5100"

# O desde el directorio del servicio
cd services/identity/src/Api
dotnet run --urls "http://localhost:5100"

# Build de la solución completa
cd services/identity
dotnet build
```

**Endpoints**:
- `GET /health` - Health check
- `POST /token` - Generar JWT (desarrollo)

### Catalog Service (próximamente)
**Ubicación**: `services/catalog/`  
**Puerto**: 5101  
**Propósito**: Catálogo de eventos y asientos

### Inventory Service (próximamente)
**Ubicación**: `services/inventory/`  
**Puerto**: 5102  
**Propósito**: Gestión de inventario y reservas

### Ordering Service (próximamente)
**Ubicación**: `services/ordering/`  
**Puerto**: 5103  
**Propósito**: Carrito de compras y órdenes

### Payment Service (próximamente)
**Ubicación**: `services/payment/`  
**Puerto**: 5104  
**Propósito**: Procesamiento de pagos (simulado)

### Fulfillment Service (próximamente)
**Ubicación**: `services/fulfillment/`  
**Puerto**: 5105  
**Propósito**: Generación de tickets PDF+QR

### Notification Service (próximamente)
**Ubicación**: `services/notification/`  
**Puerto**: 5106  
**Propósito**: Envío de notificaciones por email

## Infraestructura

Los servicios compartidos (PostgreSQL, Redis, Kafka, Zookeeper) se ejecutan con Docker Compose:

```bash
cd infra
docker compose up -d
```

Ver [infra/README.md](infra/README.md) para más detalles sobre configuración de base de datos y servicios.

## Estructura de Proyectos

Cada microservicio tiene su propia solución (.sln):

```
services/
├── identity/
│   ├── Identity.sln          # Solución del microservicio
│   ├── README.md
│   └── src/
│       ├── Domain/           # Identity.Domain.csproj
│       ├── Application/      # Identity.Application.csproj
│       ├── Infrastructure/   # Identity.Infrastructure.csproj
│       └── Api/              # Identity.Api.csproj (entry point)
├── catalog/
│   └── Catalog.sln
├── inventory/
│   └── Inventory.sln
└── ...
```

## Desarrollo

### Requisitos
- .NET 8 SDK
- Docker y Docker Compose
- PostgreSQL (via Docker)
- Redis (via Docker)
- Kafka + Zookeeper (via Docker)

### Setup Inicial

1. Iniciar infraestructura:
```bash
cd infra
docker compose up -d
```

2. Verificar que los servicios estén saludables:
```bash
docker compose ps
```

3. Ejecutar un microservicio:
```bash
cd services/identity
dotnet run --project src/Api/Identity.Api.csproj
```

### Compilar todos los servicios

Cada microservicio se compila independientemente:

```bash
# Identity
cd services/identity && dotnet build

# Catalog (cuando esté disponible)
cd services/catalog && dotnet build

# etc.
```

### Testing

```bash
# Ejecutar tests de un microservicio
cd services/identity
dotnet test

# Ejecutar tests de integración (próximamente)
cd tests/Integration
dotnet test
```

## Documentación Adicional

- [Plan de Implementación](specs/001-ticketing-mvp/plan.md)
- [Especificación](specs/001-ticketing-mvp/spec.md)
- [Tareas](specs/001-ticketing-mvp/tasks.md)
- [Infraestructura](infra/README.md)
- [Identity Service](services/identity/README.md)

## Database Schemas

Cada microservicio tiene su propio schema en PostgreSQL:

| Schema | Microservicio | Propósito |
|--------|---------------|-----------|
| `bc_identity` | Identity | Usuarios y autenticación |
| `bc_catalog` | Catalog | Eventos y catálogo |
| `bc_inventory` | Inventory | Reservas e inventario |
| `bc_ordering` | Ordering | Órdenes y carrito |
| `bc_payment` | Payment | Transacciones de pago |
| `bc_fulfillment` | Fulfillment | Generación de tickets |
| `bc_notification` | Notification | Historial de notificaciones |

## Convenciones

- Cada microservicio tiene su propia solución (.sln)
- Los microservicios NO comparten código entre ellos (solo contratos vía eventos)
- Comunicación asíncrona vía Kafka
- Comunicación síncrona solo cuando sea estrictamente necesario
- Cada microservicio tiene su propio DbContext y schema
- Logging estructurado con Serilog
- Tracing distribuido con OpenTelemetry

## Estado del Proyecto

✅ **Fase 0 - Foundation**
- [x] T001: Docker Compose con Postgres, Redis, Kafka, Zookeeper
- [x] T002: Schemas de base de datos
- [x] T003: Scripts de inicialización
- [x] T004: README de operaciones
- [x] T005: Identity Service skeleton

🚧 **Fase 1 - Core Services** (En progreso)
- [ ] T006-T010: Completar Identity con DbContext
- [ ] T011-T022: Catalog e Inventory services
- [ ] T023-T029: Ordering service

⏳ **Fase 2 - Payment & Fulfillment** (Pendiente)

⏳ **Fase 3 - Polish & Hardening** (Pendiente)
