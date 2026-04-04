# ADR-005: Observability with OpenTelemetry + Serilog + Seq

## Status
**Accepted**

## Date
2025-01-01

## Context
We need unified observability (logs, traces, metrics) across all services with correlation support for distributed tracing.

## Decision
Use a three-pillar observability stack:

| Pillar | Technology | Export |
|--------|-----------|--------|
| **Logging** | Serilog | Console + OTLP → Seq |
| **Tracing** | OpenTelemetry | OTLP (HTTP/Protobuf) → Seq |
| **Metrics** | OpenTelemetry | OTLP (HTTP/Protobuf) → Seq |

### Implementation
A single `AddObservability()` extension method in `Shared.Observability` configures all three pillars. Each service calls it with its service name and optional custom meter names:

```csharp
builder.Services.AddObservability("Orders.API", builder.Configuration,
    additionalMeterNames: [OrdersMetrics.MeterName]);
```

### Instrumentation Sources
- ASP.NET Core (HTTP request traces and metrics)
- HttpClient (outbound HTTP call traces)
- MassTransit (message processing traces and metrics)
- Custom business metrics per service

### Custom Metrics
| Service | Meter | Metrics |
|---------|-------|---------|
| Orders | `Orders.API` | `orders.placed.total`, `orders.confirmed.total`, `orders.failed.total`, `orders.total_amount` |
| Inventory | `Inventory.API` | `inventory.stock_reservations.total`, `inventory.stock_insufficient.total`, `inventory.stock_rollbacks.total` |
| Notifications | `Notifications.Worker` | `notifications.sent.total` (tagged by `channel`) |

### Correlation
- Every integration event carries a `CorrelationId`
- Serilog enriches all logs with `ServiceName`, `MachineName`, and `EnvironmentName`
- OpenTelemetry propagates trace context across HTTP and MassTransit boundaries

## Consequences
- **Positive**: Single pane of glass (Seq) for logs, traces, and metrics.
- **Positive**: `CorrelationId` enables end-to-end tracing of an order from placement through confirmation/failure.
- **Positive**: Custom metrics provide real-time business KPIs without application-level dashboarding code.
- **Negative**: Seq is a single point of failure for observability (acceptable for local development; production would use a distributed backend).
- **Negative**: OTLP/Protobuf adds serialization overhead (negligible at this scale).
