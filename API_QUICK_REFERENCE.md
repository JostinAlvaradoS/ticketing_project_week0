# 📋 API Quick Reference

## Endpoints Summary

### CRUD Service (http://localhost:8002)

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| GET | `/health` | Service health | 200 |
| GET | `/api/events` | List all events | 200 |
| GET | `/api/events/{id}` | Get event | 200 |
| POST | `/api/events` | Create event | 201 |
| PUT | `/api/events/{id}` | Update event | 200 |
| DELETE | `/api/events/{id}` | Delete event | 204 |
| GET | `/api/tickets/event/{eventId}` | List event tickets | 200 |
| GET | `/api/tickets/{id}` | Get ticket | 200 |
| POST | `/api/tickets/bulk` | Create tickets | 201 |
| PUT | `/api/tickets/{id}` | Update ticket | 200 |

### Producer Service (http://localhost:8001)

| Method | Endpoint | Purpose | Status |
|--------|----------|---------|--------|
| GET | `/health` | Service health | 200 |
| POST | `/api/tickets/reserve` | Reserve ticket | **202** |

---

## Request/Response Examples

### Create Event
```bash
curl -X POST http://localhost:8002/api/events \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Concierto 2026",
    "startsAt": "2026-06-01T20:00:00Z"
  }'
```

**Response (201):**
```json
{
  "id": 3,
  "name": "Concierto 2026",
  "startsAt": "2026-06-01T20:00:00Z",
  "availableTickets": 0,
  "reservedTickets": 0,
  "paidTickets": 0
}
```

---

### Create Tickets
```bash
curl -X POST http://localhost:8002/api/tickets/bulk \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "quantity": 50
  }'
```

**Response (201):**
```json
{
  "createdCount": 50,
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
    }
    // ... 49 more
  ]
}
```

---

### Reserve Ticket
```bash
curl -X POST http://localhost:8001/api/tickets/reserve \
  -H "Content-Type: application/json" \
  -d '{
    "eventId": 1,
    "ticketId": 5,
    "orderId": "ORD-2026-001",
    "reservedBy": "user@example.com",
    "expiresInSeconds": 300
  }'
```

**Response (202 - Note: ACCEPTED, not 200):**
```json
{
  "message": "Reserva procesada",
  "ticketId": 5
}
```

⚠️ **Important:** 202 = Accepted for async processing, NOT confirmation of completion

---

### Update Ticket Status
```bash
curl -X PUT http://localhost:8002/api/tickets/1 \
  -H "Content-Type: application/json" \
  -d '{
    "newStatus": "released",
    "reason": "Usuario canceló"
  }'
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
  "reservedBy": "user@example.com",
  "version": 1
}
```

---

## Validation Rules

### Event Creation
```json
{
  "name": "max 200 chars, required",
  "startsAt": "ISO 8601 datetime, required"
}
```

### Ticket Creation (Bulk)
```json
{
  "eventId": "required, > 0",
  "quantity": "required, 1-1000"
}
```

### Ticket Reservation
```json
{
  "eventId": "required, > 0",
  "ticketId": "required, > 0",
  "orderId": "required, max 80 chars",
  "reservedBy": "required, max 120 chars",
  "expiresInSeconds": "required, > 0"
}
```

---

## Ticket Status Lifecycle

```
available ──→ reserved ──→ paid ──→ released
    ↓              ↓         ↓         (final)
    └─→ cancelled ←┴─────────┘
         (final)
```

### Valid Transitions
- `available` → `reserved`, `cancelled`
- `reserved` → `paid`, `released`, `cancelled`
- `paid` → `released`, `cancelled`
- `released` → ✗ (final state)
- `cancelled` → ✗ (final state)

---

## Error Codes

| Code | Meaning | Example |
|------|---------|---------|
| 200 | OK | GET/PUT success |
| 201 | Created | POST success |
| 202 | Accepted | Async operation started |
| 204 | No Content | DELETE success |
| 400 | Bad Request | Invalid input |
| 404 | Not Found | Resource doesn't exist |
| 409 | Conflict | Version mismatch |
| 500 | Server Error | Unexpected error |

---

## Common Error Messages

### Event Errors
```
"El nombre del evento es requerido"
"La fecha de inicio es requerida"
"Evento {id} no encontrado"
```

### Ticket Errors
```
"EventId debe ser mayor a 0"
"Quantity debe estar entre 1 y 1000"
"Ticket no encontrado"
"Conflicto de versión. El ticket fue modificado por otro usuario."
```

### Reservation Errors
```
"OrderId es requerido"
"ReservedBy es requerido"
"ExpiresInSeconds debe ser mayor a 0"
"Error al procesar la reserva"
```

---

## Quick Code Snippets

