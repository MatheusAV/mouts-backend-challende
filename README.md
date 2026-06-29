# Developer Evaluation Project

`READ CAREFULLY`

## Use Case
**You are a developer on the DeveloperStore team. Now we need to implement the API prototypes.**

As we work with `DDD`, to reference entities from other domains, we use the `External Identities` pattern with denormalization of entity descriptions.

Therefore, you will write an API (complete CRUD) that handles sales records. The API needs to be able to inform:

* Sale number
* Date when the sale was made
* Customer
* Total sale amount
* Branch where the sale was made
* Products
* Quantities
* Unit prices
* Discounts
* Total amount for each item
* Cancelled/Not Cancelled

It's not mandatory, but it would be a differential to build code for publishing events of:
* SaleCreated
* SaleModified
* SaleCancelled
* ItemCancelled

If you write the code, **it's not required** to actually publish to any Message Broker. You can log a message in the application log or however you find most convenient.

### Business Rules

* Purchases above 4 identical items have a 10% discount
* Purchases between 10 and 20 identical items have a 20% discount
* It's not possible to sell above 20 identical items
* Purchases below 4 items cannot have a discount

These business rules define quantity-based discounting tiers and limitations:

1. Discount Tiers:
   - 4+ items: 10% discount
   - 10-20 items: 20% discount

2. Restrictions:
   - Maximum limit: 20 items per product
   - No discounts allowed for quantities below 4 items

## Overview
This section provides a high-level overview of the project and the various skills and competencies it aims to assess for developer candidates. 

See [Overview](/.doc/overview.md)

## Tech Stack
This section lists the key technologies used in the project, including the backend, testing, frontend, and database components. 

See [Tech Stack](/.doc/tech-stack.md)

## Frameworks
This section outlines the frameworks and libraries that are leveraged in the project to enhance development productivity and maintainability. 

See [Frameworks](/.doc/frameworks.md)

<!-- 
## API Structure
This section includes links to the detailed documentation for the different API resources:
- [API General](./docs/general-api.md)
- [Products API](/.doc/products-api.md)
- [Carts API](/.doc/carts-api.md)
- [Users API](/.doc/users-api.md)
- [Auth API](/.doc/auth-api.md)
-->

## Project Structure
This section describes the overall structure and organization of the project files and directories. 

See [Project Structure](/.doc/project-structure.md)

---

## Setup & Running

### Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [Docker](https://www.docker.com/products/docker-desktop) + Docker Compose

### 1. Run with Docker Compose (recommended)

```bash
cd template/backend
docker compose up --build
```

This starts:
- **Web API** on `http://localhost:8080`
- **PostgreSQL** on `localhost:5432`
- **MongoDB** on `localhost:27017`
- **Redis** on `localhost:6379`

Swagger UI: `http://localhost:8080/swagger`

### 2. Run locally (without Docker)

Ensure PostgreSQL is running, then update `appsettings.Development.json` with your connection string.

```bash
cd template/backend

# Restore packages
dotnet restore

# Apply migrations
dotnet ef database update \
  --project src/Ambev.DeveloperEvaluation.ORM \
  --startup-project src/Ambev.DeveloperEvaluation.WebApi

# Run API
dotnet run --project src/Ambev.DeveloperEvaluation.WebApi
```

### 3. Environment Variables

| Variable | Description | Default |
|----------|-------------|---------|
| `ConnectionStrings__DefaultConnection` | PostgreSQL connection string | See `appsettings.json` |
| `Jwt__SecretKey` | JWT signing key | See `appsettings.json` |

---

## Testing

```bash
cd template/backend

# Run all unit tests
dotnet test tests/Ambev.DeveloperEvaluation.Unit

# Run with coverage report
./coverage-report.sh   # Linux/Mac
coverage-report.bat    # Windows
```

---

## Sales API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `POST` | `/api/sales` | Create a new sale |
| `GET` | `/api/sales` | List sales (paginated, `?page=1&pageSize=10`) |
| `GET` | `/api/sales/{id}` | Get sale by ID |
| `PUT` | `/api/sales/{id}` | Update a sale |
| `DELETE` | `/api/sales/{id}` | Delete a sale |
| `PATCH` | `/api/sales/{id}/cancel` | Cancel a sale |
| `PATCH` | `/api/sales/{saleId}/items/{itemId}/cancel` | Cancel a sale item |

### Example: Create Sale

```json
POST /api/sales
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

Response includes auto-calculated discount (20% for 10 items) and total amount.

### Business Rules Applied Automatically

| Quantity | Discount |
|----------|----------|
| 1–3 | 0% |
| 4–9 | 10% |
| 10–20 | 20% |
| > 20 | Error |

---

## Domain Events (logged)

Events are published via MediatR and logged to the application log:

- `SaleCreated` — on sale creation
- `SaleModified` — on sale update
- `SaleCancelled` — on sale cancellation
- `ItemCancelled` — on item cancellation
