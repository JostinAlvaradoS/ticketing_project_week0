# Notification Service

## Overview

The Notification Service is responsible for sending email notifications to customers when their tickets are issued. It consumes `ticket-issued` events from Kafka and sends confirmation emails with ticket details and PDF attachments.

### Architecture

The service follows **Hexagonal (Ports & Adapters) Architecture**:

```
src/
├── Domain/              # Core entities (EmailNotification)
├── Application/         # Use cases (SendTicketNotification handler)
│   ├── Ports/          # Interfaces (IEmailService, IEmailNotificationRepository)
│   └── UseCases/       # Command handlers (MediatR)
├── Infrastructure/      # Implementations
│   ├── Events/         # Kafka consumer (TicketIssuedEventConsumer)
│   ├── Email/          # SMTP adapter (SmtpEmailService)
│   ├── Persistence/    # Database context & repository
│   └── Migrations/     # EF Core migration files
└── Api/                # Minimal API endpoints
```

## Technology Stack

- **.NET 8**: Target framework
- **EF Core**: ORM with PostgreSQL
- **Kafka**: Event streaming (Confluent.Kafka)
- **MediatR**: Command handler pattern
- **FluentValidation**: Input validation
- **Serilog**: Structured logging

## Database Schema

Uses PostgreSQL schema `bc_notification` with the following table:

### EmailNotifications
- `Id` (GUID, PK)
- `TicketId` (GUID, FK to Fulfillment service)
- `OrderId` (GUID, FK to Ordering service)
- `RecipientEmail` (string, max 255)
- `Subject` (string, max 255)
- `Body` (text)
- `Status` (enum: Pending, Sent, Failed, Retrying)
- `TicketPdfUrl` (string, optional)
- `FailureReason` (string, optional)
- `CreatedAt`, `SentAt`, `UpdatedAt` (timestamps)
- **Indexes**: OrderId (unique), Status, CreatedAt

## Configuration

### Environment Variables

```bash
# Database
ConnectionStrings__Default=Host=postgres;Port=5432;Database=speckit;Username=postgres;Password=postgres;SearchPath=bc_notification

# Kafka
Kafka__BootstrapServers=kafka:9092
Kafka__ConsumerGroupId=notification-service
Kafka__Topics__TicketIssued=ticket-issued

# Email (SMTP)
Email__Smtp__Host=smtp.gmail.com
Email__Smtp__Port=587
Email__Smtp__Username=your-email@gmail.com
Email__Smtp__Password=app-specific-password
Email__Smtp__FromAddress=noreply@ticketing.local
Email__Smtp__FromName=Ticketing Platform
Email__Smtp__EnableSsl=true
Email__Smtp__UseDevMode=false  # Set to false in production
```

### appsettings.json Example

```json
{
  "ConnectionStrings": {
    "Default": "Host=postgres;Port=5432;Database=speckit;Username=postgres;Password=postgres;SearchPath=bc_notification"
  },
  "Kafka": {
    "BootstrapServers": "kafka:9092",
    "ConsumerGroupId": "notification-service",
    "Topics": {
      "TicketIssued": "ticket-issued"
    }
  },
  "Email": {
    "Smtp": {
      "Host": "localhost",
      "Port": 587,
      "Username": "",
      "Password": "",
      "FromAddress": "noreply@ticketing.local",
      "FromName": "Ticketing Platform",
      "EnableSsl": true,
      "UseDevMode": true
    }
  }
}
```

## Event Schema

### Input: `ticket-issued` Event

```json
{
  "ticket_id": "550e8400-e29b-41d4-a716-446655440000",
  "order_id": "550e8400-e29b-41d4-a716-446655550000",
  "customer_email": "customer@example.com",
  "event_name": "Concert 2026",
  "seat_number": "A1",
  "price": 99.99,
  "currency": "USD",
  "ticket_pdf_url": "https://bucket.s3.amazonaws.com/tickets/550e8400-e29b-41d4-a716-446655440000.pdf",
  "qr_code_data": "QR_CODE_ENCODED_DATA",
  "issued_at": "2026-03-01T10:00:00Z",
  "timestamp": "2026-03-01T10:00:00Z"
}
```

## API Endpoints

- **GET** `/health` - Service health check
  - Response: `{ "status": "healthy", "service": "Notification" }`

## Features

### 1. Kafka Consumer (`TicketIssuedEventConsumer`)
- Listens to `ticket-issued` topic
- Deserializes JSON messages
- Invokes `SendTicketNotificationCommand` via MediatR
- Handles deserialization errors gracefully

