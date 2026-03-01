# AI Workflow — Registro del flujo de trabajo con IA

Fecha: 2026-02-26

Resumen
- Este documento registra el flujo de trabajo con IA usado para generar los artefactos del proyecto `001-ticketing-mvp` (Spec, Plan, Tasks) usando el framework Spec Kit / speckit.
- Contiene los prompts finales utilizados, referencias a los archivos generados y cómo se verificó cada paso.

Framework utilizado
- Speckit / Spec Kit (comandos utilizados: `/speckit.constitution`, `/speckit.specify`, `/speckit.plan`, `/speckit.tasks`, `/speckit.implement`).

Artefactos generados
- `specs/001-ticketing-mvp/spec.md` — Especificación del MVP.
- `specs/001-ticketing-mvp/plan.md` — Plan técnico accionable.
- `specs/001-ticketing-mvp/tasks.md` — Lista de tareas con checkboxes.

## 🛠️ Adaptaciones y Decisiones Críticas del Equipo

### Personalización de Speckit
Durante la fase de implementación, se tomaron decisiones de personalización sobre las herramientas de automatización de Spec Kit (scripts `.sh` de Speckit/GitHub):

- **Modificación de Scripts de Git:** Se editaron los archivos `.sh` (como `phase-1-smoke-test.sh` y otros scripts de automatización interna de speckit) para permitir convenciones de nomenclatura de ramas y estructuras de commits personalizadas.
- **Filosofía del Desarrollador (Jostin):** Se estableció que, aunque se utilicen frameworks de trabajo guiado por IA o metodologías como *Spec Driven Development*, el equipo debe **priorizar las convenciones internas del equipo de desarrollo** por encima de las restricciones rígidas del framework. 
- **Resultado:** El flujo de trabajo es ahora "Spec-Driven" pero adaptado al lenguaje y cultura del equipo, permitiendo commits automáticos y nombres de ramas que reflejen mejor el contexto del proyecto real más allá del boilerplate del framework.

### Actualización de Estrategia de Implementación (2026-02-28)
- **Migración a ATDD (Acceptance Test-Driven Development):** Ante las funcionalidades pendientes listadas en [tasks.md](specs/001-ticketing-mvp/tasks.md), se ha tomado la decisión estratégica de abordar su desarrollo bajo el enfoque **ATDD**.
- **Redefinición con la IA:** Se ha reconfigurado el flujo de trabajo con la IA para que las tareas de implementación restantes se generen y ejecuten basándose primero en las pruebas de aceptación. Esto garantiza que cada funcionalidad pendiente cumpla con los criterios de éxito definidos antes de dar por terminada la codificación.
- **Registro de Pruebas:** El progreso y los resultados de este enfoque se documentarán detalladamente en [TDD_report.md](TDD_report.md).

Referencias (archivos en el repo)
- [spec.md](specs/001-ticketing-mvp/spec.md)
- [plan.md](specs/001-ticketing-mvp/plan.md)
- [tasks.md](specs/001-ticketing-mvp/tasks.md)

Prompts finales usados (versión ratificada)

1) Constitución — comando: `/speckit.constitution`

Prompt final (v1.1.0):

```
Este proyecto es un sistema distribuido de venta de boletos para eventos usando microservicios .NET con arquitectura hexagonal.

Principios obligatorios:
- Arquitectura: Hexagonal (Ports & Adapters) estricta en cada microservicio. Domain puro, sin dependencias de infraestructura.
- Base de datos: UNA instancia PostgreSQL compartida con schemas por bounded context (bc_identity, bc_catalog, bc_inventory, etc.).
- Comunicación: Sincrónica HTTP/REST (Minimal APIs) + gRPC opcional; asíncrona con Kafka para eventos y eventual consistency.
- Transacciones: Priorizar locales ACID; evitar sagas complejas al inicio.
- Redis: Obligatorio para locks distribuidos y TTL de reservas temporales.
- Observability: Logs estructurados (Serilog), traces y métricas con OpenTelemetry.
- Despliegue local: Docker Compose con 1 postgres + redis + kafka + zookeeper + servicios .NET.
- Calidad: Unit tests con mocks de puertos; integration con Testcontainers (single Postgres).
- Tech stack base: .NET 9+, EF Core + Npgsql, Confluent.Kafka, MediatR, FluentValidation, Serilog + OTEL.
- Configuración: Uso de valores por defecto en `appsettings.json` y `docker-compose.yml`, omitiendo `.env` para facilitar el intercambio de código entre pares (Contexto Training).
- Seguridad: JWT desde Identity Service, rate limiting, secrets via .env/User Secrets (Omitido intencionalmente según decisión de equipo).
```

