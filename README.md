# DeveloperStore — Sales API

> Backend challenge implementing a complete **Sales API** built on top of an ASP.NET Core 8 template, following **DDD**, **CQRS**, **SOLID** principles and the **External Identities** pattern.

---

## Table of Contents

- [Architecture](#architecture)
- [Tech Stack](#tech-stack)
- [Business Rules](#business-rules)
- [Getting Started](#getting-started)
- [API Reference](#api-reference)
- [Domain Events](#domain-events)
- [Testing](#testing)
- [Project Structure](#project-structure)
- [SOLID Principles Applied](#solid-principles-applied)

---

## Architecture

The solution is organized in four layers following **Domain-Driven Design**:

```
┌─────────────────────────────────────────────┐
│              WebAPI (Presentation)           │
│   Controllers · DTOs · Profiles · Middleware │
├─────────────────────────────────────────────┤
│           Application (Use Cases)            │
│   Commands · Queries · Handlers · Events     │
├─────────────────────────────────────────────┤
│              Domain (Core)                   │
│   Entities · Aggregates · Repositories       │
│   Services · Domain Events · Value Objects   │
├─────────────────────────────────────────────┤
│         Infrastructure (ORM / IoC)           │
│   EF Core · PostgreSQL · Repositories · DI   │
└─────────────────────────────────────────────┘
```

**Key design decisions:**

- **CQRS** via MediatR — commands and queries are fully decoupled
- **External Identities** — Customer and Branch are denormalized on the Sale aggregate (no cross-domain FK)
- **Strategy Pattern** (`IDiscountStrategy`) — discount tiers are swappable without touching the domain entity *(OCP)*
- **Domain Events** — `SaleCreated`, `SaleModified`, `SaleCancelled`, `ItemCancelled` published via MediatR and logged via `ILogger`
- **Repository Pattern** — `ISaleRepository` abstracts all persistence concerns
- **FluentValidation** injected as `IValidator<T>` *(DIP)*

---

## Tech Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 / C# 12 |
| Web Framework | ASP.NET Core Web API |
| ORM | Entity Framework Core 8 + Npgsql |
| Database | PostgreSQL 13 |
| Mediator | MediatR 12 |
| Mapping | AutoMapper 13 |
| Validation | FluentValidation 11 |
| Documentation | Swashbuckle / Swagger UI |
| Logging | Serilog |
| Testing | xUnit · FluentAssertions · NSubstitute |
| Infrastructure | Docker · Docker Compose |
| Cache | Redis 7 |
| NoSQL | MongoDB 8 |

---

## Business Rules

Discounts are applied **automatically** based on quantity per identical item:

| Quantity | Discount |
|---|---|
| 1 – 3 items | 0% |
| 4 – 9 items | 10% |
| 10 – 20 items | 20% |
| > 20 items | ❌ Error — not allowed |

Rules are enforced in `QuantityDiscountStrategy` (implements `IDiscountStrategy`) and applied in `SaleItem.ApplyDiscount()`.

---

## Getting Started

### Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop) (recommended)
- **or** [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) + PostgreSQL

### Run with Docker (recommended)

```bash
cd template/backend
docker compose up --build
```

Services started:

| Service | URL |
|---|---|
| Web API | http://localhost:8080 |
| Swagger UI | http://localhost:8080/swagger |
| PostgreSQL | localhost:5432 |
| MongoDB | localhost:27017 |
| Redis | localhost:6379 |

> Database migrations are applied automatically on startup.

### Run locally

```bash
cd template/backend

dotnet restore

# Apply migrations
dotnet ef database update \
  --project src/Ambev.DeveloperEvaluation.ORM \
  --startup-project src/Ambev.DeveloperEvaluation.WebApi

dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

Update `appsettings.Development.json` with your PostgreSQL connection string beforehand.

### Environment variables

| Variable | Description |
|---|---|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string |
| `Jwt__SecretKey` | JWT signing secret (min. 32 chars) |

---

## API Reference

Full interactive docs available at **`http://localhost:8080/swagger`** after running the project.

### Sales endpoints

| Method | Endpoint | Description |
|---|---|---|
| `POST` | `/api/sales` | Create a new sale |
| `GET` | `/api/sales` | List sales (paginated) |
| `GET` | `/api/sales/{id}` | Get sale by ID |
| `PUT` | `/api/sales/{id}` | Update a sale |
| `DELETE` | `/api/sales/{id}` | Delete a sale |
| `PATCH` | `/api/sales/{id}/cancel` | Cancel a sale |
| `PATCH` | `/api/sales/{saleId}/items/{itemId}/cancel` | Cancel a single item |

### Create sale — request body

```json
POST /api/sales
Authorization: Bearer <token>

{
  "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
  "customerName": "ACME Corp",
  "branchId": "3fa85f64-5717-4562-b3fc-2c963f66afa7",
  "branchName": "Main Branch",
  "saleDate": "2024-12-01T10:00:00Z",
  "items": [
    {
      "productId": "3fa85f64-5717-4562-b3fc-2c963f66afa8",
      "productName": "Widget Pro",
      "quantity": 10,
      "unitPrice": 29.90
    }
  ]
}
```

### Create sale — response

```json
{
  "success": true,
  "message": "Venda criada com sucesso",
  "data": {
    "id": "a1b2c3d4-...",
    "saleNumber": "SALE-20241201-A1B2C3D4",
    "totalAmount": 239.20,
    "items": [
      {
        "productName": "Widget Pro",
        "quantity": 10,
        "unitPrice": 29.90,
        "discount": 0.20,
        "totalAmount": 239.20
      }
    ]
  }
}
```

### List sales — query params

```
GET /api/sales?page=1&pageSize=10
```

### Error responses

| Status | Meaning |
|---|---|
| `400` | Validation error (invalid input) |
| `401` | Unauthorized (missing or invalid JWT) |
| `404` | Sale or item not found |
| `422` | Domain rule violation (e.g. quantity > 20) |

---

## Domain Events

Events are published via **MediatR** `INotification` and handled by dedicated handlers that log the occurrence using `ILogger`.

| Event | Trigger |
|---|---|
| `SaleCreatedEvent` | Sale successfully created |
| `SaleModifiedEvent` | Sale updated |
| `SaleCancelledEvent` | Sale cancelled |
| `ItemCancelledEvent` | A single item cancelled |

Each event handler is a separate class *(SRP)* and can be extended to publish to a message broker without modifying domain logic.

---

## Testing

```bash
cd template/backend

# Unit tests
dotnet test tests/Ambev.DeveloperEvaluation.Unit

# With coverage report
coverage-report.bat    # Windows
./coverage-report.sh   # Linux / macOS
```

Test coverage includes:

- `SaleTests` — discount rules, cancel, total recalculation, domain exceptions
- `CreateSaleHandlerTests` — handler orchestration, validation, discount strategy injection

---

## Project Structure

```
template/backend/
├── src/
│   ├── Ambev.DeveloperEvaluation.Domain/
│   │   ├── Entities/           # Sale, SaleItem (aggregates)
│   │   ├── Events/             # SaleCreated, SaleModified, SaleCancelled, ItemCancelled
│   │   ├── Repositories/       # ISaleRepository
│   │   └── Services/           # IDiscountStrategy, ISaleNumberGenerator
│   ├── Ambev.DeveloperEvaluation.Application/
│   │   └── Sales/              # CQRS handlers, commands, queries, profiles, event handlers
│   ├── Ambev.DeveloperEvaluation.ORM/
│   │   ├── Mapping/            # EF Core entity configurations
│   │   ├── Repositories/       # SaleRepository implementation
│   │   └── Migrations/         # Database migrations
│   ├── Ambev.DeveloperEvaluation.IoC/
│   │   └── ModuleInitializers/ # Dependency injection registration
│   └── Ambev.DeveloperEvaluation.WebApi/
│       ├── Features/Sales/     # Controller, DTOs, request validators, profiles
│       └── Middleware/         # Global exception handler
└── tests/
    └── Ambev.DeveloperEvaluation.Unit/
        ├── Domain/             # SaleTests
        └── Application/        # CreateSaleHandlerTests
```

---

## SOLID Principles Applied

| Principle | Implementation |
|---|---|
| **SRP** | Each handler, event handler, and service has a single, well-defined responsibility |
| **OCP** | `IDiscountStrategy` allows new discount tiers without modifying `SaleItem` |
| **LSP** | All repository and strategy implementations are fully substitutable |
| **ISP** | `ISaleRepository` exposes only what consumers need |
| **DIP** | Handlers depend on `IValidator<T>`, `IDiscountStrategy`, `ISaleNumberGenerator` — never on concrete types |
