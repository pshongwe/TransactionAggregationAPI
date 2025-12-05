# 🚀 TransactionAggregationAPI

![Coverage](https://pshongwe.github.io/TransactionAggregationAPI/coverage.svg)

A **.NET 8** service that ingests customer transactions from heterogeneous upstream providers, normalizes them into a shared domain model, categorizes spending, and exposes a REST API for querying raw transactions or category summaries.

---

## Table of Contents
1. [Project Overview](#project-overview)
2. [Architecture](#architecture)
3. [Getting Started](#getting-started)
4. [Running Locally](#running-locally)
5. [API Surface](#api-surface)
6. [Testing & Coverage](#testing--coverage)
7. [Deployment](#deployment)
8. [Project Structure](#project-structure)
9. [Contributing](#contributing)
10. [Author](#author)

---

## Project Overview
- **Goal:** Provide a single API that hides the quirks of multiple financial data feeds while surfacing categorized insights.
- **Tech Stack:** ASP.NET Core 8, FluentAssertions, xUnit, Moq, Fly.io, GitHub Actions, ReportGenerator.
- **Key Capabilities:**
  - Adapter layer for each upstream payload shape (JSON array/object variants).
  - Deterministic keyword-based categorizer over a unified transaction record.
  - Filtering by date range plus per-category aggregation endpoints.
  - Comprehensive unit, mapping, adapter, and controller integration tests.

---

## Architecture

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

**Why this design?** Clear layering decouples upstream data contracts from domain logic, keeps the API surface stable, and makes new sources or business rules easy to add. Everything is async-first for non-blocking I/O and fully testable via isolated adapters plus controller integration tests.

---

## Getting Started
1. **Prerequisites**
   - .NET SDK 8.0 (`dotnet --list-sdks` to confirm)
   - Podman or Docker for container-based workflows
   - Fly.io CLI (`brew install flyctl`) if deploying
2. **Clone & Restore**
   ```bash
   git clone https://github.com/pshongwe/TransactionAggregationAPI.git
   cd TransactionAggregationAPI
   dotnet restore
   ```
3. **Configuration**
   - Use the supplied `TransactionAggregation.Api/appsettings.Development.json` for local runs.
   - Override connection strings or source locations via environment variables or additional `appsettings.{Environment}.json` files.
   - Mock transaction payloads live in `TransactionAggregation.Api/MockData/` and are copied automatically into the test output.

---

## Running Locally
- **Using the .NET SDK**
  ```bash
  dotnet run --project TransactionAggregation.Api/TransactionAggregation.Api.csproj
  ```
  The service listens on `http://localhost:5196` by default (or `http://0.0.0.0:8080` inside containers).

- **Using Podman/Docker (no SDK required)**
  ```bash
  podman run -it --rm \
    -v $(pwd):/src \
    -w /src \
    mcr.microsoft.com/dotnet/sdk:8.0 \
    dotnet run --project TransactionAggregation.Api/TransactionAggregation.Api.csproj
  ```

- **Container Image (Fly/Render compatible)**
  ```bash
  podman build -t transaction-aggregation-api .
  podman run -p 8080:8080 transaction-aggregation-api
  ```

---

## API Surface

### `GET /customers`
Simple health probe used by Render/Fly.io.

### `GET /customers/{customerId}/transactions`
Returns normalized transactions for a customer; supports optional `from`/`to` ISO8601 query parameters for date filtering.

### `GET /customers/{customerId}/transactions/summary`
Aggregates totals per category:

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

## Testing & Coverage
- **Standard run**
  ```bash
  dotnet test
  ```
- **Containerized tests (no local SDK)**
  ```bash
  podman run -it --rm \
    -v $(pwd):/src \
    -w /src \
    mcr.microsoft.com/dotnet/sdk:8.0 \
    dotnet test
  ```
- **Coverage artifacts**
  - Collect via coverlet/ReportGenerator: `dotnet test /p:CollectCoverage=true /p:CoverletOutputFormat=cobertura`
  - Browse the committed sample report at `coverage-report/index.html` or regenerate locally with `./serve-coverage.sh` then open the file in a browser.
- **Docker Compose helper**
  - `docker-compose.test.yml` mirrors the Podman workflow: it mounts the repo into the official .NET 8 SDK image and runs `dotnet restore` plus `dotnet test` in a clean container.
  - Useful when you want to verify tests in the same environment as CI or when the SDK is not installed locally.
  - Run it with:
    ```bash
    docker compose -f docker-compose.test.yml up --abort-on-container-exit
    ```
    or, if you prefer Podman Compose:
    ```bash
    podman compose -f docker-compose.test.yml up --abort-on-container-exit
    ```
    The command exits after the test container finishes and returns the same pass/fail status as the test suite.
- CI runs the same `dotnet test` suite plus coverage publishing; see `.github/workflows/ci.yml`.
> **Note:** Mock failure scenarios inside `TransactionAggregation.Tests` are purely illustrative—they simulate how the API would react once real upstream service calls are wired up, but no live services are invoked during tests.

---

## Deployment
- **Fly.io**
  ```bash
  flyctl deploy
  ```
  Requires `FLY_API_TOKEN` (set locally or as a GitHub Actions secret). The container binds to `0.0.0.0:8080` as defined in `fly.toml`.
- **Other targets**
  - `Dockerfile` is multi-stage and ready for any OCI platform.
  - `render.yaml` includes a Render blueprint if you prefer that hosting provider.

---

## Project Structure
```
TransactionAggregationAPI/
├── TransactionAggregation.Api/
│     ├── Controllers/
│     ├── Adapters/
│     ├── DTOs/
│     ├── Mapping/
│     ├── MockData/
│     └── Program.cs
├── TransactionAggregation.Domain/
│     ├── Models/
│     ├── Abstractions/
│     ├── Services/
├── TransactionAggregation.Tests/
│     ├── Adapters/
│     ├── Aggregation/
│     ├── Integration/
│     ├── Mapping/
│     ├── TestServer/
├── Dockerfile
├── fly.toml
├── render.yaml
└── README.md
```

---

## Contributing
- Fork the repo, create feature branches from `main`, and prefer conventional commit messages.
- Stay within the existing architecture: adapters should remain thin and deterministic; domain models stay immutable records.
- Add or update tests for every behavioral change (`TransactionAggregation.Tests`). Run `dotnet test` before opening a PR.
- Confirm CI passes and that coverage does not regress. If coverage drops, regenerate reports with `ReportGenerator` and update docs when necessary.
- Describe API or config changes in the PR body; include screenshots or `curl` samples when modifying endpoints.

---

## Author
**Evans Shongwe** — Full-stack & backend engineer (South Africa)