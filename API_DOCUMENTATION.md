# 📚 API Documentation - Ticketing System v1.0

## Introducción

Este documento describe todos los endpoints disponibles para el frontend del sistema de ticketing. La API está dividida en dos servicios:
- **CRUD Service** (http://localhost:8002): Gestión de eventos, tickets y pagos
- **Producer Service** (http://localhost:8001): Publicación de eventos de reserva

---

## 📋 Tabla de Contenidos

1. [Authentication](#authentication)
2. [Response Format](#response-format)
3. [Error Handling](#error-handling)
4. [CRUD Service](#crud-service)
   - [Events](#events)
   - [Tickets](#tickets)
5. [Producer Service](#producer-service)
6. [Data Models](#data-models)
7. [Status Codes & Errors](#status-codes--errors)
8. [Examples](#examples)

---

## 🔐 Authentication

**Nota:** En esta versión no hay autenticación implementada. Todos los endpoints son públicos.

Para futuras versiones, se recomienda agregar:
- JWT Bearer Token
- API Key
- OAuth 2.0

---

## 📤 Response Format

Todas las respuestas son en formato JSON.

### Respuesta Exitosa
```json
{
  "id": 1,
  "name": "Evento",
  "startsAt": "2026-03-15T20:00:00Z"
}
```

### Respuesta con Array
```json
[
  { "id": 1, "name": "Evento 1" },
  { "id": 2, "name": "Evento 2" }
]
```

### Respuesta de Error
```json
"Mensaje descriptivo del error"
```

---

## ❌ Error Handling

### Códigos de Error Comunes

| Code | Meaning | Cause |
|------|---------|-------|
| 400 | Bad Request | Datos inválidos en el request |
| 404 | Not Found | Recurso no existe |
| 409 | Conflict | Versión de ticket no coincide |
| 500 | Server Error | Error interno del servidor |

### Estrategia de Reintentos

Para el frontend:
- **Retry en 500**: Esperar 2s, reintentar hasta 3 veces
- **No retry en 400**: Error del cliente, mostrar mensaje
- **No retry en 404**: Recurso no existe

---

## 🏗️ CRUD Service

**Base URL:** `http://localhost:8002`

### Health Check

```http
GET /health
```

**Response (200):**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-10T23:45:30.1234567Z"
}
```

---

## 📌 EVENTS

### 1. Get All Events

```http
GET /api/events
```

**Response (200):**
```json
[
  {
    "id": 1,
    "name": "Concierto de Rock 2026",
    "startsAt": "2026-03-15T20:00:00Z",
    "availableTickets": 5,
    "reservedTickets": 2,
    "paidTickets": 1
  },
  {
    "id": 2,
    "name": "Festival de Música Electrónica",
    "startsAt": "2026-04-10T18:00:00Z",
    "availableTickets": 3,
    "reservedTickets": 0,
    "paidTickets": 0
  }
]
```

**Frontend Usage:**
```javascript
// Obtener lista de eventos
const response = await fetch('http://localhost:8002/api/events');
const events = await response.json();
console.log(events); // Array de eventos
```

---

### 2. Get Event by ID

```http
GET /api/events/{id}
```

**Parameters:**
- `id` (path, required): Event ID

**Response (200):**
```json
{
  "id": 1,
  "name": "Concierto de Rock 2026",
  "startsAt": "2026-03-15T20:00:00Z",
  "availableTickets": 5,
  "reservedTickets": 2,
  "paidTickets": 1
}
```

**Error (404):**
```
"Evento 999 no encontrado"
```

**Frontend Usage:**
```javascript
const eventId = 1;
const response = await fetch(`http://localhost:8002/api/events/${eventId}`);
if (response.ok) {
  const event = await response.json();
  console.log(event);
} else if (response.status === 404) {
  console.error("Evento no encontrado");
}
```

---

### 3. Create Event

```http
POST /api/events
Content-Type: application/json
```

**Request Body:**
```json
{
  "name": "Nuevo Concierto",
  "startsAt": "2026-06-01T20:00:00Z"
}
```

**Validations:**
- `name`: Required, no empty strings
- `startsAt`: Required, valid ISO 8601 datetime

**Response (201):**
```json
{
  "id": 3,
  "name": "Nuevo Concierto",
  "startsAt": "2026-06-01T20:00:00Z",
  "availableTickets": 0,
  "reservedTickets": 0,
  "paidTickets": 0
}
```

**Error (400):**
```json
"El nombre del evento es requerido"
```

**Frontend Usage:**
```javascript
const newEvent = {
  name: "Mi Evento",
  startsAt: new Date('2026-06-01T20:00:00Z').toISOString()
};

const response = await fetch('http://localhost:8002/api/events', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(newEvent)
});

if (response.status === 201) {
  const created = await response.json();
  console.log('Evento creado:', created.id);
}
```

---

### 4. Update Event

```http
PUT /api/events/{id}
Content-Type: application/json
```

**Parameters:**
- `id` (path, required): Event ID

**Request Body:**
```json
{
  "name": "Nombre Actualizado",
  "startsAt": "2026-06-15T20:00:00Z"
}
```

**Validations:**
- `name`: Optional, if provided cannot be empty
- `startsAt`: Optional, if provided must be valid datetime

**Response (200):**
```json
{
  "id": 1,
  "name": "Nombre Actualizado",
  "startsAt": "2026-06-15T20:00:00Z",
  "availableTickets": 5,
  "reservedTickets": 2,
  "paidTickets": 1
}
```

**Error (404):**
```
"Evento 999 no encontrado"
```

**Frontend Usage:**
```javascript
const updates = {
  name: "Nombre Nuevo",
  startsAt: new Date().toISOString()
};

const response = await fetch('http://localhost:8002/api/events/1', {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(updates)
});

const updated = await response.json();
```

---

### 5. Delete Event

```http
DELETE /api/events/{id}
```

**Parameters:**
- `id` (path, required): Event ID

**Response (204):** No content

**Error (404):**
```
"Evento 999 no encontrado"
```

**Frontend Usage:**
```javascript
const response = await fetch('http://localhost:8002/api/events/1', {
  method: 'DELETE'
});

if (response.status === 204) {
  console.log("Evento eliminado");
}
```

**⚠️ IMPORTANTE:** Eliminar un evento también elimina todos sus tickets (CASCADE delete)

---

## 🎫 TICKETS

### 1. Get Tickets by Event

```http
GET /api/tickets/event/{eventId}
```

**Parameters:**
- `eventId` (path, required): Event ID

**Response (200):**
```json
[
  {
    "id": 1,
    "eventId": 1,
    "status": "available",
    "reservedAt": null,
    "expiresAt": null,
    "paidAt": null,
    "orderId": null,
    "reservedBy": null,
    "version": 0
  },
  {
    "id": 2,
    "eventId": 1,
    "status": "reserved",
    "reservedAt": "2026-02-10T23:45:00Z",
    "expiresAt": "2026-02-10T23:50:00Z",
    "paidAt": null,
    "orderId": "ORD-2026-001",
    "reservedBy": "usuario@example.com",
    "version": 1
  }
]
```

**Frontend Usage:**
```javascript
const eventId = 1;
const response = await fetch(`http://localhost:8002/api/tickets/event/${eventId}`);
const tickets = await response.json();
// Filtrar por estado
const available = tickets.filter(t => t.status === 'available');
const reserved = tickets.filter(t => t.status === 'reserved');
```

---

### 2. Get Ticket by ID

```http
GET /api/tickets/{id}
```

**Parameters:**
- `id` (path, required): Ticket ID

**Response (200):**
```json
{
  "id": 1,
  "eventId": 1,
  "status": "available",
  "reservedAt": null,
  "expiresAt": null,
  "paidAt": null,
  "orderId": null,
  "reservedBy": null,
  "version": 0
}
```

---

### 3. Create Tickets in Bulk

```http
POST /api/tickets/bulk
Content-Type: application/json
```

**Request Body:**
```json
{
  "eventId": 1,
  "quantity": 10
}
```

**Validations:**
- `eventId`: Required, must be > 0
- `quantity`: Required, must be between 1 and 1000

**Response (201):**
```json
{
  "createdCount": 10,
  "tickets": [
    {
      "id": 1,
      "eventId": 1,
      "status": "available",
      "reservedAt": null,
      "expiresAt": null,
      "paidAt": null,
      "orderId": null,
      "reservedBy": null,
      "version": 0
    },
    // ... 9 more tickets
  ]
}
```

**Error (400):**
```
"EventId debe ser mayor a 0"
```

**Error (404):**
```
"Evento no encontrado"
```

**Frontend Usage:**
```javascript
const response = await fetch('http://localhost:8002/api/tickets/bulk', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    eventId: 1,
    quantity: 100
  })
});

const result = await response.json();
console.log(`Creados ${result.createdCount} tickets`);
```

---

### 4. Update Ticket Status

```http
PUT /api/tickets/{id}
Content-Type: application/json
```

**Parameters:**
- `id` (path, required): Ticket ID
- `expectedVersion` (query, optional): For optimistic concurrency control

**Request Body:**
```json
{
  "newStatus": "released",
  "reason": "Usuario canceló la reserva"
}
```

**Validations:**
- `newStatus`: Required, must be one of: available, reserved, paid, released, cancelled
- `reason`: Optional, context for the change

**Valid Status Transitions:**
```
available → reserved, cancelled
reserved  → paid, released, cancelled
paid      → released, cancelled
released  → (final state)
cancelled → (final state)
```

**Response (200):**
```json
{
  "id": 1,
  "eventId": 1,
  "status": "released",
  "reservedAt": "2026-02-10T23:45:00Z",
  "expiresAt": "2026-02-10T23:50:00Z",
  "paidAt": null,
  "orderId": "ORD-2026-001",
  "reservedBy": "usuario@example.com",
  "version": 1
}
```

**Error (404):**
```
"Ticket no encontrado"
```

**Error (409):** Optimistic Concurrency Conflict
```
"Conflicto de versión. El ticket fue modificado por otro usuario."
```

**Frontend Usage:**
```javascript
const response = await fetch('http://localhost:8002/api/tickets/1', {
  method: 'PUT',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    newStatus: 'released',
    reason: 'Usuario solicitó cancelación'
  })
});

if (response.status === 200) {
  const updated = await response.json();
  console.log('Status actualizado a:', updated.status);
  console.log('Nueva versión:', updated.version);
} else if (response.status === 409) {
  console.error("El ticket fue modificado. Recarga los datos.");
}
```

---

## 🚀 PRODUCER SERVICE

**Base URL:** `http://localhost:8001`

### Health Check

```http
GET /health
```

**Response (200):**
```json
{
  "status": "healthy",
  "timestamp": "2026-02-10T23:45:30.1234567Z"
}
```

---

### Reserve Ticket

```http
POST /api/tickets/reserve
Content-Type: application/json
```

**Request Body:**
```json
{
  "eventId": 1,
  "ticketId": 5,
  "orderId": "ORD-2026-001",
  "reservedBy": "usuario@example.com",
  "expiresInSeconds": 300
}
```

**Validations:**
- `eventId`: Required, must be > 0
- `ticketId`: Required, must be > 0
- `orderId`: Required, non-empty string, max 80 characters
- `reservedBy`: Required, non-empty string, max 120 characters (email recommended)
- `expiresInSeconds`: Required, must be > 0 (default: 300 = 5 minutes)

**Response (202):** Accepted (async processing)
```json
{
  "message": "Reserva procesada",
  "ticketId": 5
}
```

**Error (400):**
```json
"EventId debe ser mayor a 0"
```

**Errors (400):**
- "EventId debe ser mayor a 0"
- "TicketId debe ser mayor a 0"
- "OrderId es requerido"
- "ReservedBy es requerido"
- "ExpiresInSeconds debe ser mayor a 0"

**Error (500):**
```json
{
  "message": "Error al procesar la reserva"
}
```

**Frontend Usage:**
```javascript
const reserveRequest = {
  eventId: 1,
  ticketId: 5,
  orderId: "ORD-" + Date.now(),
  reservedBy: "usuario@example.com",
  expiresInSeconds: 600 // 10 minutes
};

const response = await fetch('http://localhost:8001/api/tickets/reserve', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(reserveRequest)
});

if (response.status === 202) {
  const result = await response.json();
  console.log('Reserva en proceso para ticket:', result.ticketId);
  // En el backend, el mensaje se publica a RabbitMQ
} else if (response.status === 400) {
  const error = await response.text();
  console.error('Validación fallida:', error);
}
```

**⚠️ IMPORTANTE:**
- Response 202 = Accepted (mensaje en cola, no significa que se completó)
- El cambio de status del ticket en BD es asincrónico
- Se recomienda verificar el status del ticket después de algunos segundos
- Los mensajes se publican a RabbitMQ, eventos posteriores pueden modificar el ticket

---

## 📊 DATA MODELS

### EventDto
```json
{
  "id": 1,
  "name": "string (max 200)",
  "startsAt": "datetime (ISO 8601)",
  "availableTickets": 0,
  "reservedTickets": 0,
  "paidTickets": 0
}
```

### CreateEventRequest
```json
{
  "name": "string (required, max 200)",
  "startsAt": "datetime (required, ISO 8601)"
}
```

### UpdateEventRequest
```json
{
  "name": "string (optional, max 200)",
  "startsAt": "datetime (optional, ISO 8601)"
}
```

### TicketDto
```json
{
  "id": 1,
  "eventId": 1,
  "status": "available|reserved|paid|released|cancelled",
  "reservedAt": "datetime or null",
  "expiresAt": "datetime or null",
  "paidAt": "datetime or null",
  "orderId": "string or null (max 80)",
  "reservedBy": "string or null (max 120)",
  "version": 0
}
```

### CreateTicketsRequest
```json
{
  "eventId": 1,
  "quantity": 100
}
```

### UpdateTicketStatusRequest
```json
{
  "newStatus": "available|reserved|paid|released|cancelled",
  "reason": "string (optional, max 200)"
}
```

### ReserveTicketRequest
```json
{
  "eventId": 1,
  "ticketId": 5,
  "orderId": "string (required, max 80)",
  "reservedBy": "string (required, max 120)",
  "expiresInSeconds": 300
}
```

---

## 📈 STATUS CODES & ERRORS

### Success Codes
| Code | Meaning | Use Case |
|------|---------|----------|
| 200 | OK | GET, PUT successful |
| 201 | Created | POST (resource created) |
| 202 | Accepted | POST (async - reserve) |
| 204 | No Content | DELETE successful |

### Error Codes
| Code | Meaning | When |
|------|---------|------|
| 400 | Bad Request | Invalid input data |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Version mismatch in concurrency |
| 500 | Server Error | Unexpected error |

### Common Errors by Endpoint

**Events:**
- 400: "El nombre del evento es requerido"
- 400: "La fecha de inicio es requerida"
- 404: "Evento {id} no encontrado"

**Tickets:**
- 400: "EventId debe ser mayor a 0"
- 400: "Quantity debe estar entre 1 y 1000"
- 404: "Evento no encontrado"
- 404: "Ticket no encontrado"
- 409: "Conflicto de versión. El ticket fue modificado por otro usuario."

**Reserve:**
- 400: "EventId debe ser mayor a 0"
- 400: "TicketId debe ser mayor a 0"
- 400: "OrderId es requerido"
- 400: "ReservedBy es requerido"
- 400: "ExpiresInSeconds debe ser mayor a 0"
- 500: "Error al procesar la reserva"

---

## 💡 EXAMPLES

### Ejemplo 1: Crear evento y tickets

```javascript
// 1. Crear evento
const eventRes = await fetch('http://localhost:8002/api/events', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    name: 'Concierto Especial',
    startsAt: new Date('2026-07-01T20:00:00Z').toISOString()
  })
});

const event = await eventRes.json();
console.log('Evento creado:', event.id);

// 2. Crear 100 tickets
const ticketsRes = await fetch('http://localhost:8002/api/tickets/bulk', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    eventId: event.id,
    quantity: 100
  })
});

const ticketsData = await ticketsRes.json();
console.log('Tickets creados:', ticketsData.createdCount);
```

---

### Ejemplo 2: Reservar un ticket

```javascript
// 1. Obtener tickets disponibles
const ticketsRes = await fetch(`http://localhost:8002/api/tickets/event/1`);
const tickets = await ticketsRes.json();
const available = tickets.find(t => t.status === 'available');

if (!available) {
  console.error('No hay tickets disponibles');
  return;
}

// 2. Reservar el ticket
const reserveRes = await fetch('http://localhost:8001/api/tickets/reserve', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    eventId: 1,
    ticketId: available.id,
    orderId: `ORD-${Date.now()}`,
    reservedBy: 'user@example.com',
    expiresInSeconds: 600
  })
});

if (reserveRes.status === 202) {
  console.log('Reserva en proceso');
  
  // 3. Verificar después de 2 segundos
  setTimeout(async () => {
    const checkRes = await fetch(`http://localhost:8002/api/tickets/${available.id}`);
    const updated = await checkRes.json();
    console.log('Status actual:', updated.status);
  }, 2000);
}
```

---

### Ejemplo 3: Cancelar reserva

```javascript
// 1. Obtener ticket
const res = await fetch(`http://localhost:8002/api/tickets/5`);
const ticket = await res.json();

// 2. Cancelar (si está reservado)
if (ticket.status === 'reserved') {
  const cancelRes = await fetch(`http://localhost:8002/api/tickets/5`, {
    method: 'PUT',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      newStatus: 'released',
      reason: 'Usuario canceló'
    })
  });
  
  const cancelled = await cancelRes.json();
  console.log('Ticket liberado, versión:', cancelled.version);
}
```

---

## 🛠️ Development Tips para Frontend

### Rate Limiting (Recomendado implementar)
```javascript
const delay = (ms) => new Promise(resolve => setTimeout(resolve, ms));

