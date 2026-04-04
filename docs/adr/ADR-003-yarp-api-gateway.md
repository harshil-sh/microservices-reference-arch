# ADR-003: YARP as API Gateway

## Status
**Accepted**

## Date
2025-01-01

## Context
We need a single entry point for all client traffic that provides routing, rate limiting, and observability. The gateway should not contain business logic.

## Alternatives Considered
1. **Ocelot** — Popular .NET API gateway, but development has slowed and configuration is JSON-heavy.
2. **YARP (Yet Another Reverse Proxy)** — Microsoft-maintained, high-performance, config-driven reverse proxy built on ASP.NET Core.
3. **Nginx / Envoy** — Battle-tested, but outside the .NET ecosystem. Adds operational complexity for a .NET reference architecture.

## Decision
Use **YARP 2.1.0** as the API gateway.

### Configuration
- **Routes**: `orders-route` → `orders-cluster`, `inventory-route` → `inventory-cluster`
- **Rate limiting**: Fixed-window policy — 100 requests/minute, queue depth 10
- **Health check**: `/health` endpoint on the gateway itself
- **Observability**: Shared `AddObservability()` extension wires up OTLP traces/metrics/logs

### Gateway does NOT:
- Authenticate/authorize (would be added via middleware in production)
- Transform request/response bodies
- Contain any business logic

## Consequences
- **Positive**: First-party Microsoft support, actively maintained, excellent performance.
- **Positive**: Configuration-driven (routes/clusters in `appsettings.json`) — no code changes to add new routes.
- **Positive**: Rate limiting is built into ASP.NET Core 8 — no additional packages needed.
- **Negative**: Less feature-rich than Envoy for advanced traffic management (weighted routing, circuit breaking at gateway level).
