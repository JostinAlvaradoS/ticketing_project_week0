---
title: Identity Service
description: Autenticación y generación de tokens JWT para la plataforma SpecKit Ticketing
---

# Identity Service

## Propósito

El Identity Service es el punto de entrada de autenticación del sistema. Su responsabilidad es emitir tokens JWT válidos que los demás servicios utilizan para verificar la identidad del usuario y su rol (User o Admin).

En el contexto actual del proyecto (entrenamiento), no valida contraseñas. Su objetivo es proveer el mecanismo de identidad y la estructura de autorización que los servicios downstream consumen.

---

## Stack Técnico

| Componente | Tecnología |
|-----------|-----------|
| Framework | .NET 9 — Minimal APIs |
| ORM | Entity Framework Core |
| Base de Datos | PostgreSQL — schema `bc_identity` |
| Autenticación | JWT (System.IdentityModel.Tokens.Jwt) |
| Mediator | MediatR |
| Puerto | `5000` (local), `50000` (Docker) |

---

## Estructura Interna

```
services/identity/
├── Api/
│   └── Endpoints/
│       ├── TokenEndpoints.cs        ← POST /token
│       └── UserEndpoints.cs         ← POST /users
├── Application/
│   ├── Commands/
│   │   ├── IssueTokenCommand.cs
│   │   ├── IssueTokenHandler.cs
│   │   ├── CreateUserCommand.cs
│   │   └── CreateUserHandler.cs
│   └── Ports/
│       └── IUserRepository.cs       ← Puerto hacia persistencia
├── Domain/
│   └── Entities/
│       └── User.cs                  ← id, email, password, role
└── Infrastructure/
    ├── Persistence/
    │   ├── IdentityDbContext.cs
    │   └── UserRepository.cs
    └── Security/
        └── JwtTokenGenerator.cs
```

---

## Endpoints

### `POST /token`

Genera un JWT para el usuario indicado.

**Request:**
```json
{
  "email": "user@example.com",
  "password": "cualquiera"
}
```

**Response 200:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "expiresAt": "2026-04-07T12:00:00Z",
  "userRole": "User",
  "userEmail": "user@example.com"
}
```

**Lógica:**
1. Busca el usuario por email en `bc_identity.Users`
2. Si existe, genera el JWT con su rol
3. Si no existe, retorna 401

> En modo desarrollo, el password no se valida. Solo importa que el email exista.

---

### `POST /users`

Crea un nuevo usuario en el sistema.

**Request:**
```json
{
  "email": "admin@example.com",
  "password": "admin123",
  "role": "Admin"
}
```

**Response 201:**
```json
{
  "userId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "email": "admin@example.com",
  "role": "Admin"
}
```

**Roles disponibles:** `User`, `Admin`

---

### `GET /health`

Retorna el estado del servicio.

---

## Esquema de Base de Datos

**Schema:** `bc_identity`

```sql
CREATE TABLE "Users" (
    "Id"        UUID PRIMARY KEY,
    "Email"     VARCHAR(255) NOT NULL UNIQUE,
    "Password"  VARCHAR(255) NOT NULL,
    "Role"      VARCHAR(50)  NOT NULL DEFAULT 'User',
    "CreatedAt" TIMESTAMP NOT NULL
);
```

---

## Configuración JWT

```json
{
  "Jwt": {
    "Key": "dev-secret-key-minimum-32-chars-required-for-security",
    "Issuer": "SpecKit.Identity",
    "Audience": "SpecKit.Services",
    "ExpirationHours": 24
  }
}
```

El token generado incluye los claims:
- `sub` — userId
- `email` — email del usuario
- `role` — User | Admin
- `exp` — fecha de expiración

---

## Uso del Token en Otros Servicios

Los servicios que exponen endpoints protegidos (como Catalog para operaciones de administración) validan el JWT usando la misma clave y configuración:

```
Authorization: Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...
```

El rol `Admin` es necesario para acceder a los endpoints de `POST /admin/events`, `PUT /admin/events/{id}`, etc.

---

## Notas de Desarrollo

- **No hashea contraseñas**: En contexto de entrenamiento, las contraseñas se almacenan en texto plano
- **No valida credenciales**: El endpoint `/token` solo verifica que el email exista
- **Fase futura**: OAuth2/OIDC, bcrypt/argon2, tokens de refresco, 2FA