### 2. Email Service (`SmtpEmailService`)
- **Dev Mode**: Logs emails instead of sending (useful for testing)
- **Production Mode**: Sends via configured SMTP server
- Supports email attachments (PDF tickets)
- Extensible for future integration with AWS SES, SendGrid, etc.

### 3. Notification Handler (`SendTicketNotificationHandler`)
- Implements ATDD (Acceptance Test Driven Development)
- **Idempotency**: Checks if notification already exists before creating
- **Transactional**: Persists notification to database
- **Error Handling**: Captures and logs failures
- **Status Tracking**: Updates notification status (Pending → Sent/Failed)

### 4. Repository Pattern (`EmailNotificationRepository`)
- CRUD operations on `EmailNotifications` table
- Unique constraint on `OrderId` to prevent duplicates
- Indexed queries for efficient lookups

## Testing

### Unit Tests (xUnit + Moq)

Test suites for:

1. **Application Layer** (`Notification.Application.UnitTests/`)
   - Handler command processing
   - Idempotency checks
   - Email service failure handling
   - Database persistence

2. **Domain Layer** (`Notification.Domain.UnitTests/`)
   - Entity creation and validation
   - Status transitions
   - Field constraints

3. **Infrastructure Layer** (`Notification.IntegrationTests/`)
   - Repository CRUD operations
   - Kafka consumer event deserialization
   - Database transaction handling

### Running Tests

```bash
# Unit tests
cd services/notification
dotnet test

# Specific test class
dotnet test --filter "SendTicketNotificationHandlerTests"

# With coverage
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover
```

## Running the Service

### Local Development

```bash
# Navigate to API project
cd services/notification/src/Api

# Run locally (requires PostgreSQL + Kafka running)
dotnet run --environment Development

# Service runs on http://localhost:5006
```

### Docker

```bash
# From root directory
docker-compose up notification

# Verify
curl http://localhost:5006/health
```

## Smoke Test

Run the Phase 2 smoke test:

```bash
./docker-smoke-test-phase2.sh
```

Or the notification service-specific smoke test:

```bash
./services/notification/smoke-test.sh
```

## Deployment

### Database Migrations

Migrations are automatically applied on service startup via `IDbInitializer`. To manually manage:

```bash
cd services/notification/src/Api

# Create new migration
dotnet ef migrations add {MigrationName} --project ../Infrastructure

# Update database
dotnet ef database update --project ../Infrastructure
```

### Production Deployment

1. **Configure SMTP credentials** in environment variables
2. **Set `Email:Smtp:UseDevMode=false`** to enable actual email sending
3. **Configure connection strings** for production PostgreSQL instance
4. **Set Kafka brokers** to production cluster
5. **Enable structured logging** with Serilog sinks (Seq, DataDog, etc.)

## Troubleshooting

### Emails not being sent

- Check if `UseDevMode=true` is set (emails are logged, not sent)
- Verify SMTP credentials in configuration
- Check service logs for SMTP connection errors
- Ensure Kafka messages are being consumed properly

### Database connection errors

```bash
# Test connection
psql -h {host} -U postgres -d speckit -c "SELECT 1"

# Verify schema exists
psql -h {host} -U postgres -d speckit -c "SELECT schema_name FROM information_schema.schemata"
```

### Kafka consumer not receiving events

- Verify topic exists: `kafka-topics --list --bootstrap-server localhost:9092`
- Check consumer group: `kafka-consumer-groups --list --bootstrap-server localhost:9092`
- Inspect messages: `kafka-console-consumer --bootstrap-server localhost:9092 --topic ticket-issued --from-beginning`

## Future Enhancements

1. **Queue-based Email**: Use background job queue (Hangfire) for better reliability
2. **Outbox Pattern**: Implement for guaranteed delivery
3. **Email Templates**: HTML template engine (Liquid/Scriban)
4. **Attachments**: Properly handle PDF and other file types
5. **Retry Logic**: Exponential backoff for failed notifications
6. **Observability**: Add distributed tracing with OpenTelemetry
7. **Analytics**: Track email open rates and click-throughs
8. **Multi-language**: Support localized email templates

## Related Services

- **Fulfillment**: Publishes `ticket-issued` event
- **Ordering**: Provides order context
- **Identity**: Provides customer authentication

## References

- [Notification Service Spec](../../specs/001-ticketing-mvp/notification.feature)
- [Project Architecture](../../specs/001-ticketing-mvp/plan.md)
- [Hexagonal Architecture](https://en.wikipedia.org/wiki/Hexagonal_architecture_(software))
- [MediatR Documentation](https://github.com/jbogard/MediatR)
