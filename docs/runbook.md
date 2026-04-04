# Operations Runbook

## Prerequisites

- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (with Docker Compose v2)
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for local development)

## Quick Start

```bash
# 1. Clone the repository
git clone https://github.com/harshil-sh/microservices-reference-arch.git
cd microservices-reference-arch

# 2. Copy and configure environment variables
cp .env.example .env
# Edit .env and set secure passwords

# 3. Start all services
docker compose up -d --build

# 4. Verify health
curl http://localhost:8080/health    # Gateway
curl http://localhost:5001/health    # Orders API (dev only)
curl http://localhost:5002/health    # Inventory API (dev only)
```

## Service URLs

| Service | URL | Notes |
|---------|-----|-------|
| Gateway | http://localhost:8080 | Single entry point (production) |
| Orders API | http://localhost:5001 | Direct access (development only) |
| Inventory API | http://localhost:5002 | Direct access (development only) |
| RabbitMQ UI | http://localhost:15672 | Credentials: `guest/guest` |
| Seq UI | http://localhost:8081 | Log aggregator dashboard |

## API Endpoints

### Orders (via Gateway)
```
GET    http://localhost:8080/api/orders          # List all orders
GET    http://localhost:8080/api/orders/{id}     # Get order by ID
POST   http://localhost:8080/api/orders          # Place a new order
```

### Inventory (via Gateway)
```
GET    http://localhost:8080/api/inventory        # List all inventory items
GET    http://localhost:8080/api/inventory/{id}   # Get inventory item by ID
```

### Sample Request — Place Order
```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "customerId": "3fa85f64-5717-4562-b3fc-2c963f66afa6",
    "items": [
      {
        "productId": "a1b2c3d4-e5f6-7890-abcd-ef1234567890",
        "productName": "Widget",
        "quantity": 2,
        "unitPrice": 19.99
      }
    ]
  }'
```

## Observability

### Logs
- **Console**: Each service outputs structured logs to stdout
- **Seq**: All logs forwarded via OTLP to Seq at http://localhost:8081
- **Format**: `[HH:mm:ss LVL] ServiceName | Message`
- **Correlation**: Every event carries a `CorrelationId` for distributed tracing

### Traces
- OpenTelemetry traces exported to Seq via OTLP (HTTP/Protobuf)
- ASP.NET Core, HttpClient, and MassTransit instrumentation included
- View in Seq → Traces tab

### Metrics
Custom business metrics exported to Seq:

| Meter | Metric | Type | Description |
|-------|--------|------|-------------|
| `Orders.API` | `orders.placed.total` | Counter | Orders placed |
| `Orders.API` | `orders.confirmed.total` | Counter | Orders confirmed |
| `Orders.API` | `orders.failed.total` | Counter | Orders failed |
| `Orders.API` | `orders.total_amount` | Histogram | Order amount distribution (USD) |
| `Inventory.API` | `inventory.stock_reservations.total` | Counter | Successful stock reservations |
| `Inventory.API` | `inventory.stock_insufficient.total` | Counter | Stock insufficient events |
| `Inventory.API` | `inventory.stock_rollbacks.total` | Counter | Partial reservation rollbacks |
| `Notifications.Worker` | `notifications.sent.total` | Counter | Notifications dispatched (tagged by `channel`) |

## Database Management

### Migrations
EF Core code-first migrations run automatically on startup. To create a new migration:

```bash
# Orders
dotnet ef migrations add <MigrationName> \
  --project src/Services/Orders/Orders.Infrastructure \
  --startup-project src/Services/Orders/Orders.API

# Inventory
dotnet ef migrations add <MigrationName> \
  --project src/Services/Inventory/Inventory.Infrastructure \
  --startup-project src/Services/Inventory/Inventory.API
```

### Connection Strings
- **Local dev**: `(localdb)\mssqllocaldb` (configured in `appsettings.json`)
- **Docker**: SQL Server containers (`orders-db`, `inventory-db`), passwords in `.env`

## RabbitMQ Operations

### Queue Health
1. Open RabbitMQ Management: http://localhost:15672
2. Navigate to **Queues** tab
3. Check for messages in `_error` queues (dead-letter)

### Error Queues (DLQ)
Messages that exhaust all retries are moved to `{queue}_error`:
- `inventory-order-placed_error`
- `orders-stock-reserved_error`
- `orders-stock-insufficient_error`
- `notifications-order-placed_error`
- `notifications-stock-reserved_error`
- `notifications-stock-insufficient_error`
- `notifications-order-confirmed_error`
- `notifications-order-failed_error`

### Retry Policy
| Layer | Strategy | Config |
|-------|----------|--------|
| Immediate retry | Exponential | 3 attempts, 5–125s |
| Delayed redelivery | Fixed intervals | 30s, 60s, 120s |
| Kill switch | Circuit breaker | 15% failure rate (min 5 msgs), 60s restart |

### Reprocessing Failed Messages
In RabbitMQ Management UI:
1. Go to the `_error` queue
2. **Get Message(s)** to inspect the payload and exception
3. Fix the root cause
4. Move the message back to the source queue (or publish a new corrective event)

## Troubleshooting

### Service won't start
```bash
# Check container logs
docker compose logs orders-api --tail 50
docker compose logs inventory-api --tail 50
docker compose logs notifications-worker --tail 50

# Check health of dependencies
docker compose ps
docker compose logs rabbitmq --tail 20
docker compose logs orders-db --tail 20
```

### Database connection issues
- Verify `.env` passwords match `docker-compose.yml` environment variables
- Ensure SQL Server health check passes: `docker compose ps orders-db`
- Check if migrations ran: look for `An error occurred while migrating` in service logs

### Messages stuck in queues
1. Check consumer is running: `docker compose ps notifications-worker`
2. Check for errors in Seq: filter by `ServiceName = "Notifications.Worker"`
3. Inspect `_error` queues in RabbitMQ UI for poison messages
4. Check kill switch didn't trip: look for `KillSwitch` log entries

### Rate limiting (429 responses)
The Gateway applies fixed-window rate limiting:
- **Limit**: 100 requests per minute per client
- **Queue**: Up to 10 requests queued beyond the limit
- Adjust in `Gateway.API/Program.cs` → `AddFixedWindowLimiter`

## Running Tests

```bash
# Run all 113 tests
dotnet test

# Run a specific project
dotnet test tests/Orders.Tests
dotnet test tests/Inventory.Tests
dotnet test tests/Notifications.Tests

# With verbose output
dotnet test --verbosity normal
```

## Docker Operations

```bash
# Start all services
docker compose up -d --build

# Stop all services
docker compose down

# Stop and remove volumes (reset databases)
docker compose down -v

# Rebuild a single service
docker compose up -d --build orders-api

# View logs (follow mode)
docker compose logs -f orders-api
```
