# Identity Service

Identity service for the SpecKit Ticketing Platform. Provides JWT token generation for development and testing.

## Architecture

Follows Hexagonal Architecture with complete separation of concerns:
- **Domain** (`Identity.Domain`): Core business entities and domain logic (no infrastructure dependencies)
- **Application** (`Identity.Application`): Use cases, commands, queries, and application services
- **Infrastructure** (`Identity.Infrastructure`): Database context, repositories, and external adapters
- **Api** (`Identity.Api`): HTTP endpoints using Minimal API

## Project Structure

```
services/identity/
├── Identity.sln                    # Solution file
├── src/
│   ├── Domain/                     # Identity.Domain.csproj
│   ├── Application/                # Identity.Application.csproj
│   ├── Infrastructure/             # Identity.Infrastructure.csproj
│   └── Api/                        # Identity.Api.csproj (entry point)
└── README.md
```

## Schema

Uses PostgreSQL schema: `bc_identity`

## Endpoints

### Health Check
```http
GET /health
```

Returns service health status.

### Issue Token (Development)
```http
POST /token
Content-Type: application/json

{
  "userId": "user-123",
  "email": "user@example.com",
  "expiresInHours": 24,
  "claims": {
    "role": "customer"
  }
}
```

Issues a JWT token for development/testing. All fields are optional.

## Running the Microservice

### From the solution root:
```bash
cd services/identity
dotnet run --project src/Api/Identity.Api.csproj
```

### From the API directory:
```bash
cd services/identity/src/Api
dotnet run
```

### Building the entire solution:
```bash
cd services/identity
dotnet build
```

The service will start on `http://localhost:5000` (or configured port via `--urls`).

### Running on a specific port:
```bash
dotnet run --project src/Api/Identity.Api.csproj --urls "http://localhost:5100"
```

## Configuration

See `appsettings.json` for configuration options:
- **Jwt**: Token signing key, issuer, and audience
- **Serilog**: Structured logging configuration
- **Seq**: Log aggregation server URL (optional)
- **Jaeger**: Distributed tracing configuration (optional)

## Development

The token endpoint is intentionally simple for MVP development. It does not validate credentials or connect to a user database. This will be enhanced in later phases.

## Dependencies

- .NET 8
- Serilog (structured logging)
- OpenTelemetry (distributed tracing)
- Swashbuckle (Swagger/OpenAPI)
- JWT Bearer authentication

## Testing

```bash
# Health check
curl http://localhost:5100/health

# Generate token
curl -X POST http://localhost:5100/token \
  -H "Content-Type: application/json" \
  -d '{"userId":"test-123","email":"test@example.com"}'
```

