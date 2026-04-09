---
title: Notification Service
description: Envío de emails con boletos digitales al completarse el flujo de compra
---

# Notification Service

## Propósito

El Notification Service es el último eslabón del flujo de compra. Escucha el evento `ticket-issued` y envía un email al cliente con su boleto adjunto. Persiste el estado de cada notificación para tener trazabilidad del envío.

Como el Fulfillment Service, es **completamente event-driven** — no expone endpoints de negocio al exterior. Solo responde a eventos Kafka.

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | .NET 9 — Minimal APIs |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL — schema `bc_notification` |
| Mensajería | Apache Kafka (consumidor) |
| Email | SMTP / Dev Mode (log en consola) |
| Mediator | MediatR |
| Puerto | `5005` (local), `50005` (Docker) |

---

## Estructura Interna

```
services/notification/
├── Api/
│   └── Endpoints/
│       └── HealthEndpoints.cs                  ← GET /health
├── Application/
│   └── Commands/
│       ├── SendTicketNotificationCommand.cs
│       └── SendTicketNotificationHandler.cs
├── Domain/
│   └── Entities/
│       └── EmailNotification.cs                ← estado del envío
└── Infrastructure/
    ├── Persistence/
    │   ├── NotificationDbContext.cs
    │   └── NotificationRepository.cs
    ├── Messaging/
    │   └── TicketIssuedEventConsumer.cs         ← Consume: ticket-issued
    └── Email/
        └── SmtpEmailService.cs                  ← Envía o simula el email
```

---

## Flujo de Notificación

```
Kafka: ticket-issued
        │
        ▼
TicketIssuedEventConsumer.Handle()
        │
        ▼
SendTicketNotificationCommand {
  ticketId, ticketNumber, orderId,
  customerId, eventId, seatNumber,
  pdfPath, qrCode, issuedAt
}
        │
        ▼
SendTicketNotificationHandler:
  1. Verificar idempotencia (¿ya existe notificación para orderId?)
     └── Si sí → ignorar
  2. Construir EmailNotification (subject, body, recipientEmail)
  3. Persistir con status = "Pending"
  4. SmtpEmailService.Send(email)
     ├── Dev Mode: log en consola
     └── Prod Mode: SMTP real
  5. Actualizar status = "Sent" (o "Failed")
```

---

## Esquema de Base de Datos

**Schema:** `bc_notification`

```sql
CREATE TABLE "EmailNotifications" (
    "Id"             UUID PRIMARY KEY,
    "TicketId"       UUID NOT NULL,
    "OrderId"        UUID NOT NULL UNIQUE,
    "RecipientEmail" VARCHAR(255) NOT NULL,
    "Subject"        VARCHAR(500) NOT NULL,
    "Body"           TEXT NOT NULL,
    "Status"         VARCHAR(50) NOT NULL DEFAULT 'Pending',
    "TicketPdfUrl"   VARCHAR(500) NULL,
    "FailureReason"  VARCHAR(500) NULL,
    "CreatedAt"      TIMESTAMP NOT NULL,
    "SentAt"         TIMESTAMP NULL,
    "UpdatedAt"      TIMESTAMP NOT NULL
);
```

**Estados:** `Pending`, `Sent`, `Failed`, `Retrying`

> La restricción `UNIQUE` en `OrderId` garantiza que un cliente no reciba múltiples emails por la misma compra.

---

## Mensajería Kafka

### Consume: `ticket-issued`

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

---

## Configuración de Email

```json
{
  "Email": {
    "Smtp": {
      "Host": "localhost",
      "Port": 587,
      "Username": "",
      "Password": "",
      "FromAddress": "noreply@ticketing.local",
      "FromName": "SpecKit Ticketing",
      "EnableSsl": true,
      "UseDevMode": true
    }
  }
}
```

Cuando `UseDevMode: true`, el servicio escribe el contenido del email en los logs en lugar de enviarlo por SMTP. Útil para desarrollo y pruebas.

---

## Contenido del Email

El email generado incluye:

- **Subject:** `Tu boleto para {eventName} — {ticketNumber}`
- **Body HTML:**
  - Confirmación de compra
  - Detalles del evento (nombre, fecha, venue)
  - Detalles del asiento (sección, fila, número)
  - Imagen del código QR embebida
  - Instrucciones de acceso al evento
- **Adjunto:** PDF del boleto (referenciado desde `pdfPath`)

---

## Idempotencia

```csharp
var existing = await _repo.FindByOrderId(command.OrderId);
if (existing != null) {
    _logger.LogWarning("Notification already sent for order {OrderId}. Skipping.", command.OrderId);
    return;
}
```

---

## Notas de Diseño

- La persistencia del estado de la notificación (`Pending` → `Sent`/`Failed`) permite implementar reintentos en el futuro sin riesgo de duplicados
- En producción se integraría con un provider de email transaccional (SendGrid, Amazon SES) modificando solo `SmtpEmailService` — sin cambios en Application ni Domain
- El servicio podría extenderse para enviar notificaciones push o SMS cambiando el adaptador de infraestructura, sin tocar la lógica de aplicación
