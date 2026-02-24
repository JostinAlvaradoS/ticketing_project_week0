# 🎫 SpecKit Ticketing - Guía de Integración Frontend

## 📋 Tabla de Contenidos

1. [Setup & Configuración](#setup--configuración)
2. [Puertos y URLs](#puertos-y-urls)
3. [Flujo de Compra E2E](#flujo-de-compra-e2e)
4. [Endpoints Detallados](#endpoints-detallados)
5. [Modelos de Datos](#modelos-de-datos)
6. [Ejemplos de Requests](#ejemplos-de-requests)
7. [Manejo de Errores](#manejo-de-errores)
8. [Autenticación](#autenticación)

---

## Setup & Configuración

### Requisitos Previos

- **Docker Desktop** instalado y corriendo
- **Docker Compose** v2.0+
- Puerto local disponible: 5003, 50000-50002

### Iniciar los Servicios

```bash
cd infra
docker compose up -d
```

### Verificar el Estado

```bash
docker compose ps
```

Todos los servicios deberían estar en estado **Up (healthy)**.

### Health Check

```bash
# Verificar que todos responden
curl http://localhost:50000/health
curl http://localhost:50001/health
curl http://localhost:50002/health
curl http://localhost:5003/health
```

Respuesta esperada:
```json
{ "status": "Healthy", "service": "ServiceName" }
```

---

## 🔌 Puertos y URLs

### URLs Base de Servicios

| Servicio | URL | Puerto Interno | Propósito |
|----------|-----|---|---|
| **Identity** | `http://localhost:50000` | 5000 | Autenticación y autorización |
| **Catalog** | `http://localhost:50001` | 5001 | Eventos, asientos, seatmaps |
| **Inventory** | `http://localhost:50002` | 5002 | Reservaciones de asientos |
| **Ordering** | `http://localhost:5003` | 5003 | Carrito y pedidos |

### Bases de Datos (Uso Interno)

| Recurso | Endpoint | Puerto |
|---------|----------|--------|
| PostgreSQL | `postgres://postgres:postgres@localhost:5432/ticketing` | 5432 |
| Redis | `redis://localhost:6379` | 6379 |
| Kafka | `kafka://localhost:9092` | 9092 |

---

## 🔄 Flujo de Compra E2E

```
┌─────────────────────────────────────────────────────────────────┐
│                        FRONTEND APP                              │
└─────────────────────────────────────────────────────────────────┘
                             │
                             ▼
        ┌────────────────────────────────────────┐
        │  1. GET /events/{eventId}/seatmap     │
        │      CATALOG (localhost:50001)         │
        │  ← Obtener evento y asientos           │
        └────────────────────────────────────────┘
                             │
                             ▼
        ┌────────────────────────────────────────┐
        │  2. POST /reservations                 │
        │      INVENTORY (localhost:50002)       │
        │  ← Reservar asiento (30 min)           │
        └────────────────────────────────────────┘
                             │
                    ⏱️ ESPERAR 2-3 SEG
                             │
                             ▼
        ┌────────────────────────────────────────┐
        │  3. POST /cart/add                     │
        │      ORDERING (localhost:5003)        │
        │  ← Agregar al carrito                  │
        └────────────────────────────────────────┘
                             │
                             ▼
        ┌────────────────────────────────────────┐
        │  4. POST /orders/checkout              │
        │      ORDERING (localhost:5003)        │
        │  ← Finalizar compra                    │
        └────────────────────────────────────────┘
                             │
                             ▼
                    ✅ PEDIDO COMPLETED
```

---

## 🔌 Endpoints Detallados

### **1️⃣ CATALOG - Obtener Evento y Seatmap**

#### Endpoint
```http
GET http://localhost:50001/events/{eventId}/seatmap
```

#### Parámetros

| Parámetro | Ubicación | Tipo | Requerido | Ejemplo |
|-----------|-----------|------|-----------|---------|
| `eventId` | URL Path | UUID | ✅ Sí | `550e8400-e29b-41d4-a716-446655440000` |

#### Headers

```http
Content-Type: application/json
Accept: application/json
```

#### Request Ejemplo

```bash
curl -X GET http://localhost:50001/events/550e8400-e29b-41d4-a716-446655440000/seatmap \
  -H "Content-Type: application/json"
```

#### Response (200 OK)

```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Coldplay - Live in Santiago",
  "description": "World tour 2026",
  "eventDate": "2026-03-15T19:00:00Z",
  "basePrice": 50.00,
  "seats": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440002",
      "sectionCode": "A",
      "rowNumber": 1,
      "seatNumber": 2,
      "price": 50.00,
      "status": "available"
    },
    {
      "id": "550e8400-e29b-41d4-a716-446655440003",
      "sectionCode": "A",
      "rowNumber": 1,
      "seatNumber": 3,
      "price": 50.00,
      "status": "reserved"
    }
  ]
}
```

#### Estados de Asiento

- `available` → El asiento está disponible para reservar
- `reserved` → El asiento está reservado por otro usuario
- `sold` → El asiento ya fue vendido

---

### **2️⃣ INVENTORY - Reservar Asiento**

#### Endpoint
```http
POST http://localhost:50002/reservations
```

#### Headers

```http
Content-Type: application/json
Accept: application/json
```

#### Body Parameters

| Parámetro | Tipo | Requerido | Descripción | Ejemplo |
|-----------|------|-----------|-------------|---------|
| `seatId` | UUID | ✅ Sí | ID del asiento a reservar | `550e8400-e29b-41d4-a716-446655440002` |
| `customerId` | string | ✅ Sí | ID del cliente (email, user ID, etc) | `customer-123` o `jose@example.com` |

#### Request Ejemplo

```bash
curl -X POST http://localhost:50002/reservations \
  -H "Content-Type: application/json" \
  -d '{
    "seatId": "550e8400-e29b-41d4-a716-446655440002",
    "customerId": "customer-123"
  }'
```

#### Response (200 OK)

```json
{
  "reservationId": "8bf7fffc-9ff5-401c-9d2d-86f525f42e40",
  "seatId": "550e8400-e29b-41d4-a716-446655440002",
  "customerId": "customer-123",
  "expiresAt": "2026-02-24T16:15:00Z",
  "status": "active"
}
```

#### Detalles Importantes

- **Duración de reserva**: 30 minutos desde la creación
- **Renovación automática**: Si agregas al carrito antes de expirar, se mantiene
- **Expiración**: Si no se agrega al carrito en 30 min, la reserva se cancela automáticamente
- **Evento Kafka**: Se genera `reservation-created` → Llega a Ordering en 2-3 seg
- **Esperar**: ⏱️ Espera 2-3 segundos antes de proceder a /cart/add

#### Errores Comunes

```json
// 400 Bad Request - Datos inválidos
{
  "error": "Invalid seatId format"
}

// 409 Conflict - Asiento ya reservado
{
  "error": "Seat is already reserved"
}

// 404 Not Found - Asiento no existe
{
  "error": "Seat not found"
}
```

---

### **3️⃣ ORDERING - Agregar al Carrito**

#### Endpoint
```http
POST http://localhost:5003/cart/add
```

#### Headers

```http
Content-Type: application/json
Accept: application/json
```

#### Body Parameters

| Parámetro | Tipo | Requerido | Descripción | Ejemplo |
|-----------|------|-----------|-------------|---------|
| `reservationId` | UUID | ❌ No | ID de la reservación (puede ser null) | `8bf7fffc-9ff5-401c-9d2d-86f525f42e40` |
| `seatId` | UUID | ✅ Sí | ID del asiento | `550e8400-e29b-41d4-a716-446655440002` |
| `price` | decimal | ✅ Sí | Precio del asiento | `50.00` |
| `userId` | string | ⚠️ Uno | ID del usuario autenticado | `user-123` |
| `guestToken` | string | ⚠️ Uno | Token para compras anónimas | `guest-abc123xyz` |

**⚠️ Requisito**: Debes enviar **al menos uno** de `userId` o `guestToken`

#### Request Ejemplo (Usuario Autenticado)

```bash
curl -X POST http://localhost:5003/cart/add \
  -H "Content-Type: application/json" \
  -d '{
    "reservationId": "8bf7fffc-9ff5-401c-9d2d-86f525f42e40",
    "seatId": "550e8400-e29b-41d4-a716-446655440002",
    "price": 50.00,
    "userId": "user-123"
  }'
```

#### Request Ejemplo (Compra Anónima)

```bash
curl -X POST http://localhost:5003/cart/add \
  -H "Content-Type: application/json" \
  -d '{
    "reservationId": "8bf7fffc-9ff5-401c-9d2d-86f525f42e40",
    "seatId": "550e8400-e29b-41d4-a716-446655440002",
    "price": 50.00,
    "guestToken": "guest-abc123xyz"
  }'
```

#### Response (200 OK)

```json
{
  "id": "order-uuid-001",
  "userId": "user-123",
  "guestToken": null,
  "totalAmount": 50.00,
  "state": "draft",
  "createdAt": "2026-02-24T15:15:00Z",
  "paidAt": null,
  "items": [
    {
      "id": "item-001",
      "seatId": "550e8400-e29b-41d4-a716-446655440002",
      "price": 50.00
    }
  ]
}
```

#### Estados del Carrito

- `draft` → Carrito creado, esperando más asientos o checkout
- `pending` → Checkout realizado, esperando pago
- `completed` → Pago completado
- `cancelled` → Pedido cancelado

#### Errores Comunes

```json
// 400 Bad Request - UserId y GuestToken no enviados
{
  "error": "Either UserId or GuestToken must be provided"
}

// 400 Bad Request - Reservación no encontrada
{
  "error": "Reservation not found"
}

// 400 Bad Request - Asiento ya en carrito
{
  "error": "Seat is already in the cart"
}

// 400 Bad Request - Reservación expirada
{
  "error": "Reservation has expired"
}
```

#### Agregar Múltiples Asientos

Llama a `/cart/add` una vez por cada asiento. El sistema:
- Obtiene el carrito draft existente
- Agrega el nuevo asiento
- Actualiza el total

```bash
# Llamada 1 - Asiento A1
curl -X POST http://localhost:5003/cart/add \
  -H "Content-Type: application/json" \
  -d '{"reservationId":"id-1","seatId":"seat-1","price":50.00,"userId":"user-123"}'

# Respuesta contiene orderId
# → Usar ese orderId en las siguientes llamadas (el sistema lo matchea por userId)

# Llamada 2 - Asiento A2
curl -X POST http://localhost:5003/cart/add \
  -H "Content-Type: application/json" \
  -d '{"reservationId":"id-2","seatId":"seat-2","price":50.00,"userId":"user-123"}'

# El carrito ahora tiene 2 asientos, totalAmount = 100.00
```

---

### **4️⃣ ORDERING - Checkout**

#### Endpoint
```http
POST http://localhost:5003/orders/checkout
```

#### Headers

```http
Content-Type: application/json
Accept: application/json
```

#### Body Parameters

| Parámetro | Tipo | Requerido | Descripción | Ejemplo |
|-----------|------|-----------|-------------|---------|
| `orderId` | UUID | ✅ Sí | ID del pedido (del response de /cart/add) | `order-uuid-001` |
| `userId` | string | ⚠️ Uno | ID del usuario autenticado | `user-123` |
| `guestToken` | string | ⚠️ Uno | Token para compras anónimas | `guest-abc123xyz` |

**⚠️ Requisito**: Debes enviar **al menos uno** de `userId` o `guestToken`, y debe coincidir con el usado en /cart/add

#### Request Ejemplo

```bash
curl -X POST http://localhost:5003/orders/checkout \
  -H "Content-Type: application/json" \
  -d '{
    "orderId": "order-uuid-001",
    "userId": "user-123"
  }'
```

#### Response (200 OK)

```json
{
  "id": "order-uuid-001",
  "userId": "user-123",
  "guestToken": null,
  "totalAmount": 50.00,
  "state": "pending",
  "createdAt": "2026-02-24T15:15:00Z",
  "paidAt": "2026-02-24T15:16:30Z",
  "items": [
    {
      "id": "item-001",
      "seatId": "550e8400-e29b-41d4-a716-446655440002",
      "price": 50.00
    }
  ]
}
```

#### Errores Comunes

```json
// 404 Not Found - Pedido no existe
{
  "error": "Order not found"
}

// 401 Unauthorized - UserId/GuestToken no coincide
{
  "error": "Unauthorized"
}

// 400 Bad Request - Pedido no está en estado draft
{
  "error": "Order is not in draft state"
}
```

---

## 📊 Modelos de Datos

### Event

```typescript
interface Event {
  id: string;              // UUID
  name: string;
  description: string;
  eventDate: Date;
  basePrice: number;
  seats: Seat[];
}
```

### Seat

```typescript
interface Seat {
  id: string;              // UUID
  sectionCode: string;
  rowNumber: number;
  seatNumber: number;
  price: number;
  status: "available" | "reserved" | "sold";
}
```

### Reservation

```typescript
interface Reservation {
  reservationId: string;   // UUID
  seatId: string;          // UUID
  customerId: string;
  expiresAt: Date;
  status: "active" | "expired" | "consumed";
}
```

### Order

```typescript
interface Order {
  id: string;              // UUID
  userId?: string;
  guestToken?: string;
  totalAmount: number;
  state: "draft" | "pending" | "completed" | "cancelled";
  createdAt: Date;
  paidAt?: Date;
  items: OrderItem[];
}

interface OrderItem {
  id: string;              // UUID
  seatId: string;          // UUID
  price: number;
}
```

---

## 📝 Ejemplos de Requests

### JavaScript/Fetch

```javascript
// 1. Obtener evento y seatmap
async function getEventSeatmap(eventId) {
  const response = await fetch(
    `http://localhost:50001/events/${eventId}/seatmap`
  );
  return response.json();
}

// 2. Reservar asiento
async function reserveSeat(seatId, customerId) {
  const response = await fetch('http://localhost:50002/reservations', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ seatId, customerId })
  });
  return response.json();
}

// 3. Agregar al carrito
async function addToCart(reservationId, seatId, price, userId) {
  const response = await fetch('http://localhost:5003/cart/add', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({
      reservationId,
      seatId,
      price,
      userId
    })
  });
  return response.json();
}

// 4. Checkout
async function checkout(orderId, userId) {
  const response = await fetch('http://localhost:5003/orders/checkout', {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ orderId, userId })
  });
  return response.json();
}

