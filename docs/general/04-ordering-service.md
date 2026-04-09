---
title: Ordering Service
description: Carrito de compras, gestión de órdenes y máquina de estados del flujo de compra
---

# Ordering Service

## Propósito

El Ordering Service gestiona el carrito de compras y el ciclo de vida de las órdenes. Recibe reservas del usuario, las agrega a una orden draft, y orquesta la transición de la orden hacia el pago mediante un modelo de máquina de estados.

Una característica clave es que valida las reservas **sin llamadas HTTP a Inventory** — en su lugar, mantiene un caché en memoria alimentado por eventos Kafka (`reservation-created`). Esto lo hace resiliente a la latencia de otros servicios durante el flujo de compra.

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | .NET 9 — Minimal APIs |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL — schema `bc_ordering` |
| Mensajería | Apache Kafka (consumidor) |
| Mediator | MediatR |
| Puerto | `5003` (local y Docker) |

---

## Estructura Interna

```
services/ordering/
├── Api/
│   └── Endpoints/
│       ├── CartEndpoints.cs             ← POST /cart/add
│       └── OrderEndpoints.cs            ← POST /orders/checkout
├── Application/
│   ├── Commands/
│   │   ├── AddToCartCommand.cs
│   │   ├── AddToCartHandler.cs
│   │   ├── CheckoutCommand.cs
│   │   └── CheckoutHandler.cs
│   └── Services/
│       ├── ReservationValidationService.cs
│       └── ReservationStore.cs          ← Caché en memoria de reservas
├── Domain/
│   └── Entities/
│       ├── Order.cs                     ← Máquina de estados
│       └── OrderItem.cs
└── Infrastructure/
    ├── Persistence/
    │   ├── OrderingDbContext.cs
    │   └── OrderRepository.cs
    └── Messaging/
        ├── ReservationEventConsumer.cs  ← Consume: reservation-created
        └── PaymentFailedConsumer.cs     ← Consume: payment-failed
```

---

## Máquina de Estados de la Orden

```
         AddToCart
[Creación] ────────► [Draft]
                        │
                   Checkout()
                        │
                        ▼
                    [Pending]
                   /         \
     payment-succeeded   payment-failed
              │                 │
              ▼                 ▼
           [Paid]          [Cancelled]
              │
     fulfillment completo
              │
              ▼
         [Fulfilled]
```

| Estado | Significado |
|--------|-------------|
| `Draft` | Orden en construcción (carrito abierto) |
| `Pending` | Checkout completado, esperando pago |
| `Paid` | Pago confirmado |
| `Fulfilled` | Boleto emitido |
| `Cancelled` | Pago fallido o cancelación manual |

---

## Endpoints

### `POST /cart/add`

Agrega un asiento reservado al carrito del usuario.

**Request:**
```json
{
  "reservationId": "uuid",
  "seatId": "uuid",
  "price": 150.00,
  "userId": "uuid",
  "guestToken": null
}
```

> Se puede usar `userId` (usuario autenticado) o `guestToken` (checkout como invitado). Solo uno de los dos.

**Response 200:**
```json
{
  "success": true,
  "order": {
    "id": "uuid",
    "state": "Draft",
    "totalAmount": 150.00,
    "items": [
      {
        "id": "uuid",
        "seatId": "uuid",
        "price": 150.00
      }
    ]
  }
}
```

**Lógica interna:**
1. Verifica que `reservationId` exista en `ReservationStore` (caché Kafka)
2. Busca orden Draft del usuario/guest o crea una nueva
3. Agrega `OrderItem` a la orden
4. Actualiza `totalAmount`

---

### `POST /orders/checkout`

Convierte una orden Draft en Pending, habilitándola para pago.

**Request:**
```json
{
  "orderId": "uuid",
  "userId": "uuid",
  "guestToken": null
}
```

**Response 200:**
```json
{
  "id": "uuid",
  "state": "Pending",
  "totalAmount": 300.00,
  "paidAt": null,
  "createdAt": "2026-04-06T13:00:00Z"
}
```

---

## ReservationStore — Caché en Memoria

`ReservationStore` es un diccionario en memoria que se alimenta de eventos `reservation-created` consumidos desde Kafka.

```csharp
// Estructura interna simplificada
private readonly Dictionary<Guid, ReservationState> _reservations = new();

public void Register(ReservationCreatedEvent evt) {
    _reservations[evt.ReservationId] = new ReservationState(
        evt.SeatId, evt.CustomerId, evt.ExpiresAt, evt.Status
    );
}

public bool IsValid(Guid reservationId) {
    return _reservations.TryGetValue(reservationId, out var r)
        && r.Status == "Active"
        && r.ExpiresAt > DateTime.UtcNow;
}
```

**¿Por qué este patrón?**
- Evita una llamada HTTP a Inventory en cada `AddToCart`
- Kafka garantiza orden de mensajes por partición
- El TTL de 15 minutos hace que los datos en caché nunca estén desactualizados por más de 15 minutos

**Retries en Frontend:**
El cliente frontend implementa `addToCartWithRetry()` con 3 intentos y 3s de delay para compensar la latencia de propagación Kafka entre Inventory y Ordering.

---

## Soporte para Guest Checkout

Las órdenes pueden pertenecer a un usuario registrado o a un guest anónimo:

```sql
-- Una orden pertenece a userId O guestToken, nunca a ambos
"UserId"      UUID NULL,
"GuestToken"  VARCHAR(255) NULL,
```

El frontend genera un `guestToken` (UUID) al inicio de la sesión de compra si el usuario no está autenticado.

---

## Esquema de Base de Datos

**Schema:** `bc_ordering`

```sql
CREATE TABLE "Orders" (
    "Id"           UUID PRIMARY KEY,
    "UserId"       UUID NULL,
    "GuestToken"   VARCHAR(255) NULL,
    "TotalAmount"  DECIMAL(10,2) NOT NULL DEFAULT 0,
    "State"        VARCHAR(50) NOT NULL DEFAULT 'Draft',
    "CreatedAt"    TIMESTAMP NOT NULL,
    "PaidAt"       TIMESTAMP NULL
);

CREATE TABLE "OrderItems" (
    "Id"        UUID PRIMARY KEY,
    "OrderId"   UUID NOT NULL REFERENCES "Orders"("Id"),
    "SeatId"    UUID NOT NULL,
    "Price"     DECIMAL(10,2) NOT NULL,
    "CreatedAt" TIMESTAMP NOT NULL
);
```

---

## Mensajería Kafka

### Consume: `reservation-created`

Alimenta el `ReservationStore` con datos de nuevas reservas para validación posterior.

### Consume: `payment-failed`

Cuando un pago falla, cancela la orden correspondiente:

```json
{
  "orderId": "uuid",
  "status": "failed"
}
```

**Acción:** `Order.State` → `Cancelled`

---

## Notas de Diseño

- La separación entre `AddToCart` y `Checkout` permite que el usuario revise su carrito antes de comprometerse al pago
- En un sistema productivo, el `ReservationStore` en memoria se reemplazaría por Redis o una tabla de caché para soportar múltiples instancias del servicio
- La orden Draft se crea automáticamente al primer `AddToCart` — no se requiere un endpoint separado para "crear carrito"
