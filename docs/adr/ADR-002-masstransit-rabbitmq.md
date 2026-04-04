# ADR-002: MassTransit + RabbitMQ for Async Messaging

## Status
**Accepted**

## Date
2025-01-01

## Context
Services need to communicate asynchronously to maintain loose coupling. We need reliable message delivery with retry, dead-letter, and observability support.

## Alternatives Considered
1. **Raw RabbitMQ client** — Maximum control, but requires hand-rolling serialization, retry, DLQ, and consumer pipeline.
2. **MassTransit + RabbitMQ** — Opinionated abstraction over RabbitMQ with built-in retry, redelivery, kill switch, saga support, and OpenTelemetry integration.
3. **Azure Service Bus / AWS SQS** — Cloud-managed, but adds cloud vendor lock-in for a reference architecture meant to run locally.

## Decision
Use **MassTransit 9.1.0 with RabbitMQ transport**.

### Configuration
- **Bus-level retry**: Exponential backoff (3 attempts, 5–125s) + delayed redelivery (30s, 60s, 120s) applied once at the bus level, not per-endpoint.
- **Kill switch**: Circuit breaker at 15% failure rate (min 5 messages), 60s restart timeout.
- **Named receive endpoints**: Each consumer gets a dedicated queue (e.g., `inventory-order-placed`, `notifications-order-confirmed`) for independent scaling and monitoring.
- **Endpoint tuning**: `PrefetchCount = 16`, `ConcurrentMessageLimit = 8` per endpoint for backpressure control.
- **Dead-letter**: Messages that exhaust all retries automatically move to `{queue}_error` (MassTransit default behavior).

## Consequences
- **Positive**: Retry, DLQ, and circuit breaker work out of the box with minimal configuration.
- **Positive**: MassTransit emits OpenTelemetry traces and metrics natively.
- **Positive**: Consumers are plain classes with `IConsumer<T>` — easy to test with `MassTransit.Testing`.
- **Negative**: MassTransit is an opinionated abstraction. Switching to a different broker (e.g., Kafka) requires adapter changes.
- **Negative**: Named endpoints mean queue topology must be managed (though MassTransit auto-creates them).
