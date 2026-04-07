---
title: Payment Service
description: Procesamiento y simulación de pagos con validación basada en eventos
---

# Payment Service

## Propósito

El Payment Service procesa los pagos de las órdenes. Valida que la orden y la reserva sean válidas, simula el procesamiento del pago (con 95% de tasa de éxito) y publica el resultado como evento Kafka para que el resto del sistema reaccione.

Un aspecto diferenciador de su diseño es que **no realiza llamadas HTTP a otros servicios** para validar. En su lugar, mantiene su propio caché de reservas y órdenes alimentado por eventos Kafka — lo que lo hace resistente a fallos en cadena.

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | .NET 9 — Minimal APIs |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL — schema `bc_payment` |
| Mensajería | Apache Kafka (productor y consumidor) |
| Mediator | MediatR |
| Puerto | `5004` (local y Docker) |

---

## Estructura Interna

```
services/payment/
├── Api/
│   └── Endpoints/
│       └── PaymentEndpoints.cs              ← POST /payments
├── Application/
│   ├── Commands/
│   │   ├── ProcessPaymentCommand.cs
│   │   └── ProcessPaymentHandler.cs
│   └── Services/
│       ├── PaymentSimulatorService.cs        ← Simula resultado del pago
│       ├── EventBasedOrderValidationService.cs
│       └── EventBasedReservationValidationService.cs
├── Domain/
│   └── Entities/
│       └── Payment.cs                        ← id, orderId, amount, status
└── Infrastructure/
    ├── Persistence/
    │   ├── PaymentDbContext.cs
    │   └── PaymentRepository.cs
    ├── Messaging/
    │   ├── PaymentResultProducer.cs          ← Produce: payment-succeeded / payment-failed
    │   └── ReservationEventConsumer.cs       ← Consume: reservation-created
    └── State/
        └── ReservationStateStore.cs          ← Caché en memoria de reservas
```

---

## Endpoints

### `POST /payments`

Procesa el pago de una orden.

**Request:**
```json
{
  "orderId": "uuid",
  "customerId": "uuid",
  "reservationId": "uuid",
  "amount": 300.00,
  "currency": "USD",
  "paymentMethod": "credit_card"
}
```

**Métodos de pago aceptados:** `credit_card`, `debit_card`, `wallet`, `bank_transfer`

**Response 200 — Pago exitoso:**
```json
{
  "success": true,
  "payment": {
    "id": "uuid",
    "orderId": "uuid",
    "status": "Succeeded",
    "transactionId": "TXN-20260406-001",
    "processedAt": "2026-04-06T13:05:00Z"
  }
}
```

**Response 422 — Pago fallido:**
```json
{
  "success": false,
  "errorMessage": "Payment declined: insufficient funds",
  "payment": {
    "id": "uuid",
    "status": "Failed"
  }
}
```

**Códigos de respuesta:**
| Código | Situación |
|--------|-----------|
| `200` | Pago procesado (éxito o fallo — ver `success` en body) |
| `400` | Request inválido (campos faltantes o mal formados) |
| `404` | Orden no encontrada |
| `422` | El pago no puede procesarse (reserva expirada, orden en estado incorrecto) |
| `500` | Error interno |

---

## Lógica de Procesamiento de Pago

```
POST /payments
   │
   ▼
1. Validar campos del request
   │
   ▼
2. EventBasedOrderValidationService
   ├── ¿La orden existe en caché?
   └── ¿La orden está en estado "Pending"?
   │
   ▼
3. EventBasedReservationValidationService
   ├── ¿La reserva existe en ReservationStateStore?
   └── ¿La reserva no ha expirado?
   │
   ▼
4. Persistir Payment en estado "Pending"
   │
   ▼
5. PaymentSimulatorService.Simulate(amount, method)
   ├── 95% → PaymentResult.Success
   └── 5%  → PaymentResult.Failed (razón aleatoria)
   │
   ▼
6. Actualizar Payment.Status = Succeeded | Failed
   │
   ├── Succeeded → Publicar "payment-succeeded"
   └── Failed    → Publicar "payment-failed"
```

---

## PaymentSimulatorService

Simula el procesamiento de un gateway de pagos externo:

```csharp
// Comportamiento del simulador
public PaymentResult Simulate(decimal amount, string paymentMethod) {
    var random = new Random();
    var successRate = 0.95; // 95% de éxito

    if (random.NextDouble() < successRate) {
        return PaymentResult.Success(GenerateTransactionId());
    }

    var failureReasons = new[] {
        "insufficient_funds",
        "card_declined",
        "expired_card",
        "network_error"
    };
    return PaymentResult.Failed(failureReasons[random.Next(failureReasons.Length)]);
}
```

---

## Esquema de Base de Datos

**Schema:** `bc_payment`

```sql
CREATE TABLE "Payments" (
    "Id"            UUID PRIMARY KEY,
    "OrderId"       UUID NOT NULL,
    "CustomerId"    VARCHAR(255) NOT NULL,
    "ReservationId" UUID NULL,
    "Amount"        DECIMAL(10,2) NOT NULL,
    "Currency"      VARCHAR(10) NOT NULL DEFAULT 'USD',
    "PaymentMethod" VARCHAR(50) NOT NULL,
    "Status"        VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "TransactionId" VARCHAR(255) NULL,
    "FailureReason" VARCHAR(255) NULL,
    "CreatedAt"     TIMESTAMP NOT NULL,
    "ProcessedAt"   TIMESTAMP NULL
);
```

**Estados:** `Pending`, `Succeeded`, `Failed`

---

## Mensajería Kafka

### Produce: `payment-succeeded`

```json
{
  "paymentId": "uuid",
  "orderId": "uuid",
  "customerId": "uuid",
  "reservationId": "uuid",
  "amount": 300.00,
  "currency": "USD",
  "paymentMethod": "credit_card",
  "transactionId": "TXN-20260406-001",
  "processedAt": "2026-04-06T13:05:00Z",
  "status": "succeeded"
}
```

**Consumidores:** Fulfillment (genera ticket), Inventory (confirma venta del asiento)

---

### Produce: `payment-failed`

```json
{
  "paymentId": "uuid",
  "orderId": "uuid",
  "customerId": "uuid",
  "reservationId": "uuid",
  "amount": 300.00,
  "currency": "USD",
  "failureReason": "card_declined",
  "failedAt": "2026-04-06T13:05:00Z",
  "status": "failed"
}
```

**Consumidores:** Ordering (cancela orden), Inventory (libera asiento)

---

### Consume: `reservation-created`

Alimenta `ReservationStateStore` para poder validar reservas sin llamadas HTTP.

---

## Notas de Diseño

- La validación basada en eventos (sin HTTP) hace que Payment sea resiliente: funciona incluso si Inventory u Ordering están temporalmente caídos
- En producción, este servicio se reemplazaría con una integración real a un gateway (Stripe, PayPal, etc.) usando el adaptador de infraestructura correspondiente — sin cambios en la lógica de dominio
- El patrón de publicar tanto `payment-succeeded` como `payment-failed` (en lugar de lanzar una excepción) permite que múltiples servicios reaccionen al mismo resultado de forma independiente
