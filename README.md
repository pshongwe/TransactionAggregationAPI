# 🚀 TransactionAggregationAPI

![Coverage](https://pshongwe.github.io/TransactionAggregationAPI/coverage.svg)

A **production-grade .NET 8 service** that aggregates customer financial transactions from multiple heterogeneous data sources, normalizes them into a unified domain model, categorizes transactions, and exposes a clean REST API for querying aggregated financial information.

---

## 🧱 Architecture Overview

```
┌──────────────────────────────────────────────────────────────┐
│                       TransactionAggregationAPI              │
├──────────────────────────────────────────────────────────────┤
│ Controllers ➜ DTOs ➜ Mappers                                 │
│        ↓                                                     │
│ Aggregation Service (core domain logic)                      │
│        ↓                                                     │
│ Source Adapters:                                             │
│   • ASourceAdapter – JSON array { cust, txn_id, ... }        │
│   • BSourceAdapter – JSON array { customer, id, ... }        │
│   • CSourceAdapter – JSON object with nested "entries"       │
│        ↓                                                     │
│ Domain Model: UnifiedTransaction                             │
│ Categorizer: deterministic keyword-based categories          │
└──────────────────────────────────────────────────────────────┘
```

### Why this design?

- Decouples data sources from domain logic  
- Makes adding new sources effortless  
- Ensures consistent transaction shape  
- Fully testable with isolated adapters  
- Clean layering for API, domain, integration, and adapters  
- Async-first design (non-blocking I/O)

---

## 📡 API Endpoints

### `GET /`  
Health check.

### `GET /customers/{customerId}/transactions`  
Returns normalized & categorized transactions for a customer.

Optional query params:

```
?from=2024-01-01&to=2024-12-31
```

### `GET /customers/{customerId}/transactions/summary`  
Returns aggregated totals per category:

```json
[
  {
    "category": "Food",
    "totalAmount": -1420.50,
    "transactionCount": 12
  }
]
```

---

## 🧪 Testing & Coverage

Test coverage includes:

- Adapter tests validating JSON → UnifiedTransaction  
- Aggregation service behavior  
- Date filtering  
- Categorization  
- DTO mapping tests  
- Controller integration tests via WebApplicationFactory  
- AutoFixture + Moq randomization  
- Coverage generation (Cobertura + HTML)

### Generate local coverage (Podman / Docker):

```bash
./serve-coverage.sh
open coverage-report/index.html
```

---

## ⚙️ CI/CD Pipeline

GitHub Actions performs:

- Build  
- Test  
- Coverage generation  
- Publishing a static coverage badge (GitHub Pages)  
- Automatic deployment to Fly.io  

Workflow file:  
```
.github/workflows/ci.yml
```

Coverage badge URL (GitHub Pages):

```
https://pshongwe.github.io/TransactionAggregationAPI/coverage.svg
```

---

## ☁️ Fly.io Deployment

Fly.io is used to deploy the containerized API.

### Manual deployment:

```bash
flyctl deploy
```

### Required secret in GitHub Actions:

```
FLY_API_TOKEN
```

### Port binding

The app listens on:

```
http://0.0.0.0:8080
```

Fly automatically routes traffic to the container.

---

## 🗂 Project Structure

```
TransactionAggregationAPI/
│
├── TransactionAggregation.Api/
│     ├── Controllers/
│     ├── Adapters/
│     ├── DTOs/
│     ├── Mapping/
│     ├── MockData/
│     └── Program.cs
│
├── TransactionAggregation.Domain/
│     ├── Models/
│     ├── Abstractions/
│     ├── Services/
│
├── TransactionAggregation.Tests/
│     ├── Adapters/
│     ├── Aggregation/
│     ├── Integration/
│     ├── Mapping/
│     ├── TestServer/
│
├── Dockerfile
├── fly.toml
└── README.md
```

---

## 🧩 Design Patterns & Principles

### **Adapter Pattern**  
Each external source has its own adapter to handle unique formats.

### **Domain Model**  
`UnifiedTransaction` ensures consistency across all downstream logic.

### **DTO + Mapping Layer**  
Decouples API responses from domain objects.

### **Immutable Records**  
Domain models are immutable, simplifying reasoning and testing.

### **Async-first architecture**  
All adapters and services use `async/await` for non-blocking I/O.

---

## 🔍 Possible Future Enhancements

- Add persistence (PostgreSQL / SQLite)  
- Add message-based ingestion (Kafka, RabbitMQ)  
- Replace deterministic categorizer with ML-based classification  
- Support gRPC or streaming endpoints  
- Add caching (Redis / MemoryCache)  
- Add OpenAPI documentation via Swashbuckle  

---

## 🙌 Author

**Evans Shongwe**  
Full-stack & backend engineer — South Africa  
Passionate about clean architecture and high-quality engineering.
