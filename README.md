# TicketRush MVP

Sistema de ticketing para eventos basado en microservicios con arquitectura orientada a eventos.

## Descripcion del Proyecto

TicketRush es un MVP que permite a usuarios reservar y comprar tickets para eventos. El sistema implementa un flujo asincrono donde las reservas se procesan mediante colas de mensajes, permitiendo escalabilidad y desacoplamiento entre componentes.

## Arquitectura

```
┌─────────────┐     ┌─────────────┐     ┌─────────────────────┐
│   Frontend  │────▶│ Producer API│────▶│      RabbitMQ       │
└─────────────┘     └─────────────┘     │  (Exchange: tickets)│
                                        └──────────┬──────────┘
                                                   │
                    ┌──────────────────────────────┼──────────────────────────────┐
                    │                              │                              │
                    ▼                              ▼                              ▼
        ┌───────────────────┐        ┌───────────────────┐        ┌───────────────────┐
        │ Consumer Service 1│        │ Consumer Service 2│        │    (Expirados)    │
        │   (Reservations)  │        │ (Payments & TTL)  │        │                   │
        └─────────┬─────────┘        └─────────┬─────────┘        └─────────┬─────────┘
                  │                            │                            │
                  └────────────────────────────┼────────────────────────────┘
                                               ▼
                                        ┌─────────────┐
                                        │  PostgreSQL │
                                        └─────────────┘
```

## Stack Tecnologico

| Componente | Tecnologia |
|------------|------------|
| Backend | .NET 8 (LTS) |
| Base de Datos | PostgreSQL 15 |
| Message Broker | RabbitMQ 3.12 |
| Contenedores | Docker & Docker Compose |

## Microservicios

| Servicio | Responsable | Descripcion |
|----------|-------------|-------------|
| Producer API | Jostin | Recibe peticiones HTTP y publica eventos a RabbitMQ |
| Consumer Service 1 (Reservations) | Jorge | Procesa reservas de tickets |
| Consumer Service 2 (Payments & TTL) | Guillermo | Procesa pagos y expiracion de reservas |

## Eventos RabbitMQ

| Evento | Descripcion |
|--------|-------------|
| `ticket.reserved` | Ticket reservado exitosamente |
| `ticket.payments.approved` | Pago aprobado |
| `ticket.payments.rejected` | Pago rechazado |
| `ticket.expired` | Reserva expirada por timeout |

---

## Alcance del MVP

### Incluido

- Reserva de tickets individuales
- Bloqueo temporal de tickets (5 minutos)
- Procesamiento de pagos (simulado)
- Liberacion automatica por timeout
- Comunicacion asincrona via RabbitMQ
- Optimistic locking para control de concurrencia basico

### Fuera de Alcance (Limitaciones Conocidas)

> Estas limitaciones son decisiones conscientes para el MVP, no bugs.

| Limitacion | Descripcion | Solucion en Produccion |
|------------|-------------|------------------------|
| **Race condition pago/timeout** | Si un pago esta en proceso y el timeout expira, otro usuario podria reservar el mismo ticket. El primer pago podria completarse sin entregar el ticket. | Distributed locks, two-phase commit, o ventana de gracia antes de liberar |
| **Pagos parciales** | No se soportan pagos parciales o en cuotas | Integrar con pasarela que soporte pagos parciales |
| **Reintentos automaticos** | Si RabbitMQ falla, no hay reintentos automaticos | Implementar retry policies con exponential backoff |
| **Idempotencia** | Los mensajes podrian procesarse mas de una vez si hay fallas | Agregar idempotency keys y deduplicacion |
| **Reservas multiples** | Un usuario no puede reservar multiples tickets en una sola transaccion | Implementar carrito de compras con reserva atomica |
| **Alta disponibilidad** | No hay redundancia en los servicios | Kubernetes, replicas, health checks |
| **Monitoreo** | No hay observabilidad implementada | Prometheus, Grafana, distributed tracing |

---

## Ejecucion Local

### Prerrequisitos

- Docker y Docker Compose
- .NET 8 SDK
- Git

### Pasos

```bash
# 1. Clonar el repositorio
git clone https://github.com/JostinAlvaradoS/ticketing_project_week0.git
cd ticketing_project_week0

# 2. Copiar variables de entorno
cp .env.example .env

# 3. Levantar infraestructura (PostgreSQL + RabbitMQ)
docker-compose up -d

# 4. Ejecutar migraciones (si aplica)
# [Instrucciones especificas por microservicio]

# 5. Ejecutar microservicios
cd ReservationService
dotnet run --project src/ReservationService.Worker
```

### URLs Locales

| Servicio | URL |
|----------|-----|
| RabbitMQ Management | http://localhost:15672 (guest/guest) |
| PostgreSQL | localhost:5432 |

---

## Lo que la IA hizo mal

> Esta seccion documenta casos donde la IA sugirio soluciones que funcionaban pero eran malas practicas. El equipo las identifico y corrigio.

| Fecha | Situacion | Sugerencia de la IA | Correccion del Equipo |
|-------|-----------|---------------------|----------------------|
| | | | |

---

## Equipo

| Nombre | Rol | Herramienta IA |
|--------|-----|----------------|
| Jostin | Developer (Producer API, QA) | |
| Jorge | Developer (Consumer Reservations) | Claude Code |
| Guillermo | Developer (Consumer Payments) | |

---

## Documentacion Adicional

- [AI_WORKFLOW.md](./AI_WORKFLOW.md) - Metodologia de trabajo con IA
- [ReservationService/README.md](./ReservationService/README.md) - Documentacion del Consumer de Reservas
