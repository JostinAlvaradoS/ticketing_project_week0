---
title: Arquitectura General del Sistema
description: Visión técnica completa de la plataforma SpecKit Ticketing — patrones, decisiones y flujos de comunicación
---

# Arquitectura General — SpecKit Ticketing Platform

## Contexto de Negocio

SpecKit Ticketing es una plataforma distribuida para la venta de boletos de eventos en vivo. Permite a los usuarios explorar eventos, reservar asientos, completar el pago y recibir sus boletos digitales, todo dentro de un flujo orquestado de forma asíncrona que garantiza consistencia sin sacrificar disponibilidad.

El sistema resuelve tres problemas clave del dominio de ticketing:

1. **Competencia por asientos**: múltiples usuarios intentando reservar el mismo lugar simultáneamente
2. **Flujos de larga duración**: la cadena reserva → pago → emisión puede tardar segundos o fallar en cualquier punto
3. **Consistencia eventual**: las partes del sistema (inventario, órdenes, pagos, boletos) deben mantenerse coherentes sin una transacción distribuida global

---

## Mapa de Servicios

| Servicio | Puerto | Responsabilidad |
|----------|--------|-----------------|
| **Identity** | 5000 | Autenticación, generación de JWT |
| **Catalog** | 5001 | Gestión y consulta de eventos y asientos |
| **Inventory** | 5002 | Reservas de asientos con TTL y locks distribuidos |
| **Ordering** | 5003 | Carrito de compras y estado de órdenes |
| **Payment** | 5004 | Procesamiento y simulación de pagos |
| **Fulfillment** | 5004* | Generación de boletos PDF con QR |
| **Notification** | 5005 | Envío de emails con boletos |
| **Waitlist** | 5006 | Lista de espera para eventos agotados |

> *Fulfillment y Payment comparten rango de puertos en compose por configuración de red interna. Ver `infra/docker-compose.yml`.

---

## Patrones Arquitectónicos

### Arquitectura Hexagonal (Ports & Adapters)

Cada microservicio está organizado en tres capas:

```
┌─────────────────────────────────────────────┐
│               API Layer (Adapters IN)       │  ← Controllers, Minimal API endpoints
├─────────────────────────────────────────────┤
│           Application Layer                 │  ← Commands, Queries, Handlers (MediatR)
├─────────────────────────────────────────────┤
│              Domain Layer                   │  ← Entidades, Value Objects, Reglas de negocio
├─────────────────────────────────────────────┤
│         Infrastructure Layer (Adapters OUT) │  ← EF Core, Kafka, Redis, HTTP Clients
└─────────────────────────────────────────────┘
```

La capa de dominio no tiene dependencias externas. La infraestructura implementa los puertos (interfaces) definidos en la capa de aplicación.

### CQRS (Command Query Responsibility Segregation)

Las operaciones de escritura y lectura están separadas semánticamente:

- **Commands**: `CreateReservationCommand`, `AddToCartCommand`, `ProcessPaymentCommand`, `GenerateTicketCommand`
- **Queries**: `GetAllEventsQuery`, `GetEventQuery`, `GetEventSeatmapQuery`
- Implementados con **MediatR** (pipeline pattern)

### Event-Driven Choreography

El sistema usa coreografía de eventos (no orquestación centralizada). Cada servicio publica eventos y otros los consumen de forma independiente. No existe un coordinador central que dirija el flujo.

**Ventajas en este contexto:**
- Desacoplamiento total entre servicios
- Cada servicio puede escalar y fallar de forma independiente
- El flujo completo es observable a través de los eventos

**Trade-off asumido:**
- La depuración de flujos completos es más compleja
- La consistencia es eventual, no inmediata

---

## Flujo Principal: Compra de un Boleto

```
Usuario selecciona asiento
        │
        ▼
[Inventory] Reserva asiento
  - Redis lock (previene race condition)
  - TTL 15 minutos
  - Publica: reservation-created
        │
        ▼ (Kafka: reservation-created)
[Ordering] Registra reserva en caché
  - Usuario agrega al carrito (AddToCart)
  - Usuario hace checkout → Orden en estado "pending"
        │
        ▼ (HTTP POST /payments)
[Payment] Procesa el pago
  - Valida orden y reserva (desde eventos en memoria)
  - Simula resultado (95% éxito)
  - Publica: payment-succeeded o payment-failed
        │
   ┌────┴─────┐
   ▼          ▼
[Kafka]   [Kafka]
succeeded  failed
   │          │
   ▼          ▼
[Fulfillment] [Ordering+Inventory]
Genera PDF    Cancela orden, libera asiento
Publica: ticket-issued
   │
   ▼ (Kafka: ticket-issued)
[Notification]
Envía email con boleto
```

---

## Comunicación Entre Servicios

### Síncrona (HTTP/REST)

| Origen | Destino | Propósito |
|--------|---------|-----------|
| Frontend | Catalog | Consultar eventos y mapas de asientos |
| Frontend | Inventory | Crear reserva |
| Frontend | Ordering | Agregar al carrito, checkout |
| Frontend | Payment | Procesar pago |
| Frontend | Identity | Obtener token JWT |
| Inventory | Waitlist | Verificar si hay pendientes en espera (ADR-03) |