// Uso:
async function buyTicket(eventId, seatId, userId) {
  try {
    // Step 1
    const event = await getEventSeatmap(eventId);
    console.log('Event loaded:', event.name);

    // Step 2
    const reservation = await reserveSeat(seatId, userId);
    console.log('Reservation created:', reservation.reservationId);

    // Step 3 - Wait for Kafka
    await new Promise(resolve => setTimeout(resolve, 3000));

    // Step 4
    const seat = event.seats.find(s => s.id === seatId);
    const order = await addToCart(
      reservation.reservationId,
      seatId,
      seat.price,
      userId
    );
    console.log('Order created:', order.id);

    // Step 5
    const finalOrder = await checkout(order.id, userId);
    console.log('Purchase complete:', finalOrder.state);

    return finalOrder;
  } catch (error) {
    console.error('Error:', error.message);
  }
}
```

### cURL

```bash
#!/bin/bash

EVENT_ID="550e8400-e29b-41d4-a716-446655440000"
SEAT_ID="550e8400-e29b-41d4-a716-446655440002"
USER_ID="user-123"

# Step 1: Get event seatmap
echo "1. Getting event seatmap..."
EVENT=$(curl -s http://localhost:50001/events/$EVENT_ID/seatmap)
PRICE=$(echo $EVENT | jq -r '.basePrice')
echo "Price: $PRICE"

# Step 2: Reserve seat
echo "2. Reserving seat..."
RESERVATION=$(curl -s -X POST http://localhost:50002/reservations \
  -H "Content-Type: application/json" \
  -d "{\"seatId\":\"$SEAT_ID\",\"customerId\":\"$USER_ID\"}")
RESERVATION_ID=$(echo $RESERVATION | jq -r '.reservationId')
echo "Reservation ID: $RESERVATION_ID"

# Step 3: Wait for Kafka
echo "3. Waiting for event processing (3 seconds)..."
sleep 3

# Step 4: Add to cart
echo "4. Adding to cart..."
ORDER=$(curl -s -X POST http://localhost:5003/cart/add \
  -H "Content-Type: application/json" \
  -d "{\"reservationId\":\"$RESERVATION_ID\",\"seatId\":\"$SEAT_ID\",\"price\":$PRICE,\"userId\":\"$USER_ID\"}")
ORDER_ID=$(echo $ORDER | jq -r '.id')
echo "Order ID: $ORDER_ID"

# Step 5: Checkout
echo "5. Checking out..."
FINAL=$(curl -s -X POST http://localhost:5003/orders/checkout \
  -H "Content-Type: application/json" \
  -d "{\"orderId\":\"$ORDER_ID\",\"userId\":\"$USER_ID\"}")
echo "Final order:"
echo $FINAL | jq .
```

### Python

```python
import requests
import time
import json

BASE_URLs = {
    'catalog': 'http://localhost:50001',
    'inventory': 'http://localhost:50002',
    'ordering': 'http://localhost:5003'
}

def get_event_seatmap(event_id):
    """GET /events/{eventId}/seatmap from Catalog"""
    url = f"{BASE_URLs['catalog']}/events/{event_id}/seatmap"
    response = requests.get(url)
    response.raise_for_status()
    return response.json()

def reserve_seat(seat_id, customer_id):
    """POST /reservations to Inventory"""
    url = f"{BASE_URLs['inventory']}/reservations"
    payload = {"seatId": seat_id, "customerId": customer_id}
    response = requests.post(url, json=payload)
    response.raise_for_status()
    return response.json()

def add_to_cart(reservation_id, seat_id, price, user_id):
    """POST /cart/add to Ordering"""
    url = f"{BASE_URLs['ordering']}/cart/add"
    payload = {
        "reservationId": reservation_id,
        "seatId": seat_id,
        "price": price,
        "userId": user_id
    }
    response = requests.post(url, json=payload)
    response.raise_for_status()
    return response.json()

def checkout(order_id, user_id):
    """POST /orders/checkout to Ordering"""
    url = f"{BASE_URLs['ordering']}/orders/checkout"
    payload = {"orderId": order_id, "userId": user_id}
    response = requests.post(url, json=payload)
    response.raise_for_status()
    return response.json()

def buy_ticket(event_id, seat_id, user_id):
    """Complete purchase flow"""
    try:
        # Step 1: Get event
        print(f"1. Getting event {event_id}...")
        event = get_event_seatmap(event_id)
        seat = next(s for s in event['seats'] if s['id'] == seat_id)
        price = seat['price']
        print(f"   Seat price: ${price}")

        # Step 2: Reserve
        print(f"2. Reserving seat {seat_id}...")
        reservation = reserve_seat(seat_id, user_id)
        reservation_id = reservation['reservationId']
        print(f"   Reservation ID: {reservation_id}")

        # Step 3: Wait
        print("3. Waiting for Kafka event (3 seconds)...")
        time.sleep(3)

        # Step 4: Add to cart
        print("4. Adding to cart...")
        order = add_to_cart(reservation_id, seat_id, price, user_id)
        order_id = order['id']
        print(f"   Order ID: {order_id}")
        print(f"   Total: ${order['totalAmount']}")

        # Step 5: Checkout
        print("5. Checking out...")
        final = checkout(order_id, user_id)
        print(f"   Order State: {final['state']}")
        print("✅ Purchase complete!")

        return final

    except requests.exceptions.RequestException as e:
        print(f"❌ Error: {e}")
        return None
```

---

## 🚨 Manejo de Errores

### Errores Comunes y Soluciones

#### 1. "Reservation not found"

**Causa**: La reservación no ha llegado a Ordering todavía

**Solución**: 
- Espera 2-3 segundos después de crear la reservación
- El evento debe procesarse por Kafka

```javascript
// ❌ Esto falla
const reservation = await reserveSeat(seatId, userId);
const order = await addToCart(reservation.id, seatId, price, userId);

// ✅ Esto funciona
const reservation = await reserveSeat(seatId, userId);
await delay(3000); // Esperar 3 segundos
const order = await addToCart(reservation.id, seatId, price, userId);
```

---

#### 2. "Reservation has expired"

**Causa**: Pasaron más de 30 minutos desde la reservación

**Solución**:
- Crea una nueva reservación
- El carrito tiene 30 minutos para completarse

```javascript
// Si el tiempo expira, vuelve a reservar
const newReservation = await reserveSeat(seatId, userId);
await delay(3000);
const order = await addToCart(newReservation.id, seatId, price, userId);
```

---

#### 3. "Seat is already in the cart"

**Causa**: El asiento ya fue agregado al carrito

**Solución**:
- NO llames a `/cart/add` con el mismo seatId dos veces
- Para múltiples asientos, usa diferentes seatIds

```javascript
// ❌ Esto falla - mismo seatId
await addToCart(res1.id, seatId, price, userId);
await addToCart(res2.id, seatId, price, userId); // Error!

// ✅ Esto funciona - diferentes seatIds
await addToCart(res1.id, seatId1, price, userId);
await addToCart(res2.id, seatId2, price, userId);
```

---

#### 4. "Either UserId or GuestToken must be provided"

**Causa**: No enviaste ni userId ni guestToken

**Solución**:
- Si es usuario autenticado: envía `userId`
- Si es compra anónima: envía `guestToken`
- NUNCA envíes ambos como null/undefined

```javascript
// ❌ Incorrecto
await addToCart(resId, seatId, price, undefined, undefined);

// ✅ Correcto
await addToCart(resId, seatId, price, "user-123", null);
// O
await addToCart(resId, seatId, price, null, "guest-token");
```

---

#### 5. Servicio no responde (Connection Refused)

**Causa**: 
- Docker no está corriendo
- El servicio no ha iniciado
- Puerto incorrecto

**Solución**:
```bash
# Verificar que Docker está corriendo
docker compose ps

# Si algún servicio está DOWN, reinicia
docker compose up -d

# Esperar ~10 segundos para que se inicialicen los servicios
sleep 10

# Test health
curl http://localhost:50001/health
curl http://localhost:50002/health
curl http://localhost:5003/health
```

---

#### 6. CORS Error (Frontend - Backend)

**Causa**: El frontend (en puerto 3000/8080) no puede llamar al backend (5003, etc)

**Solución**: Los servicios necesitan CORS configurado

```javascript
// Solicitar al backend que agregue estas headers:
// Access-Control-Allow-Origin: *
// Access-Control-Allow-Methods: GET, POST, PUT, DELETE
// Access-Control-Allow-Headers: Content-Type

// En el frontend, hacer requests normalmente:
fetch('http://localhost:5003/cart/add', {
  method: 'POST',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(...)
});
```

---

## 🔐 Autenticación

### Identity Service (Próximamente)

El servicio Identity está disponible en `http://localhost:50000` pero aún está en desarrollo.

**Endpoints esperados:**
- `POST /identity/login` → Obtener JWT token
- `POST /identity/register` → Crear usuario
- `GET /identity/validate` → Validar token

**Por ahora**, usa los endpoints sin validación:
```javascript
// Usa un userId simple
const userId = "user-" + Math.random().toString(36).substring(7);
// O un email
const userId = "customer@example.com";
```

---

## 📱 Flujo Recomendado para Frontend

### Página 1: Seleccionar Evento y Asientos

```
1. Mostrar lista de eventos (future work - usar datos hardcodeados por ahora)
2. Al seleccionar evento:
   - GET /events/{eventId}/seatmap
   - Mostrar seatmap visual
3. Usuario selecciona asientos y hace click "Reservar"
```

### Página 2: Confirmar Reserva

```
1. Por cada asiento seleccionado:
   - POST /reservations
   - Guardar reservationId
2. Esperar 3 segundos
3. Mostrar resumen de asientos y precios
4. Click en "Continuar a carrito"
```

### Página 3: Carrito

```
1. Por cada asiento:
   - POST /cart/add con reservationId
   - Guardar orderId (del primer asiento, luego usa igual orderId)
2. Mostrar resumen:
   - Lista de asientos
   - Total a pagar
3. Click en "Finalizar compra"
```

### Página 4: Confirmación

```
1. POST /orders/checkout
2. Mostrar:
   - ✅ Compra completada
   - Order ID
   - Código de confirmación
   - Detalles de asientos
```

---

## 🧪 Testing Checklist

- [ ] Todos los servicios responden a /health
- [ ] Puedo obtener el seatmap de Catalog
- [ ] Puedo reservar un asiento en Inventory
- [ ] Después de esperar 3s, puedo agregar al carrito
- [ ] Puedo hacer checkout sin errores
- [ ] Multiple asientos en el mismo carrito
- [ ] Compra anónima (con guestToken)
- [ ] Compra autenticada (con userId)

---

## 📞 Soporte

Si tienes dudas sobre los endpoints:

1. Revisa los logs: `docker compose logs -f ordering`
2. Prueba con Postman importando esta guía
3. Revisa los ejemplos en `docker-smoke-test.sh`

