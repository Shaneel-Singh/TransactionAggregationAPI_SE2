# Transaction Aggregation API

.NET 8 REST API that aggregates transaction data from multiple mock sources, automatically categorizes transactions using a keyword strategy, and exposes a queryable, paginated read surface backed by PostgreSQL and Redis.

## Quick Start

```bash
# Copy and configure environment
cp .env.example .env
# Edit .env — set API keys, DB connection, Redis, CORS origins

# Start all services (API on :8080, PostgreSQL, Redis)
docker compose up --build

# Verify readiness
curl http://localhost:8080/health/ready

# Trigger aggregation from all three sources
curl -X POST http://localhost:8080/api/transactions/aggregate \
  -H "X-API-Key: your-api-key-here"

# Query transactions
curl "http://localhost:8080/api/transactions?pageSize=10&sortBy=date&sortOrder=desc" \
  -H "X-API-Key: your-api-key-here"

# Browse interactive API docs (no auth required)
open http://localhost:8080/swagger
```

## Stack

| Layer | Technology |
|---|---|
| Runtime | .NET 8 LTS |
| Database | PostgreSQL 16 + EF Core 8 |
| Cache | Redis 7 (StackExchange.Redis) |
| Resilience | Polly v8 |
| Validation | FluentValidation |
| Logging | Serilog (structured, correlation ID enrichment) |
| API Docs | Swagger / OpenAPI (Swashbuckle 6.9) |
| Containerisation | Docker multi-stage + Compose |

## API Reference

Full interactive documentation is available at `GET /swagger` — no API key required.

### Health (no auth)
| Method | Path | Description |
|---|---|---|
| `GET` | `/health` | Basic liveness |
| `GET` | `/health/live` | Liveness probe |
| `GET` | `/health/ready` | Readiness — checks DB + Redis |
| `GET` | `/health/metrics` | Cache hit/miss/invalidation stats |

### Transactions (`X-API-Key` header required)
| Method | Path | Description |
|---|---|---|
| `POST` | `/api/transactions/aggregate` | Fetch from all sources, categorize, upsert |
| `GET` | `/api/transactions` | List all transactions (paginated, filtered) |
| `GET` | `/api/transactions/{id}` | Get by ID |
| `POST` | `/api/transactions` | Create transaction manually |
| `DELETE` | `/api/transactions/{id}` | Soft delete |
| `GET` | `/api/transactions/customer/{id}` | Customer transactions (paginated, filtered) |
| `GET` | `/api/transactions/customer/{id}/summary` | Aggregated summary with category breakdown |

### Query Parameters (list endpoints)
| Parameter | Values | Description |
|---|---|---|
| `page` | ≥ 1 | Page number (default: 1) |
| `pageSize` | 1–100 | Page size, capped at 100 |
| `sortBy` | `date`, `amount`, `category`, `merchant` | Sort field (default: date) |
| `sortOrder` | `asc`, `desc` | Sort direction (default: desc) |
| `category` | `Food`, `Transport`, `Entertainment`, … | Filter by category |
| `from` | ISO 8601 | Start of date range |
| `to` | ISO 8601 | End of date range |

## Environment Variables

All secrets are supplied via environment variables — nothing is hardcoded in source. The application fails fast at startup if any required variable is missing.

```bash
# Required (see .env.example for full format)
ConnectionStrings__Postgres=Host=postgres;Database=transactions;Username=...;Password=...
Redis__ConnectionString=redis:6379
ApiKeys__0=your-first-api-key
ApiKeys__1=your-second-api-key
Cors__AllowedOrigins__0=https://yourfrontend.example.com
```

## Running Tests

```bash
dotnet test                        # all 136 tests (111 unit + 25 integration)
dotnet test tests/UnitTests        # unit tests only (~250ms)
dotnet test tests/IntegrationTests # integration tests (WebApplicationFactory)
```

Test coverage by layer:
- **Unit (111)**: aggregation service, transaction service (cache-aside, page cap, soft delete), categorization strategy (all 14 categories), source adapters (date parsing, field mapping), FluentValidation rules, API key middleware
- **Integration (25)**: full aggregation → query flow, pagination, category filtering, customer summary, auth enforcement on all endpoints

## Project Structure

```
src/
├── API/
│   ├── Controllers/          # TransactionsController, AggregationController, HealthController
│   ├── Middleware/           # GlobalExceptionMiddleware, CorrelationIdMiddleware, ApiKeyAuthMiddleware
│   ├── Models/               # Request/response DTOs
│   ├── Validators/           # FluentValidation rules
│   └── Extensions/           # ServiceCollectionExtensions (DI wiring)
├── Application/
│   ├── Domain/               # UnifiedTransaction, TransactionCategory, AuditLog
│   ├── Interfaces/           # ITransactionRepository, ITransactionService, IAggregationService, …
│   ├── Services/             # AggregationService, TransactionService
│   └── Categorization/       # ICategorizationStrategy, KeywordCategorizationStrategy, CategorizationService
└── Infrastructure/
    ├── Data/                 # TransactionDbContext, EF Core configurations, TransactionRepository
    ├── Cache/                # RedisCacheService (cache-aside, TTL, graceful degradation)
    ├── Resilience/           # PollyPolicies (timeout + retry + circuit breaker)
    └── Sources/              # SourceAAdapter, SourceBAdapter, SourceCAdapter

tests/
├── UnitTests/                # Moq + FluentAssertions, no I/O
└── IntegrationTests/         # WebApplicationFactory, EF InMemory
```

