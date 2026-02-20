# Architecture Analysis

This document describes both the **legacy architecture** (traditional layered) and the **new architecture** (hexagonal/ports & adapters) used in this ticketing system.

---

## Table of Contents

1. [Legacy Architecture (Traditional Layered)](#legacy-architecture-traditional-layered)
2. [New Architecture (Hexagonal/Ports & Adapters)](#new-architecture-hexagonalports--adapters)
3. [Comparison](#comparison)
4. [Migration Status](#migration-status)
5. [Recommended Cleanup](#recommended-cleanup)

---

## Legacy Architecture (Traditional Layered)

### Overview

The legacy architecture follows the **traditional 3-tier layered pattern** commonly used in .NET applications:

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                       │
│         (Controllers, Consumers, Background Workers)        │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                     BUSINESS LAYER                          │
│              (Services, Business Logic)                     │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌─────────────────────────────────────────────────────────────┐
│                      DATA LAYER                             │
│            (Repositories, DbContext, Entities)              │
└─────────────────────────────────────────────────────────────┘
```

### Project Structure (Legacy)

```
ServiceName/
├── src/
│   └── ServiceName.Worker/           # All code in single project
│       ├── Controllers/              # API Controllers (if any)
│       ├── Consumers/               # RabbitMQ Consumers
│       ├── Services/                 # Business logic
│       │   ├── IService.cs
│       │   └── ServiceImpl.cs
│       ├── Repositories/             # Data access
│       │   ├── IRepository.cs
│       │   └── Repository.cs
│       ├── Models/                   # Entities & DTOs
│       │   ├── Entities/
│       │   └── DTOs/
│       ├── Data/                     # EF Core DbContext
│       │   ├── DbContext.cs
│       │   └── EntityConfigurations/
│       ├── Configurations/            # Settings classes
│       ├── Messaging/                 # RabbitMQ connection
│       ├── Handlers/                  # Event handlers
│       └── Extensions/                # DI extensions
└── tests/
    └── ServiceName.Worker.Tests/
```

### Characteristics

| Aspect | Description |
|--------|-------------|
| **Coupling** | High - layers depend directly on each other |
| **Testing** | Difficult - hard to mock dependencies |
| **Flexibility** | Low - difficult to swap implementations |
| **Dependencies** | Domain logic depends on infrastructure (EF Core, RabbitMQ) |
| **Single Project** | All code in one project/assembly |

### Example: Reservation Service (Legacy)

```
ReservationService/src/ReservationService.Worker/
├── Consumers/
│   └── TicketReservationConsumer.cs    # RabbitMQ consumer
├── Services/
│   ├── IReservationService.cs           # Service interface
│   └── ReservationServiceImpl.cs        # Business logic
├── Repositories/
│   ├── ITicketRepository.cs             # Repository interface
│   └── TicketRepository.cs              # Data access
├── Models/
│   ├── Ticket.cs                        # Entity
│   ├── TicketStatus.cs                  # Enum
│   └── ReservationMessage.cs            # DTO
├── Data/
│   └── TicketingDbContext.cs           # EF Core DbContext
└── Configurations/
    └── RabbitMQSettings.cs              # Configuration
```

### Problems with Legacy Architecture

1. **Tight Coupling**: Business logic directly depends on infrastructure (EF Core, RabbitMQ)
2. **Hard to Test**: Cannot unit test services without database/RabbitMQ
3. **Database Dependency**: Domain logic coupled to Entity Framework
4. **Message Broker Dependency**: Business logic depends on RabbitMQ client
5. **Single Responsibility Violation**: Services do too much (business + data access)
6. **Hard to Maintain**: Changes in infrastructure affect business logic

---

## New Architecture (Hexagonal/Ports & Adapters)

### Overview

The new architecture follows the **Hexagonal Architecture** (also known as **Ports and Adapters** or **Clean Architecture**):

```
┌────────────────────────────────────────────────────────────────┐
│                      PRIMARY ADAPTERS                          │
│              (API Controllers, Consumers, Workers)             │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                    INBOUND PORTS (Interfaces)                 │
│                      Use Case Interfaces                        │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                    APPLICATION CORE                             │
│                       USE CASES                                 │
│              (Business Logic - Pure C#)                        │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                   OUTBOUND PORTS (Interfaces)                  │
│              Repository & Messaging Interfaces                  │
└────────────────────────────────────────────────────────────────┘
                              │
                              ▼
┌────────────────────────────────────────────────────────────────┐
│                    SECONDARY ADAPTERS                          │
│            (Repositories, RabbitMQ Publishers/Consumers)         │
└────────────────────────────────────────────────────────────────┘
```

### Project Structure (Hexagonal)

```
ServiceName/
├── src/
│   ├── ServiceName.Domain/              # Core domain (no dependencies)
│   │   ├── Entities/
│   │   └── Enums/
│   │
│   ├── ServiceName.Application/         # Application layer
│   │   ├── Ports/
│   │   │   ├── Inbound/                # Use case interfaces
│   │   │   │   └── IUseCase.cs
│   │   │   └── Outbound/              # Repository/messaging interfaces
│   │   │       └── IRepository.cs
│   │   ├── UseCases/                   # Business logic (depends only on Domain)
│   │   │   └── UseCase.cs
│   │   └── Dtos/
│   │
│   ├── ServiceName.Infrastructure/      # Infrastructure adapters
│   │   ├── Persistence/
│   │   │   ├── DbContext.cs
│   │   │   └── Repositories/          # Repository implementations
│   │   └── Messaging/
│   │       ├── Publishers/             # RabbitMQ publishers
│   │       └── Consumers/             # RabbitMQ consumers
│   │
│   └── ServiceName.Api/                 # Entry point (Web API)
│       └── Program.cs                   # DI configuration
│
├── tests/
│   └── ServiceName.Application.Tests/  # Unit tests with mocks
│       └── UseCases/
│           └── UseCaseTests.cs
│
└── ServiceName.sln
```

### Example: Reservation Service (Hexagonal)

```
ReservationService/
├── src/
│   ├── ReservationService.Domain/
│   │   ├── Entities/
│   │   │   └── Ticket.cs
│   │   └── Enums/
│   │       └── TicketStatus.cs
│   │
│   ├── ReservationService.Application/
│   │   ├── Ports/
│   │   │   ├── Inbound/
│   │   │   │   └── IReserveTicketUseCase.cs
│   │   │   └── Outbound/
│   │   │       └── ITicketRepository.cs
│   │   ├── UseCases/
│   │   │   └── ReserveTicketUseCase.cs
│   │   └── Dtos/
│   │       └── ReservationMessageDto.cs
│   │
│   ├── ReservationService.Infrastructure/
│   │   ├── Persistence/
│   │   │   ├── TicketingDbContext.cs
│   │   │   └── TicketRepository.cs
│   │   └── Messaging/
│   │       ├── RabbitMQSettings.cs
│   │       └── TicketReservationConsumer.cs
│   │
│   └── ReservationService.Worker/
│       └── Program.cs
│
├── tests/
│   └── ReservationService.Application.Tests/
│       └── UseCases/
│           └── ReserveTicketUseCaseTests.cs
│
└── ReservationService.sln
```

### Benefits of Hexagonal Architecture

| Aspect | Benefit |
|--------|---------|
| **Loose Coupling** | Domain/Application don't depend on infrastructure |
| **Testability** | Easy to mock outbound ports for unit testing |
| **Flexibility** | Swap implementations (e.g., PostgreSQL → MySQL) |
| **Single Responsibility** | Clear separation of concerns |
| **Dependency Inversion** | Infrastructure depends on Application, not vice versa |
| **Framework Agnostic** | Business logic independent of EF Core, RabbitMQ |

### Dependency Flow

```
┌─────────────────────────────────────────────────────────────┐
│                    External World                            │
│              (HTTP, RabbitMQ, Database)                      │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    PRIMARY ADAPTERS                          │
│         (Controllers, Consumers, Workers - Entry)           │
│                  [Depends on Application]                    │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                    APPLICATION CORE                          │
│              [Depends only on Domain]                        │
│  ┌─────────────────────────────────────────────────────┐    │
│  │                 USE CASES                           │    │
│  │    (Business Logic - Pure C#, No Dependencies)     │    │
│  └─────────────────────────────────────────────────────┘    │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                      PORTS (Interfaces)                      │
│              [Domain defines interfaces]                     │
│  ┌──────────────────┐    ┌────────────────────────┐        │
│  │  Inbound Ports   │    │    Outbound Ports     │        │
│  │  (Use Cases)    │    │ (Repositories/Messaging)│        │
│  └──────────────────┘    └────────────────────────┘        │
└─────────────────────────┬───────────────────────────────────┘
                          │
                          ▼
┌─────────────────────────────────────────────────────────────┐
│                   SECONDARY ADAPTERS                         │
│              [Implements Outbound Ports]                    │
│  ┌──────────────────┐    ┌────────────────────────┐        │
│  │  Repositories   │    │   Messaging Adapters  │        │
│  │  (EF Core)      │    │   (RabbitMQ Client)   │        │
│  └──────────────────┘    └────────────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

---

## Comparison

| Feature | Legacy (Layered) | Hexagonal (Ports & Adapters) |
|---------|------------------|------------------------------|
| **Coupling** | High | Low |
| **Testability** | Difficult | Easy (mocks) |
| **Flexibility** | Low | High |
| **Dependencies** | Business → Infrastructure | Infrastructure → Business |
| **Database Access** | Direct (Repository) | Via Port Interface |
| **Messaging** | Direct (RabbitMQ Client) | Via Port Interface |
| **Swappable Parts** | Hard | Easy |
| **Onion Architecture** | No | Yes |
| **Unit Tests** | Complex setup | Simple mocks |

---

## Migration Status

### Completed ✅

| Service | Hexagonal Structure | Unit Tests |
|---------|-------------------|------------|
| **Reservation Service** | ✅ Complete | ✅ 14 tests |
| **Payment Service** | ✅ Complete | ✅ 12 tests |
| **Producer Service** | ✅ Complete | ✅ 17 tests |
| **CRUD Service** | ✅ Complete | ✅ 18 tests |

### Current Issues

1. **Duplicate Code**: Old architecture code still exists alongside new hexagonal code
2. **Docker References**: compose.yml needs updates to point to correct Dockerfiles
3. **Database Mapping**: PostgreSQL ENUM types require proper EF Core configuration

---

## Recommended Cleanup

### Files to Remove (Legacy Code)

#### ReservationService
```
src/ReservationService.Worker/
├── Data/                    # Duplicate - use Infrastructure/Persistence/
├── Consumers/               # Duplicate - use Infrastructure/Messaging/
├── Models/                  # Duplicate - use Domain/
├── Repositories/            # Duplicate - use Infrastructure/Persistence/
├── Services/               # Duplicate - use Application/UseCases/
└── Configurations/          # Duplicate - use Infrastructure/Messaging/
```

#### PaymentService
```
MsPaymentService.Worker/
├── Data/                    # Duplicate - use Infrastructure/Persistence/
├── Models/                  # Duplicate - use Domain/
├── Repositories/            # Duplicate - use Infrastructure/Persistence/
├── Services/                # Duplicate - use Application/UseCases/
├── Handlers/                # Duplicate - use Infrastructure/Messaging/
├── Configurations/          # Duplicate - use Infrastructure/
├── Messaging/               # Duplicate - use Infrastructure/Messaging/
└── Extensions/              # Duplicate - use Infrastructure/
```

#### Producer
```
Producer/
├── Producer/                # Old project structure
│   ├── Controllers/
│   ├── Services/
│   └── Repositories/
```

#### CRUD Service
```
CrudService/
├── Controllers/              # Should move to Api/
├── Services/               # Duplicate - use Application/UseCases/
├── Repositories/           # Duplicate - use Infrastructure/
└── Data/                   # Duplicate - use Infrastructure/
```

### Post-Cleanup Structure

Each service should have only:
```
ServiceName/
├── src/
│   ├── ServiceName.Domain/
│   ├── ServiceName.Application/
│   ├── ServiceName.Infrastructure/
│   └── ServiceName.Api/ (or Worker/)
├── tests/
│   └── ServiceName.Application.Tests/
└── ServiceName.sln
```

---

## Conclusion

The migration from **Legacy Layered Architecture** to **Hexagonal Architecture** provides:

1. **Better testability** through dependency inversion
2. **Loose coupling** between business logic and infrastructure
3. **Easier maintenance** with clear separation of concerns
4. **Framework independence** for core business logic
5. **Better scalability** - infrastructure can be swapped as needed

The key principle is: **Dependencies point inward**. The domain and application layers know nothing about infrastructure, making them easy to test and maintain.
