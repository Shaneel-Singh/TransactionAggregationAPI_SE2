# Transaction Aggregation API

.NET 8 REST API that aggregates transaction data from multiple mock sources and categorizes them automatically.

## Quick Start

```bash
# Set up environment
cp .env.example .env
# Edit .env with your API keys

# Start services (API on port 8080)
docker compose up --build

# Health check
curl http://localhost:8080/health/ready

# Trigger aggregation
curl -X POST http://localhost:8080/api/transactions/aggregate \
  -H "X-API-Key: your-api-key-here"

# Query transactions
curl "http://localhost:8080/api/transactions?pageSize=10" \
  -H "X-API-Key: your-api-key-here"
```

## Stack

- .NET 8 LTS
- PostgreSQL 16 + EF Core 8
- Redis (caching)
- Polly v8 (resilience)
- FluentValidation
- Serilog

## API Endpoints

### Health (no auth)
- `GET /health` - basic check
- `GET /health/ready` - DB + Redis check
- `GET /health/metrics` - cache stats

### Transactions (X-API-Key required)
- `POST /api/transactions/aggregate` - fetch from all sources
- `GET /api/transactions` - list all (paginated)
- `GET /api/transactions/{id}` - get by ID
- `POST /api/transactions` - create manually
- `DELETE /api/transactions/{id}` - soft delete
- `GET /api/transactions/customer/{id}` - customer transactions
- `GET /api/transactions/customer/{id}/summary` - customer summary

### Query params
- `page`, `pageSize` (max 100)
- `sortBy` (date|amount|category|merchant), `sortOrder` (asc|desc)
- `category` (Food|Transport|Entertainment|etc)
- `from`, `to` (ISO 8601 dates)

## Environment Variables

Required variables (see `.env.example`):
- `ConnectionStrings__Postgres` - PostgreSQL connection string
- `Redis__ConnectionString` - Redis connection
- `ApiKeys__0`, `ApiKeys__1` - API keys (add more as needed)
- `Cors__AllowedOrigins__0` - CORS origins

## Tests

```bash
dotnet test                      # all tests (138 total: 112 unit + 26 integration)
dotnet test tests/UnitTests      # unit tests only
dotnet test tests/IntegrationTests  # integration tests only
```

## Project Structure

```
src/
├── API/              # Controllers, middleware, validators
├── Application/      # Business logic
└── Infrastructure/   # EF Core, Redis, Polly, source adapters
tests/
├── UnitTests/        # Fast, isolated
└── IntegrationTests/ # WebApplicationFactory
```

## Notes

- All secrets via environment variables (no hardcoded values)
- Soft delete on all transactions
- Rate limiting: 100 req/min per IP
- Multi-stage Docker build with non-root user
- See `docs/requests.http` for example requests