### Get Available Tickets
```javascript
const response = await fetch('http://localhost:8002/api/tickets/event/1');
const tickets = await response.json();
const available = tickets.filter(t => t.status === 'available');
console.log(`${available.length} tickets available`);
```

### Reserve with Polling
```javascript
// Send reservation (202 Accepted)
await fetch('http://localhost:8001/api/tickets/reserve', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({
    eventId: 1,
    ticketId: 5,
    orderId: `ORD-${Date.now()}`,
    reservedBy: 'user@example.com',
    expiresInSeconds: 300
  })
});

// Poll for confirmation
for (let i = 0; i < 20; i++) {
  await new Promise(r => setTimeout(r, 500));
  const res = await fetch('http://localhost:8002/api/tickets/5');
  const ticket = await res.json();
  if (ticket.status === 'reserved') {
    console.log('✓ Reserved!');
    break;
  }
}
```

---

## Database Constraints

### Tickets
- `status` ∈ {available, reserved, paid, released, cancelled}
- When `status = 'reserved'`: both `reserved_at` and `expires_at` required
- `version` starts at 0, increments on each update (optimistic locking)

### Events
- `name` max 200 characters
- `starts_at` required, datetime

### Payments
- `amount_cents` must be > 0
- `currency` defaults to 'USD'
- `paid_at` nullable, set when status = 'paid'

---

## Configuration

### Environment Variables (`.env`)

```env
# CRUD Service
CONNECTIONSTRINGS__DEFAULTCONNECTION=Host=postgres;Port=5432;Database=ticketing;Username=ticketing_user;Password=ticketing_password

# Producer Service
RABBITMQ__HOST=rabbitmq
RABBITMQ__PORT=5672
RABBITMQ__USERNAME=guest
RABBITMQ__PASSWORD=guest
RABBITMQ__EXCHANGE=tickets
RABBITMQ__QUEUE=reserve_queue

# Both services
ASPNETCORE_ENVIRONMENT=Development
```

### appsettings.Development.json

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=localhost;Port=5432;Database=ticketing;Username=ticketing_user;Password=ticketing_password"
  },
  "RabbitMQ": {
    "Host": "localhost",
    "Port": 5672,
    "Username": "guest",
    "Password": "guest",
    "Exchange": "tickets",
    "Queue": "reserve_queue"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning"
    }
  }
}
```

---

## Testing with Postman

1. Import `postman_collection.json`
2. Set environment variables:
   - `crud_base_url`: http://localhost:8002
   - `producer_base_url`: http://localhost:8001
3. Run collections in order:
   - Events CRUD
   - Tickets CRUD
   - Reservations

---

## Performance Notes

- **GET /api/events**: ~5ms (in-memory + DB)
- **POST /api/tickets/bulk**: ~50ms (bulk insert)
- **POST /api/tickets/reserve**: ~2ms (async, returns 202 immediately)
- **PUT /api/tickets/{id}**: ~10ms (optimistic locking check)

---

## Schema Reference

### Events Table
```sql
CREATE TABLE events (
  id SERIAL PRIMARY KEY,
  name VARCHAR(200) NOT NULL,
  starts_at TIMESTAMP NOT NULL,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
);
```

### Tickets Table
```sql
CREATE TABLE tickets (
  id SERIAL PRIMARY KEY,
  event_id INTEGER NOT NULL REFERENCES events(id) ON DELETE CASCADE,
  status ticket_status NOT NULL DEFAULT 'available',
  reserved_at TIMESTAMP,
  expires_at TIMESTAMP,
  paid_at TIMESTAMP,
  order_id VARCHAR(80),
  reserved_by VARCHAR(120),
  version INTEGER NOT NULL DEFAULT 0,
  created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP,
  CONSTRAINT valid_reservation CHECK (
    (status != 'reserved' AND reserved_at IS NULL AND expires_at IS NULL)
    OR (status = 'reserved' AND reserved_at IS NOT NULL AND expires_at IS NOT NULL)
  )
);
```

---

## Deployment Checklist

- [ ] All environment variables set
- [ ] PostgreSQL 15+ running
- [ ] RabbitMQ 3.12+ running with credentials
- [ ] Health checks return 200
- [ ] CORS enabled (if frontend on different origin)
- [ ] Logging configured
- [ ] Monitoring set up
- [ ] Backups scheduled
- [ ] Rate limiting implemented (optional)
- [ ] Documentation updated

---

**Last Updated:** February 10, 2026

For full documentation, see [API_DOCUMENTATION.md](API_DOCUMENTATION.md)
For frontend integration, see [FRONTEND_INTEGRATION_GUIDE.md](FRONTEND_INTEGRATION_GUIDE.md)
