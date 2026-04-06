<!--
Sync Impact Report

- Version change: unspecified -> 1.1.0
- Modified principles:
	- Architecture: generic -> Arquitectura: Hexagonal (Ports & Adapters)
    - Tech stack base: .NET 9 (o superior), EF Core 9+ con Npgsql, Confluent.Kafka 2.x, MediatR, FluentValidation, Serilog + OpenTelemetry.
	- Database: generic -> Base de datos: UNA instancia PostgreSQL compartida (schemas por bounded context)
	- Communication: generic -> Comunicación síncrona (HTTP/REST, gRPC) y asíncrona (Kafka)
	- Transactions/Sagas: generic -> Priorizar transacciones locales; evitar sagas complejas al inicio
	- Deployment & Quality: generic -> Despliegue local con Docker Compose + Testcontainers para integración
- Added sections: Development Workflow (Constitution Check in PRs) and explicit Database & Security Constraints
- Removed sections: none
- Templates requiring updates: .specify/templates/plan-template.md (✅ updated), .specify/templates/spec-template.md (⚠ pending), .specify/templates/tasks-template.md (⚠ pending), .specify/templates/constitution-template.md (⚠ pending)
- Follow-up TODOs:
	- TODO(RATIFICATION_DATE): confirm original ratification date and replace placeholder
	- Update .specify/templates/spec-template.md and .specify/templates/tasks-template.md to reflect shared-Postgres schema rules and required CI checks
	- Create or review commands documentation in .specify/templates/commands/ for agent-neutral guidance (if present)
	- Document recommended RBAC and schema roles for the shared Postgres instance
-->

# Speckit Ticketing Constitution
**Version**: 1.1.0  
**Ratified**: 2026-02-22  
**Last Amended**: 2026-02-22  
**Sync Impact Report** (resumen de cambios desde versión anterior):
- Arquitectura: de genérica a Hexagonal obligatoria
- Base de datos: de genérica a una sola PostgreSQL compartida con schemas por bounded context
- Comunicación: explicitada como síncrona (HTTP/REST + gRPC opcional) y asíncrona (Kafka)
- Transacciones: priorizar locales; evitar sagas complejas al inicio
- Añadidos: Redis para locks/caché, Observability (OTel), workflow de desarrollo (PRs + CI gating)
- Templates afectados: plan-template.md (actualizado), spec-template.md y tasks-template.md (pendientes de actualización)

## Core Principles

### 1. Arquitectura: Hexagonal (Ports & Adapters)
Cada microservicio **MUST** seguir estrictamente el patrón Hexagonal (Ports & Adapters).  
- El dominio **MUST** permanecer puro y aislado de cualquier detalle de infraestructura (DB, mensajería, HTTP, etc.).  
- Todo acceso externo **MUST** realizarse a través de **puertos** (interfaces) e implementarse en **adaptadores**.  
**Rationale**: Garantiza testabilidad del dominio, independencia tecnológica y facilidad de evolución futura.

### 2. Base de datos: Una sola instancia PostgreSQL compartida
Se adopta una única instancia PostgreSQL (shared database pattern) para desarrollo, integración y producción.  
- Aislamiento lógico mediante **schemas** por bounded context (ej: `bc_catalog`, `bc_inventory`, `bc_ordering`, `bc_payment`, etc.).  
- Cada microservicio **MUST** acceder a su schema correspondiente mediante repositorios definidos como puertos (`IRepository`).  
- **NO** exponer SQL directo fuera del adaptador de persistencia.  
- En .NET: usar EF Core con un `DbContext` por microservicio (configurando el schema vía `SearchPath` o `.HasDefaultSchema()`).  
- Connection string única en `docker-compose.yml`.  
- **RECOMMENDED**: Definir roles y privilegios PostgreSQL por schema para mayor seguridad.  
**Rationale**: Simplifica operaciones (backups, monitoreo, migraciones), permite transacciones locales ACID y reduce complejidad en MVP.

