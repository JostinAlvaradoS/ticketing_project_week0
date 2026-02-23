# Infrastructure — Operations Guide

## Quick Start

```bash
cd infra
docker compose up -d
```

Verify all services are healthy:
```bash
docker compose ps
```

## PostgreSQL Connection String

**Development Connection String:**
```
Server=localhost;Port=5432;Database=ticketing;User Id=postgres;Password=postgres;
```

**Inside Docker Network (service-to-service):**
```
Server=postgres;Port=5432;Database=ticketing;User Id=postgres;Password=postgres;
```

**EF Core .NET Configuration:**
```csharp
// In each service's DbContext
optionsBuilder.UseNpgsql(connectionString);
// Schema is configured per context: modelBuilder.HasDefaultSchema("bc_<service>");
```

## Schemas & Bounded Contexts

| Schema | Service | Purpose |
|--------|---------|---------|
| `bc_identity` | Identity | User/token management |
| `bc_catalog` | Catalog | Event catalog (read-side) |
| `bc_inventory` | Inventory | Seat reservations & availability |
| `bc_ordering` | Ordering | Shopping cart & orders |
| `bc_payment` | Payment | Payment records (simulated) |
| `bc_fulfillment` | Fulfillment | Ticket generation & PDF storage |
| `bc_notification` | Notification | Notification state & delivery logs |

## Database Roles (Recommended Setup)

For production-like environments, create roles per schema:

```sql
-- Connect to PostgreSQL and run:
CREATE ROLE bc_identity_user WITH LOGIN PASSWORD 'identity_password';
CREATE ROLE bc_catalog_user WITH LOGIN PASSWORD 'catalog_password';
CREATE ROLE bc_inventory_user WITH LOGIN PASSWORD 'inventory_password';
CREATE ROLE bc_ordering_user WITH LOGIN PASSWORD 'ordering_password';
CREATE ROLE bc_payment_user WITH LOGIN PASSWORD 'payment_password';
CREATE ROLE bc_fulfillment_user WITH LOGIN PASSWORD 'fulfillment_password';
CREATE ROLE bc_notification_user WITH LOGIN PASSWORD 'notification_password';

-- Grant schema privileges
GRANT USAGE ON SCHEMA bc_identity TO bc_identity_user;
GRANT CREATE ON SCHEMA bc_identity TO bc_identity_user;
GRANT ALL PRIVILEGES ON ALL TABLES IN SCHEMA bc_identity TO bc_identity_user;
GRANT ALL PRIVILEGES ON ALL SEQUENCES IN SCHEMA bc_identity TO bc_identity_user;

-- Repeat for each schema...
```

## Services & Ports

| Service | Port | Role |
|---------|------|------|
| PostgreSQL | 5432 | Single database, multi-schema |
| Redis | 6379 | Distributed locks, reservation cache |
| Zookeeper | 2181 | Kafka coordination |
| Kafka Broker | 9092 | Event streaming (localhost for dev) |

## Migration & Schema Initialization

**Initial setup:**
- `init-schemas.sql` runs automatically on `docker compose up`.
- All schemas and tables created in sequence.

**Adding new migrations (EF Core):**
```bash
cd services/<service>/src
dotnet ef migrations add <MigrationName> --project Infrastructure
dotnet ef database update
```

## Shutting Down

```bash
docker compose down
# Preserve volumes:


docker compose down
# Remove all data:
docker compose down -v
```

## Troubleshooting

**Postgres connection refused:**
```bash
docker compose logs postgres
docker compose ps  # Check if postgres is healthy
```

**Redis cache not working:**
```bash
docker exec speckit-redis redis-cli ping
```

**Kafka broker issues:**
```bash
docker compose logs kafka
docker compose logs zookeeper
```

**Reset all data:**
```bash
docker compose down -v
docker compose up -d
```