2) Especificación — comando: `/speckit.specify`

Prompt final (enfocado en MVP + flujo P1):

```
Construye la especificación completa para un sistema distribuido de venta de boletos para eventos (ticketing platform) con microservicios .NET, arquitectura hexagonal y una sola PostgreSQL compartida (schemas por bounded context), siguiendo estrictamente la constitution.md v1.1.0 ratificada.

Enfócate en hacer un MVP viable con el flujo principal de compra de boletos como prioridad absoluta.

Estructura la especificación siguiendo el template estándar de Spec Kit:
- Overview
- User Stories (priorizadas P1-P3, cada una independiente y testable; usa formato "As a ... I want ... so that ...")
- Acceptance Scenarios (Given-When-Then para cada story)
- Edge Cases
- Functional Requirements (FR-001, FR-002...)
- Key Entities (con atributos principales, sin implementación)
- Success Criteria (medibles)
- Non-Functional Requirements (NFR-...)
- Key Flows (textuales, paso a paso para el flujo principal)
- Assumptions & Clarifications Needed
- Contracts & Artifacts
- Acceptance Test Plan (high level)
- Next Steps

Dominios / Microservicios principales: Identity/Auth, Catalog, Inventory (reservas con Redis TTL), Ordering/Cart, Payment (simulado), Fulfillment (PDF/QR), Notification.

Requisitos clave para MVP:
- Flujo P1: Seleccionar asiento → reservar temporal (15 min) → agregar a carrito → pago simulado → generar boleto + QR → notificación email.
- Concurrencia: optimistic locking en Postgres + Redis locks.
- Estados básicos: Seat (available/reserved/sold), Reservation (active/expired/cancelled), Order (draft/pending/paid/fulfilled/cancelled).
- Kafka para eventos: reservation-created, payment-succeeded, payment-failed, ticket-issued, reservation-expired.

Genera un archivo spec.md claro, conciso y accionable, listo para pasar a /speckit.plan.
```

3) Plan técnico — comando: `/speckit.plan`

Prompt final:

```
Genera el plan técnico completo para el MVP de la Ticketing Platform, basado estrictamente en:
- constitution.md v1.1.0 (hexagonal estricta, shared PostgreSQL con schemas bc_*, Kafka asíncrono, Redis locks, Docker Compose simple, transacciones locales primero).
- spec.md en specs/001-ticketing-mvp/spec.md (MVP enfocado en flujo P1: reserva → carrito → pago simulado → boleto QR + notificación).

Estructura el plan siguiendo el template estándar de Spec Kit:
- Tech Stack Detallado
- Estructura de Proyecto y Carpetas Hexagonal por Microservicio
- PostgreSQL Schemas y Configuración
- Redis Usage
- Kafka Topics y Schemas Iniciales
- Docker Compose Outline
- Concurrency & Locking Strategy
- Domain Events y Choreography
- Observability Setup
- Security Baseline
- Priorización de Fases y Tareas Iniciales
- Risks & Mitigations

Mantén el plan accionable, realista para MVP y alineado con simplicidad (simulado payment, no Stripe aún; PDFs generados y referenciados en DB).
Genera un archivo plan.md claro y listo para pasar a /speckit.tasks.
```

4) Tareas (tasks.md) — comando: `/speckit.tasks`

Prompt final:

