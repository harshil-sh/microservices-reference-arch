# Integration Events Catalog

All services communicate asynchronously via integration events published to RabbitMQ through MassTransit. Every event implements the `IIntegrationEvent` contract:

```csharp
public interface IIntegrationEvent
{
    Guid EventId { get; }
    DateTime OccurredAt { get; }
    string CorrelationId { get; }
    int Version { get; }
}
```

## Event Flow

```
  Orders.API                 Inventory.API             Notifications.Worker
  ──────────                 ─────────────             ────────────────────
      │                           │                           │
      │ ── OrderPlaced ──────────►│                           │
      │                           │ ── StockReserved ────────►│
      │◄── StockReserved ─────── │                           │
      │ ── OrderConfirmed ──────────────────────────────────►│
      │                           │                           │
      │                           │ ── StockInsufficient ───►│
      │◄── StockInsufficient ──── │                           │
      │ ── OrderFailed ────────────────────────────────────►│
      │                           │                           │
      │                           │                           │── NotificationSent
```

## Events

### OrderPlaced
| Field | Type | Description |
|-------|------|-------------|
| `EventId` | `Guid` | Unique event identifier |
| `OccurredAt` | `DateTime` | UTC timestamp |
| `CorrelationId` | `string` | Distributed trace correlation |
| `Version` | `int` | Schema version (currently `1`) |
| `OrderId` | `Guid` | Order identifier |
| `CustomerId` | `Guid` | Customer identifier |
| `Items` | `List<OrderPlacedItem>` | Line items (ProductId, ProductName, Quantity, UnitPrice) |
| `TotalAmount` | `decimal` | Order total |
| `PlacedAt` | `DateTime` | When the order was placed |

**Publisher**: `Orders.Infrastructure` → `OrderEventPublisher`
**Consumers**:
- `Inventory.Infrastructure` → `OrderPlacedConsumer` (queue: `inventory-order-placed`)
- `Notifications.Worker` → `OrderPlacedConsumer` (queue: `notifications-order-placed`)

---

### StockReserved
| Field | Type | Description |
|-------|------|-------------|
| `EventId` | `Guid` | Unique event identifier |
| `OccurredAt` | `DateTime` | UTC timestamp |
| `CorrelationId` | `string` | Distributed trace correlation |
| `Version` | `int` | Schema version (currently `1`) |
| `OrderId` | `Guid` | Order that triggered the reservation |
| `ReservedAt` | `DateTime` | When stock was reserved |
| `Items` | `List<ReservedItem>` | Reserved items (ProductId, Quantity) |

**Publisher**: `Inventory.Infrastructure` → `InventoryEventPublisher`
**Consumers**:
- `Orders.Infrastructure` → `StockReservedConsumer` (queue: `orders-stock-reserved`)
- `Notifications.Worker` → `StockReservedConsumer` (queue: `notifications-stock-reserved`)

---

### StockInsufficient
| Field | Type | Description |
|-------|------|-------------|
| `EventId` | `Guid` | Unique event identifier |
| `OccurredAt` | `DateTime` | UTC timestamp |
| `CorrelationId` | `string` | Distributed trace correlation |
| `Version` | `int` | Schema version (currently `1`) |
| `OrderId` | `Guid` | Order that failed stock check |
| `FailedAt` | `DateTime` | When the failure occurred |
| `Reason` | `string` | Human-readable failure reason |

**Publisher**: `Inventory.Infrastructure` → `InventoryEventPublisher`
**Consumers**:
- `Orders.Infrastructure` → `StockInsufficientConsumer` (queue: `orders-stock-insufficient`)
- `Notifications.Worker` → `StockInsufficientConsumer` (queue: `notifications-stock-insufficient`)

---

### OrderConfirmed
| Field | Type | Description |
|-------|------|-------------|
| `EventId` | `Guid` | Unique event identifier |
| `OccurredAt` | `DateTime` | UTC timestamp |
| `CorrelationId` | `string` | Distributed trace correlation |
| `Version` | `int` | Schema version (currently `1`) |
| `OrderId` | `Guid` | Confirmed order identifier |
| `ConfirmedAt` | `DateTime` | Confirmation timestamp |

**Publisher**: `Orders.Infrastructure` → `OrderEventPublisher`
**Consumer**: `Notifications.Worker` → `OrderConfirmedConsumer` (queue: `notifications-order-confirmed`)

---

### OrderFailed
| Field | Type | Description |
|-------|------|-------------|
| `EventId` | `Guid` | Unique event identifier |
| `OccurredAt` | `DateTime` | UTC timestamp |
| `CorrelationId` | `string` | Distributed trace correlation |
| `Version` | `int` | Schema version (currently `1`) |
| `OrderId` | `Guid` | Failed order identifier |
| `FailedAt` | `DateTime` | Failure timestamp |
| `Reason` | `string` | Failure reason |

**Publisher**: `Orders.Infrastructure` → `OrderEventPublisher`
**Consumer**: `Notifications.Worker` → `OrderFailedConsumer` (queue: `notifications-order-failed`)

---

### NotificationSent
| Field | Type | Description |
|-------|------|-------------|
| `EventId` | `Guid` | Unique event identifier |
| `OccurredAt` | `DateTime` | UTC timestamp |
| `CorrelationId` | `string` | Distributed trace correlation |
| `Version` | `int` | Schema version (currently `1`) |
| `OrderId` | `Guid` | Related order identifier |
| `Channel` | `string` | Delivery channel (e.g., `"Email"`) |
| `RecipientId` | `Guid` | Notification recipient |
| `SentAt` | `DateTime` | Dispatch timestamp |

**Publisher**: `Notifications.Worker` → `NotificationService`
**Consumer**: None (terminal event / audit trail)

## Queue Naming Convention

```
{service}-{event-name}
```

| Queue | Service | Consumer |
|-------|---------|----------|
| `inventory-order-placed` | Inventory.API | `OrderPlacedConsumer` |
| `orders-stock-reserved` | Orders.API | `StockReservedConsumer` |
| `orders-stock-insufficient` | Orders.API | `StockInsufficientConsumer` |
| `notifications-order-placed` | Notifications.Worker | `OrderPlacedConsumer` |
| `notifications-stock-reserved` | Notifications.Worker | `StockReservedConsumer` |
| `notifications-stock-insufficient` | Notifications.Worker | `StockInsufficientConsumer` |
| `notifications-order-confirmed` | Notifications.Worker | `OrderConfirmedConsumer` |
| `notifications-order-failed` | Notifications.Worker | `OrderFailedConsumer` |

## Retry & Error Handling

All consumers share a bus-level retry policy:

| Layer | Strategy | Configuration |
|-------|----------|---------------|
| **Immediate retry** | Exponential backoff | 3 attempts, 5–125s intervals |
| **Delayed redelivery** | Fixed intervals | 30s → 60s → 120s |
| **Kill switch** | Circuit breaker | Trips at 15% failure rate (min 5 msgs), restarts after 60s |
| **Dead-letter queue** | MassTransit `_error` | Messages that exhaust all retries are moved to `{queue}_error` |

Per-endpoint tuning:
- `PrefetchCount = 16` — controls how many messages RabbitMQ pre-delivers
- `ConcurrentMessageLimit = 8` — caps parallel consumer execution