### 3. Comunicación
- **Síncrona**: HTTP/REST (Minimal APIs) como principal; gRPC opcional para casos de baja latencia (ej: chequeos de inventario).  
- **Asíncrona**: Kafka como event bus principal para eventos no críticos, notificaciones y eventual consistency.  
**Reglas**:  
- Eventos **MUST** ser idempotentes cuando corresponda.  
- Preferir fallos aislados + reintentos + dead-letter queues.  
**Rationale**: Balance entre respuesta inmediata y desacoplamiento.

### 4. Transacciones y Orquestación (Sagas)
- **Priorizar** transacciones locales ACID dentro del mismo schema/microservicio siempre que sea posible.  
- **NO** introducir sagas complejas o coreografías distribuidas al inicio a menos que la especificación lo justifique explícitamente.  
**Rationale**: Minimizar complejidad técnica y operativa en fases iniciales.

### 5. Caché y Locks Distribuidos
- Redis **MUST** usarse para:  
  - Reservas temporales con TTL (default 15 minutos).  
  - Distributed locks en secciones críticas de alta concurrencia.  
- **NO** abusar de Redis como caché general; priorizar PostgreSQL para datos persistentes.  
**Rationale**: Garantiza manejo seguro de concurrencia en reservas sin bloquear la BD principal.

### 6. Observability
- Todos los servicios **MUST** emitir:  
  - Logs estructurados (JSON via Serilog).  
  - Traces y métricas con OpenTelemetry.  
- Incluir correlation IDs en requests y eventos Kafka.  
- Entorno local: incluir OTEL collector en `docker-compose.yml` (export a Jaeger/Zipkin o console).  
**Rationale**: Facilita debugging distribuido y monitoreo.

### 7. Despliegue Local (Entorno de Desarrollo/Integración)
- Entorno base reproducible con `docker-compose.yml` que incluya:  
  - 1 instancia PostgreSQL  
  - Redis  
  - Kafka + Zookeeper  
  - Servicios .NET  
**Rationale**: Simplicidad y reproducibilidad en desarrollo.

### 8. Calidad y Tests
- Unit tests **MUST** mockear puertos y casos de uso.  
- Integration tests **MUST** usar Testcontainers (con un solo contenedor PostgreSQL compartido).  
- Contract tests y pruebas asíncronas (Kafka) **RECOMMENDED**.  
**Rationale**: Balance entre velocidad y fiabilidad.

## Database & Security Constraints
- Schemas **MUST** usar prefijo consistente: `bc_<bounded_context>` (ej: `bc_inventory`).  
- Migraciones: Cada microservicio mantiene y aplica sus propias migraciones solo a su schema.  
- Seguridad: Limitar permisos de cada servicio a su schema; usar secrets via `.env` o User Secrets en desarrollo.

## Development Workflow
- Cada PR **MUST** incluir:  
  - Descripción del cambio.  
  - Impacto en schemas (si aplica).  
  - Tests unitarios.  
  - Sección "Constitution Check" confirmando cumplimiento.  
- CI pipeline **MUST** ejecutar: unit tests, integración ligera con Testcontainers, y validación de contratos (si existen).  
- Cambios en schemas o migraciones **REQUIEREN** al menos dos aprobaciones + plan de migración/despliegue.

## Governance
- Enmiendas: Cambios requieren propuesta documentada y revisión técnica.  
  - Cambios menores (clarificaciones): mayoría simple.  
  - Cambios materiales: consenso + plan de migración.  
- Actualizaciones: Ejecutar `/speckit.constitution` con el nuevo texto y commitear.  
- Versioning:  
  - **MAJOR**: Cambios incompatibles (ej: eliminar shared DB o hexagonal).  
  - **MINOR**: Nuevos principios o expansiones significativas.  
  - **PATCH**: Correcciones menores.

**Tech stack base recomendado**: .NET 9 (o superior), EF Core 9+ con Npgsql, Confluent.Kafka 2.x, MediatR, FluentValidation, Serilog + OpenTelemetry.