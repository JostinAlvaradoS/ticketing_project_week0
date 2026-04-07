---
title: Catalog Service
description: Gestión de eventos y mapas de asientos — el corazón de la oferta de la plataforma
---

# Catalog Service

## Propósito

El Catalog Service es el servicio de consulta central del sistema. Expone el catálogo de eventos disponibles, sus detalles y los mapas de asientos con estado en tiempo real. También provee los endpoints de administración para crear, editar y configurar eventos y sus asientos.

Es un servicio predominantemente de **lectura (read-heavy)**. Las escrituras ocurren por parte de administradores y son menos frecuentes.

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | .NET 9 — Minimal APIs |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL — schema `bc_catalog` |
| Mediator | MediatR (CQRS) |
| Mensajería | Apache Kafka (consumidor) |
| Puerto | `5001` (local), `50001` (Docker) |

---

## Estructura Interna

```
services/catalog/
├── Api/
│   └── Endpoints/
│       ├── EventEndpoints.cs         ← GET /events, GET /events/{id}, GET /events/{id}/seatmap
│       └── AdminEventEndpoints.cs    ← POST/PUT /admin/events, POST /admin/events/{id}/seats
├── Application/
│   ├── Queries/
│   │   ├── GetAllEventsQuery.cs
│   │   ├── GetAllEventsHandler.cs
│   │   ├── GetEventQuery.cs
│   │   ├── GetEventHandler.cs
│   │   ├── GetEventSeatmapQuery.cs
│   │   └── GetEventSeatmapHandler.cs
│   └── Commands/
│       ├── CreateEventCommand.cs
│       ├── UpdateEventCommand.cs
│       ├── GenerateSeatsCommand.cs
│       └── [handlers correspondientes]
├── Domain/
│   └── Entities/
│       ├── Event.cs                  ← id, name, description, venue, date, price, isActive
│       └── Seat.cs                   ← id, eventId, section, row, number, price, status
└── Infrastructure/
    ├── Persistence/
    │   ├── CatalogDbContext.cs
    │   ├── EventRepository.cs
    │   └── SeatRepository.cs
    └── Messaging/
        └── SeatsGeneratedConsumer.cs ← Consume: seats-generated
```

---

## Endpoints Públicos

### `GET /events`

Lista todos los eventos activos.

**Response 200:**
```json
[
  {
    "id": "uuid",
    "name": "Concierto Rock Fest 2026",
    "description": "El festival de rock más grande del año",
    "eventDate": "2026-06-15T20:00:00Z",
    "venue": "Estadio Nacional",
    "maxCapacity": 5000,
    "basePrice": 50.00,
    "totalSeats": 5000,
    "soldSeats": 1200,
    "revenue": 60000.00
  }
]
```

---

### `GET /events/{id}`

Detalle completo de un evento.

**Response 200:**
```json
{
  "id": "uuid",
  "name": "Concierto Rock Fest 2026",
  "description": "...",
  "eventDate": "2026-06-15T20:00:00Z",
  "venue": "Estadio Nacional",
  "maxCapacity": 5000,
  "basePrice": 50.00,
  "isActive": true
}
```

**Response 404:** Evento no encontrado

---

### `GET /events/{id}/seatmap`

Mapa completo de asientos del evento con estado actual.

**Response 200:**
```json
{
  "eventId": "uuid",
  "eventName": "Concierto Rock Fest 2026",
  "eventDate": "2026-06-15T20:00:00Z",
  "basePrice": 50.00,
  "seats": [
    {
      "id": "uuid",
      "section": "VIP",
      "row": "A",
      "number": 1,
      "price": 150.00,
      "status": "Available"
    },
    {
      "id": "uuid",
      "section": "General",
      "row": "B",
      "number": 5,
      "price": 50.00,
      "status": "Reserved"
    }
  ]
}
```

**Estados de asiento:** `Available`, `Reserved`, `Sold`

---

## Endpoints de Administración

Todos requieren `Authorization: Bearer <token>` con rol `Admin`.

### `POST /admin/events`

Crea un nuevo evento.

**Request:**
```json
{
  "name": "Festival de Jazz",
  "description": "Noche de jazz bajo las estrellas",
  "eventDate": "2026-08-20T19:00:00Z",
  "venue": "Plaza Mayor",
  "maxCapacity": 800,
  "basePrice": 30.00
}
```

---

### `PUT /admin/events/{id}`

Actualiza los datos de un evento existente.

---

### `POST /admin/events/{id}/seats`

Genera los asientos del evento por configuración de secciones.

**Request:**
```json
{
  "sections": [
    {
      "name": "VIP",
      "rows": 5,
      "seatsPerRow": 20,
      "priceMultiplier": 3.0
    },
    {
      "name": "General",
      "rows": 30,
      "seatsPerRow": 50,
      "priceMultiplier": 1.0
    }
  ]
}
```

Tras generar los asientos, publica el evento `seats-generated` en Kafka para que Inventory sincronice su copia.

---

### `POST /admin/events/{id}/deactivate`

Desactiva un evento (no aparece en listados públicos).

### `POST /admin/events/{id}/reactivate`

Reactiva un evento desactivado.

---

## Esquema de Base de Datos

**Schema:** `bc_catalog`

```sql
CREATE TABLE "Events" (
    "Id"          UUID PRIMARY KEY,
    "Name"        VARCHAR(255) NOT NULL,
    "Description" TEXT,
    "EventDate"   TIMESTAMP NOT NULL,
    "Venue"       VARCHAR(255),
    "MaxCapacity" INT NOT NULL,
    "BasePrice"   DECIMAL(10,2) NOT NULL,
    "IsActive"    BOOLEAN NOT NULL DEFAULT TRUE,
    "CreatedAt"   TIMESTAMP NOT NULL
);

CREATE TABLE "Seats" (
    "Id"        UUID PRIMARY KEY,
    "EventId"   UUID NOT NULL REFERENCES "Events"("Id"),
    "Section"   VARCHAR(100) NOT NULL,
    "Row"       VARCHAR(10) NOT NULL,
    "Number"    INT NOT NULL,
    "Price"     DECIMAL(10,2) NOT NULL,
    "Status"    VARCHAR(50) NOT NULL DEFAULT 'Available',
    "CreatedAt" TIMESTAMP NOT NULL
);
```

---

## Mensajería Kafka

### Consume: `seats-generated`

Cuando un administrador genera asientos desde el endpoint `/admin/events/{id}/seats`, Catalog publica `seats-generated` y el mismo Catalog (u otros servicios como Inventory) sincroniza el estado.

**Schema del evento:**
```json
{
  "eventId": "uuid",
  "totalSeatsGenerated": 1100,
  "sections": [
    { "name": "VIP", "seats": 100 },
    { "name": "General", "seats": 1000 }
  ],
  "createdAt": "2026-04-06T12:00:00Z"
}
```

---

## Notas de Diseño

- El estado de los asientos (`Available`, `Reserved`, `Sold`) en Catalog se actualiza mediante eventos de Kafka, no por llamadas HTTP directas de otros servicios
- La consulta del seatmap es la operación más frecuente del sistema — es usada por el frontend cada vez que un usuario abre la página de un evento
- El servicio no realiza llamadas a otros servicios (no tiene HTTP clients salientes)
