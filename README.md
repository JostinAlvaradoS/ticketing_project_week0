# Ticketing Platform — Quickstart y arquitectura

[AI Workflow](AI_WORKFLOW.MD) | [Readme](README.md) | [Human Checks](humanchcks.md) | [Dept Report](deptReport.md) | [TDD Report](TDD_report.md)


Plataforma de venta de boletos (MVP) compuesta por microservicios .NET (arquitectura hexagonal). Este README explica cómo levantar la pila con Docker Compose, el flujo de comunicación y enlaces útiles.

## Quickstart (Docker Compose)

1. Desde la raíz del repo, inicia la infraestructura (Postgres, Redis, Kafka, Zookeeper):

```bash
cd infra
docker compose up -d
```

2. Verifica servicios:

```bash
docker compose ps
```

3. Ejecuta servicios durante desarrollo (ej. Identity):

```bash
cd services/identity
dotnet run --project src/Api/Identity.Api.csproj --urls "http://localhost:5100"
```

Notas:
- Las variables de conexión se exponen como `POSTGRES_URL`, `REDIS_URL`, `KAFKA_BOOTSTRAP_SERVERS` en `infra/docker-compose.yml`.
- Para pruebas de integración usamos Testcontainers y una sola instancia de Postgres con schemas por bounded context (`bc_*`).

## Arquitectura y flujo de comunicación

- Arquitectura: Hexagonal (Ports & Adapters) por microservicio.
- Base de datos: UNA instancia PostgreSQL compartida, schemas por bounded context.
- Comunicación síncrona: HTTP/REST (Minimal APIs) para consultas y acciones inmediatas.
- Comunicación asíncrona: Kafka para eventos (reservation-created, payment-succeeded, payment-failed, ticket-issued, reservation-expired).
- Redis: Locks distribuidos y TTL para reservas temporales.

Diagrama (Mermaid):

```mermaid
graph LR
	Browser[Cliente (Browser)] -->|HTTP| Frontend[Frontend]
	Frontend -->|HTTP| ApiGateway[API / Services]
	ApiGateway -->|HTTP| Catalog[Catalog Service]
	ApiGateway -->|HTTP| Inventory[Inventory Service]
	ApiGateway -->|HTTP| Ordering[Ordering Service]
	ApiGateway -->|HTTP| Payment[Payment Service]
	ApiGateway -->|HTTP| Identity[Identity Service]
	Inventory -.->|Redis locks| Redis[Redis]
	Inventory -->|Postgres (bc_inventory)| Postgres[(Postgres)]
	Catalog -->|Postgres (bc_catalog)| Postgres
	Ordering -->|Postgres (bc_ordering)| Postgres
	Payment -->|Postgres (bc_payment)| Postgres
	Fulfillment -->|Postgres (bc_fulfillment)| Postgres
	Inventory -->|Kafka:event reservation-created| Kafka[(Kafka)]
	Payment -->|Kafka:event payment-succeeded/failed| Kafka
	Kafka -->|Consumer: ticket issuance| Fulfillment
```

## Qué debe contener este README (resumen)

- Breve descripción del proyecto
- Quickstart con Docker Compose y comandos básicos
- Diagrama de arquitectura / comunicación (Mermaid)
- Enlaces a artefactos principales y a `AI_WORKFLOW.MD`
- Cómo ejecutar tests básicos

## Enlaces útiles
- Especificación y plan: [specs/001-ticketing-mvp/spec.md](specs/001-ticketing-mvp/spec.md)
- Plan técnico: [specs/001-ticketing-mvp/plan.md](specs/001-ticketing-mvp/plan.md)
- Tareas: [specs/001-ticketing-mvp/tasks.md](specs/001-ticketing-mvp/tasks.md)
- Registro del flujo con IA: [AI_WORKFLOW.MD](AI_WORKFLOW.MD)
- Infra: [infra/README.md](infra/README.md)

## Tests

Ejecutar tests de un servicio:

```bash
cd services/identity
dotnet test
```

Pruebas de integración (cuando estén disponibles) usan Testcontainers y la misma imagen de Postgres levantada por `infra`.

## Estado y próximos pasos

- Ver [specs/001-ticketing-mvp/tasks.md](specs/001-ticketing-mvp/tasks.md) para tareas priorizadas y progreso.