async function fetchWithRetry(url, options, retries = 3) {
  for (let i = 0; i < retries; i++) {
    try {
      const res = await fetch(url, options);
      if (res.ok || res.status === 400) return res;
      if (i < retries - 1) await delay(1000);
    } catch (err) {
      if (i === retries - 1) throw err;
      await delay(1000);
    }
  }
}
```

### Polling para Cambios Asincronos
```javascript
async function waitForReservation(ticketId, maxWaitMs = 10000) {
  const startTime = Date.now();
  
  while (Date.now() - startTime < maxWaitMs) {
    const res = await fetch(`http://localhost:8002/api/tickets/${ticketId}`);
    const ticket = await res.json();
    
    if (ticket.status === 'reserved') {
      return ticket;
    }
    
    await delay(500);
  }
  
  throw new Error('Timeout esperando reserva');
}
```

### Manejo de Errores
```javascript
async function handleApiError(response) {
  if (!response.ok) {
    const message = await response.text();
    const error = new Error(message);
    error.status = response.status;
    throw error;
  }
  return response.json();
}
```

---

## 📋 Changelog

### v1.0 (Current)
- Events CRUD completo
- Tickets CRUD con soporte para bulk create
- Reserve endpoint async
- Optimistic concurrency control
- Full validation

---

**API Endpoint Base URLs:**
- CRUD: `http://localhost:8002`
- Producer: `http://localhost:8001`

**Last Updated:** February 10, 2026