### Asíncrona (Kafka)

| Topic | Productor | Consumidores |
|-------|-----------|--------------|
| `reservation-created` | Inventory | Ordering, Payment |
| `reservation-expired` | Inventory | Waitlist |
| `payment-succeeded` | Payment | Fulfillment, Inventory |
| `payment-failed` | Payment | Ordering, Inventory |
| `ticket-issued` | Fulfillment | Notification |
| `seats-generated` | Catalog | Inventory |

---

## Almacenamiento

### PostgreSQL — Multi-Schema

Una sola instancia de PostgreSQL con schemas separados por bounded context:

| Schema | Servicio | Entidades principales |
|--------|----------|----------------------|
| `bc_identity` | Identity | Users |
| `bc_catalog` | Catalog | Events, Seats |
| `bc_inventory` | Inventory | Seats, Reservations |
| `bc_ordering` | Ordering | Orders, OrderItems |
| `bc_payment` | Payment | Payments |
| `bc_fulfillment` | Fulfillment | Tickets |
| `bc_notification` | Notification | EmailNotifications |
| `bc_waitlist` | Waitlist | WaitlistEntries |

> Esta decisión simplifica el DevOps (una sola base) mientras mantiene el aislamiento lógico de bounded contexts. Los servicios no acceden al schema de otro servicio.

### Redis

- **Propósito**: Locks distribuidos para reservas de asientos
- **Patrón**: SET con flag NX (set-if-not-exists) + TTL
- **Servicio**: Exclusivamente Inventory
- **TTL del lock**: ~200ms (tiempo de la operación)

---

## Decisiones Arquitectónicas (ADRs)

### ADR-01: PostgreSQL Multi-Schema vs Multi-Database
**Decisión**: Una instancia, múltiples schemas
**Razón**: Simplicidad de DevOps en contexto de entrenamiento, los schemas proveen el aislamiento necesario
**Trade-off**: No se pueden hacer transacciones cross-schema (se acepta consistencia eventual)

### ADR-02: Coreografía vs Orquestación
**Decisión**: Coreografía de eventos via Kafka
**Razón**: Enseña patrones event-driven, no hay cuello de botella central, mejor para escalar
**Trade-off**: Flujos más difíciles de observar de un vistazo

### ADR-03: HTTP Síncrono de Inventory → Waitlist
**Decisión**: Inventory llama HTTP GET a Waitlist con timeout de 200ms durante expiración de reserva
**Razón**: Feedback rápido sobre si hay alguien en espera antes de liberar el asiento
**Constraint**: Timeout corto — si Waitlist no responde, se libera el asiento normalmente

### ADR-04: Redis Locks en Reserva
**Decisión**: Lock distribuido por asiento durante la operación de reserva
**Razón**: Previene que dos usuarios reserven el mismo asiento simultáneamente
**Patrón**: SET NX con TTL de la operación

### ADR-05: In-Memory Reservation Cache en Ordering
**Decisión**: Ordering mantiene un caché en memoria de eventos `reservation-created`
**Razón**: Permite validar reservas sin llamadas HTTP a Inventory en cada operación
**Trade-off**: Datos ligeramente desactualizados, mitigado por TTL corto de reservas

---

## Autenticación

- **Tipo**: JWT (JSON Web Tokens)
- **Issuer**: `SpecKit.Identity`
- **Audience**: `SpecKit.Services`
- **Roles**: `User`, `Admin`
- **Endpoints protegidos**: Administración del catálogo (crear/editar eventos, generar asientos)
- **Contexto de entrenamiento**: El token se emite sin validar credenciales (dev-only)

---

## Infraestructura de Despliegue

### Contenedores (Docker Compose)

```
Infraestructura base:
├── postgres:17          → ticketing DB (5432)
├── redis:7              → locks distribuidos (6379)
├── zookeeper            → coordinación Kafka (2181)
├── kafka:7.5.0          → mensajería async (9092)
└── kafka-init           → crea topics al inicio

Microservicios (.NET 9):
├── identity             (50000 → 5000)
├── catalog              (50001 → 5001)
├── inventory            (50002 → 5002)
├── ordering             (5003)
├── payment              (5004)
├── fulfillment          (50004 → 5004)
├── notification         (50005 → 5005)
└── waitlist             (5006)
```

### Secuencia de Inicio

1. PostgreSQL, Redis, Zookeeper
2. Kafka (espera Zookeeper)
3. kafka-init (crea topics)
4. Todos los microservicios (esperan kafka-init)
5. Migraciones EF Core se aplican automáticamente al iniciar cada servicio

---

## Contratos de Eventos

Los schemas JSON de todos los eventos Kafka están definidos en `contracts/kafka/`. Esto garantiza que productores y consumidores acuerden el formato de los mensajes sin acoplamiento de código.

Ver: [`contracts/kafka/`](../../contracts/kafka/) para los schemas completos.
