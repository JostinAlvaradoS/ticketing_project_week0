---
title: Fulfillment Service
description: Generación de boletos digitales con PDF y códigos QR al confirmar el pago
---

# Fulfillment Service

## Propósito

El Fulfillment Service es el responsable de emitir los boletos digitales. Cuando un pago es confirmado, este servicio genera un boleto con código QR único y un PDF descargable, los persiste, y publica el evento `ticket-issued` para que Notification envíe el boleto al cliente.

Es un servicio **completamente event-driven** desde el punto de entrada — no expone endpoints REST para el flujo principal. Solo actúa en respuesta a eventos Kafka.

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | .NET 9 — Minimal APIs |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL — schema `bc_fulfillment` |
| Mensajería | Apache Kafka (productor y consumidor) |
| Mediator | MediatR |
| Almacenamiento | Volumen Docker (`fulfillment-data:/app/data/tickets`) |
| Puerto | `5004` (interno), `50004` (Docker) |

---

## Estructura Interna

```
services/fulfillment/
├── Api/
│   └── Endpoints/
│       └── HealthEndpoints.cs              ← GET /health
├── Application/
│   └── Commands/
│       ├── GenerateTicketCommand.cs
│       └── GenerateTicketHandler.cs
├── Domain/
│   └── Entities/
│       └── Ticket.cs                       ← id, orderId, customerId, eventId, seatNumber, qrCode, pdfPath
└── Infrastructure/
    ├── Persistence/
    │   ├── FulfillmentDbContext.cs
    │   └── TicketRepository.cs
    ├── Messaging/
    │   ├── PaymentSucceededConsumer.cs     ← Consume: payment-succeeded
    │   └── TicketIssuedProducer.cs         ← Produce: ticket-issued
    └── Services/
        ├── QrCodeGenerator.cs              ← Genera QR code único
        └── PdfTicketGenerator.cs           ← Genera PDF del boleto
```

---

## Flujo de Generación de Boleto

```
Kafka: payment-succeeded
        │
        ▼
PaymentSucceededConsumer.Handle()
        │
        ▼
GenerateTicketCommand {
  orderId, customerId, eventId,
  seatId, seatNumber, section,
  amount, currency, paymentId
}
        │
        ▼
GenerateTicketHandler:
  1. Verificar idempotencia (¿ya existe ticket para orderId?)
     └── Si sí → ignorar (evento duplicado)
  2. Generar ticketNumber único (ej: TKT-20260406-0001)
  3. QrCodeGenerator.Generate(ticketId, seatNumber, eventId)
     └── Produce: string JSON codificado en base64 → imagen QR
  4. PdfTicketGenerator.Generate(ticket, qrCode)
     └── Produce: archivo PDF en /app/data/tickets/{ticketId}.pdf
  5. Persistir Ticket en bc_fulfillment
  6. Publicar "ticket-issued" en Kafka
```

---

## Esquema de Base de Datos

**Schema:** `bc_fulfillment`

```sql
CREATE TABLE "Tickets" (
    "Id"            UUID PRIMARY KEY,
    "OrderId"       UUID NOT NULL UNIQUE,
    "CustomerId"    VARCHAR(255) NOT NULL,
    "EventId"       UUID NULL,
    "EventName"     VARCHAR(255) NULL,
    "SeatId"        UUID NULL,
    "SeatNumber"    VARCHAR(20) NULL,
    "Section"       VARCHAR(100) NULL,
    "Price"         DECIMAL(10,2) NOT NULL,
    "Currency"      VARCHAR(10) NOT NULL DEFAULT 'USD',
    "Status"        VARCHAR(50) NOT NULL DEFAULT 'Generated',
    "QrCodeData"    TEXT NOT NULL,
    "TicketPdfPath" VARCHAR(500) NULL,
    "TicketNumber"  VARCHAR(100) NULL,
    "GeneratedAt"   TIMESTAMP NOT NULL,
    "CreatedAt"     TIMESTAMP NOT NULL
);
```

**Estados del ticket:** `Generated`, `Delivered`, `Used`, `Cancelled`

> La restricción `UNIQUE` en `OrderId` garantiza idempotencia — no se pueden generar dos boletos para la misma orden.

---

## Mensajería Kafka

### Consume: `payment-succeeded`

Trigger principal para la generación del boleto.

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

---

### Produce: `ticket-issued`

Publicado tras generar exitosamente el boleto.

```json
{
  "ticketId": "uuid",
  "ticketNumber": "TKT-20260406-0001",
  "orderId": "uuid",
  "paymentId": "uuid",
  "customerId": "uuid",
  "eventId": "uuid",
  "seatId": "uuid",
  "seatNumber": "A-01",
  "section": "VIP",
  "pdfPath": "/app/data/tickets/uuid.pdf",
  "qrCode": "base64-encoded-qr-data",
  "issuedAt": "2026-04-06T13:05:30Z",
  "status": "generated"
}
```

**Consumidores:** Notification (envía email con el PDF)

---

## Almacenamiento de PDFs

Los PDFs generados se guardan en un volumen Docker persistente:

```
Volume: fulfillment-data
Mount: /app/data/tickets/

Archivos:
  /app/data/tickets/{ticketId}.pdf
```

El path se almacena en `Ticket.TicketPdfPath` para ser referenciado por el servicio de notificación al adjuntar el archivo al email.

---

## Idempotencia

El servicio maneja correctamente eventos duplicados:

```csharp
// En GenerateTicketHandler
var existingTicket = await _repo.FindByOrderId(command.OrderId);
if (existingTicket != null) {
    _logger.LogWarning("Ticket already exists for order {OrderId}. Skipping.", command.OrderId);
    return;
}
```

Esto protege contra el caso de que `payment-succeeded` sea publicado más de una vez (posible en sistemas Kafka con semántica at-least-once).

---

## Notas de Diseño

- El Fulfillment Service no necesita conocer los detalles del evento o del asiento directamente — los recibe en el evento `payment-succeeded` (que los propaga desde el contexto de Payment)
- En producción, el almacenamiento de PDFs se movería a un object storage (S3, GCS) y el path en DB sería una URL firmada
- El QR code contiene suficiente información para validar el boleto en la entrada del evento sin necesidad de conexión a internet