## Design Decisions

### Authentication: API key over JWT

API key authentication was chosen over JWT for this service because it aggregates from internal mock sources and exposes data to internal consumers — the caller identity is a system, not a user. API keys are:
- Simpler to rotate and revoke (update the env var, no key distribution infrastructure)
- Appropriate for server-to-server communication
- Supplied via env vars so never committed to source

**Trade-off**: No per-user resource ownership enforcement. Any valid API key can read any customer's data. For a multi-tenant production system, the natural extension is JWT with a `customerId` claim validated against the requested resource at the service layer.

---

### Adapter pattern for source ingestion

Each mock source has a distinct payload shape and date format — SourceA uses ISO 8601 flat JSON, SourceB uses Unix timestamps in nested JSON, SourceC uses dd/MM/yyyy with D/C debit/credit flags. Rather than branching on source type in a single ingest method, each source gets its own adapter implementing `ITransactionSource`. New sources are added by creating a new adapter and registering it in DI — the `AggregationService` iterates `IEnumerable<ITransactionSource>` and is unaware of how many sources exist.

---

### Idempotent upsert on (ExternalId, SourceSystem)

`AggregateAsync` can be called multiple times. Rather than duplicating transactions on repeated runs, the repository checks the composite unique key `(ExternalId, SourceSystem)`. Existing records are updated (amount, description, category, raw payload); new records are inserted. A database-level unique index enforces this constraint — the application logic mirrors it. This makes repeated aggregation safe without needing a deduplication step in the caller.

---

### DB-level aggregation for customer summary

`GetSummaryAsync` uses two server-side EF Core queries — a `GroupBy(_ => 1)` aggregate for scalar totals (Count, Sum, Avg, Max, Min, date range), and a `GroupBy(t => t.Category)` for the per-category breakdown — rather than loading all rows into memory. The composite index `(CustomerId, TransactionDateUtc)` ensures both queries use an efficient index seek. A customer with 100,000 transactions produces two SQL aggregate queries, not 100,000 rows transferred over the wire.

---

### Polly resilience pipeline for source adapters

Source adapters wrap their HTTP fetch in a Polly pipeline with three layers applied in order:
1. **Timeout (10s)** — prevents a slow source from holding a connection indefinitely
2. **Retry (3 attempts, exponential backoff + jitter)** — handles transient failures without thundering herd
3. **Circuit breaker (50% failure ratio, 30s break)** — stops retrying a source that is persistently down, so one failing source does not degrade the entire aggregation run

Sources that fail after all retries are recorded in `AggregationResult.SourceResults` with their error message and duration. Aggregation continues with the remaining sources — partial results are returned rather than failing the entire request.

---

### Redis cache-aside with graceful degradation

`TransactionService.GetByIdAsync` follows cache-aside: check Redis, fall back to the database on a miss, then populate the cache. Cache invalidation is explicit on create, update, and soft delete — invalidating both the per-transaction key and the affected customer's list and summary keys. If Redis is unavailable, the cache service catches the exception, logs a warning, and returns null — the application degrades to database-only reads without throwing.

---

### Soft delete over hard delete

Transactions are never physically deleted. The `IsDeleted` flag with `DeletedAtUtc` and `DeletedBy` fields, combined with a global EF Core query filter, ensures deleted records are excluded from all reads transparently. The audit trail is preserved for regulatory or debugging purposes. `IgnoreQueryFilters()` is used in the upsert path to detect previously soft-deleted records with the same `(ExternalId, SourceSystem)` key.

---

### Fail-fast startup validation

`ValidateRequiredConfig` runs before the host starts and throws `InvalidOperationException` listing all missing keys if any required configuration (`ConnectionStrings:Postgres`, `Redis:ConnectionString`, `ApiKeys`, `Cors:AllowedOrigins`) is absent. This surfaces misconfiguration immediately at startup rather than as a first-request runtime failure, which is easier to diagnose in container environments.

---

## Known Trade-offs

| Area | Current approach | Production extension |
|---|---|---|
| Auth | API key (system identity) | JWT with `customerId` claim + resource ownership check |
| Integration tests | EF Core InMemory | Testcontainers (real PostgreSQL) for constraint and index testing |
| Audit log | Schema exists, not populated | Write on create/update/delete with `CorrelationId` from middleware |
| Categorization | Keyword matching, single strategy | ML-based scoring, multi-strategy with confidence threshold |
| Observability | Serilog + structured logs | OpenTelemetry traces + Prometheus metrics + Grafana dashboards |