```
Genera la lista de tareas accionables para implementar el MVP según el plan.md en specs/001-ticketing-mvp/plan.md y spec.md.

- Usa formato Markdown con checkboxes: - [ ] T### Descripción (Prioridad Px, Est: Yh) [Dependencias: Txxx]
- Agrupa por fases del plan: Phase 0 (Foundation), Phase 1 (Core), Phase 2 (Payment/Fulfillment), Phase 3 (Polish)
- Prioriza infra y foundational tasks primero (docker-compose, schemas, migrations, Identity skeleton)
- Incluye tareas de tests (unit + integration con Testcontainers)
- Marca dependencias
- Incluye tareas para contratos (OpenAPI + Kafka schemas iniciales)
- Mantén tareas pequeñas y secuenciales

Genera tasks.md listo para marcar progreso [x] a medida que implementemos con /speckit.implement.
```

5) Implementación (comando: `/speckit.implement "T### – descripción"`)

Prompt base (plantilla usada para cada implementación):

```
Implementa la tarea TXXX del tasks.md: "[pega descripción completa de la tarea]"

- Sigue estrictamente constitution.md v1.1.0 y plan.md (arquitectura hexagonal, schemas bc_*, etc.).
- Crea los archivos necesarios en la estructura correcta (services/<service>/src/Domain, Application, Infrastructure, Api).
- Incluye unit tests básicos si aplica.
- Usa .NET 9+ y paquetes recomendados (EF Core, MediatR, etc.).
- Genera código completo y funcional para esta tarea específica.
- Al final, muestra los archivos creados y cómo probarlos (ej: dotnet run, curl, etc.).
```

6) Nuevas Funcionalidades (Admin CRUD) con TDD — comando: `/speckit.tdd_feature`

Prompt final (v1.0.0):

```
Re-elabora el plan de tareas para integrar una fase de CRUD Administración (Eventos y Asientos) en el servicio Catalog, utilizando un enfoque estrictamente TDD (Test-Driven Development) y lenguaje Gherkin.

Requerimientos para la fase:
- Especificación: Crear un archivo .feature con escenarios Gherkin (Given-When-Then) para el flujo de Admin.
- Ciclo TDD: Red-Green-Refactor como base de implementación.
- Prioridad: Event creation, bulk seat generation, soft-delete.
- Pruebas: Unit tests para dominios (Event, Seat) y tests de integración para casos de uso (Mediatr Handlers) usando Testcontainers.

Tareas a generar:
- Definición de Gherkin (.feature).
- Implementación de Unit Tests de Dominio antes que la lógica.
- Implementación de Command Handlers guiados por Unit Tests.
- Setup de Integration Tests con Testcontainers para validar la persistencia en el schema bc_catalog.
- Implementación de API Endpoints con validación de roles (Admin).
```

Verificación y evidencia de avance
- Cada artefacto generado (`spec.md`, `plan.md`, `tasks.md`) se diseñó para ser testable y transferible a la fase de implementación.
- La verificación se planificó contra los Acceptance Scenarios en `spec.md` y contra las tareas en `tasks.md` (checkboxes).
- Para implementaciones se requiere: unit tests + integration tests con Testcontainers; los eventos críticos se validan con topics Kafka y con DB state en schemas `bc_*`.

Notas y decisiones clave
- Se siguió la `constitution` v1.1.0 como single source of truth para decisiones arquitectónicas.
- Prioridad absoluta: flujo P1 (reserva → pago simulado → ticket + QR).
- Pago simulado para acelerar MVP; integración real con proveedores queda para fases posteriores.

Próximos pasos sugeridos
- Ejecutar `/speckit.implement` para las tareas prioritarias en `tasks.md` (ej. T001 docker-compose, T002 schemas/migrations, T010 Identity skeleton).
- Añadir evidencias de implementación (links a commits/PRs y resultados de tests) en este documento conforme avance el desarrollo.

Registro de cambios
- 2026-02-26: Creación inicial de `AI_WORKFLOW.MD` con prompts y referencias.
