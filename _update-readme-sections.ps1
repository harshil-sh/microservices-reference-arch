$c = Get-Content 'README.md' -Raw

# Update section 7 - ADRs table
$oldAdrs = '| ADR | Decision | Why it matters |
|---|---|---|
| [ADR 001](docs/adr/001-database-per-service.md) | Database per service ' + [char]0x2013 + ' enforced via separate containers | Prevents hidden coupling at the data layer |
| [ADR 002](docs/adr/002-async-only-inter-service-comms.md) | Async-only inter-service communication via RabbitMQ | Temporal decoupling ' + [char]0x2013 + ' services survive each other''s downtime |
| [ADR 003](docs/adr/003-gateway-single-entry-point.md) | YARP gateway as single external entry point | Single place for cross-cutting concerns, hides internal topology |
| [ADR 004](docs/adr/004-opentelemetry-first.md) | OpenTelemetry instrumentation from day one | Retrofitting observability is exponentially harder |
| [ADR 005](docs/adr/005-eventual-consistency.md) | Eventual consistency as the default model | Strong consistency in distributed systems requires unacceptable coupling |'

$newAdrs = '| ADR | Decision | Why it matters |
|---|---|---|
| [ADR-001](docs/adr/ADR-001-clean-architecture-per-service.md) | Clean Architecture per service | Enforces separation of concerns, testability, swappable infrastructure |
| [ADR-002](docs/adr/ADR-002-masstransit-rabbitmq.md) | MassTransit + RabbitMQ for async messaging | Built-in retry, DLQ, circuit breaker, OpenTelemetry integration |
| [ADR-003](docs/adr/ADR-003-yarp-api-gateway.md) | YARP as API Gateway | Microsoft-maintained, config-driven, rate limiting built-in |
| [ADR-004](docs/adr/ADR-004-shared-libraries-strategy.md) | Shared libraries in monorepo | Zero publishing friction, atomic cross-service changes |
| [ADR-005](docs/adr/ADR-005-observability-otel-serilog-seq.md) | OpenTelemetry + Serilog + Seq | Three-pillar observability with custom business metrics |
| [ADR-006](docs/adr/ADR-006-database-per-service.md) | Database-per-service with EF Core | Independent schema evolution, no cross-service data coupling |'

if ($c.Contains('ADR 001](docs/adr/001-database-per-service.md)')) {
    $c = $c.Replace($oldAdrs, $newAdrs)
    Write-Host 'ADR table updated'
} else {
    Write-Host 'ADR table marker not found - skipping'
}

# Update section 13 - Tests count and details
$oldTests = 'Tests use Testcontainers to spin up real SQL Server and RabbitMQ instances for integration
tests ' + [char]0x2013 + ' no mocking of infrastructure dependencies in integration test suites.'

$newTests = '113 unit tests across 3 projects, all passing:

| Project | Tests | Coverage |
|---|---|---|
| Orders.Tests | 57 | Domain entities, command handlers, validators, queries, consumers, publishers, mappings, pipeline behavior |
| Inventory.Tests | 46 | Domain entities, command handler, validators, queries, consumer, publisher, mappings |
| Notifications.Tests | 10 | Notification service, all 5 consumers |'

if ($c.Contains('Testcontainers to spin up real SQL Server')) {
    $c = $c.Replace($oldTests, $newTests)
    Write-Host 'Tests section updated'
} else {
    Write-Host 'Tests marker not found - skipping'
}

# Update section 14 - Mark implemented items
$oldResilience = '**Resilience**
- Polly circuit breakers on all outbound calls from the gateway
- Retry policies with exponential backoff on all event consumers (currently three retries)
- Health check endpoints on all services with readiness and liveness probes
- Chaos engineering tests (simulated service failures, network partitions)'

$newResilience = '**Resilience**
- Polly circuit breakers on all outbound calls from the gateway
- ' + [char]0x2705 + ' ~~Retry policies with exponential backoff on all event consumers~~ (implemented: bus-level exponential retry + delayed redelivery + kill switch circuit breaker)
- ' + [char]0x2705 + ' ~~Health check endpoints on all services~~ (implemented on Orders, Inventory, and Gateway)
- Chaos engineering tests (simulated service failures, network partitions)'

if ($c.Contains('currently three retries')) {
    $c = $c.Replace($oldResilience, $newResilience)
    Write-Host 'Resilience section updated'
} else {
    Write-Host 'Resilience marker not found - skipping'
}

$oldObsProd = '**Observability**
- Grafana dashboards visualising the OpenTelemetry metrics per service
- Alerting rules on queue depth, DLQ message count, and error rate
- Distributed tracing sampling strategy for high-volume production traffic'

$newObsProd = '**Observability**
- ' + [char]0x2705 + ' ~~Custom business metrics per service~~ (implemented: orders.placed.total, inventory.stock_reservations.total, notifications.sent.total, etc.)
- Grafana dashboards visualising the OpenTelemetry metrics per service
- Alerting rules on queue depth, DLQ message count, and error rate
- Distributed tracing sampling strategy for high-volume production traffic'

if ($c.Contains('Grafana dashboards visualising')) {
    $c = $c.Replace($oldObsProd, $newObsProd)
    Write-Host 'Observability prod section updated'
} else {
    Write-Host 'Observability prod marker not found - skipping'
}

Set-Content 'README.md' -Value $c -NoNewline
Write-Host 'README.md updated successfully'
