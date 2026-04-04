# Architecture Overview

## System Context

This is a **microservices reference architecture** built with .NET 8, demonstrating production-grade patterns for event-driven systems. It is designed as an interview prep artifact for Lead/Staff/Principal engineering roles.

```
                    ┌──────────────┐
                    │   Clients    │
                    └──────┬───────┘
                           │ HTTP
                    ┌──────▼───────┐
                    │  Gateway.API │  YARP reverse proxy
                    │  (port 8080) │  + rate limiting
                    └──────┬───────┘
                ┌──────────┼──────────┐
                │                     │
         ┌──────▼──────┐     ┌───────▼───────┐
         │  Orders.API │     │ Inventory.API │
         │  (port 5001)│     │  (port 5002)  │
         └──────┬──────┘     └───────┬───────┘
                │                     │
         ┌──────▼──────┐     ┌───────▼───────┐
         │  orders-db  │     │ inventory-db  │
         │  (SQL Svr)  │     │  (SQL Svr)    │
         └─────────────┘     └───────────────┘
                │                     │
                └──────────┬──────────┘
                    ┌──────▼───────┐
                    │   RabbitMQ   │  MassTransit / AMQP
                    └──────┬───────┘
                    ┌──────▼───────────────┐
                    │ Notifications.Worker │
                    │  (.NET Worker Svc)   │
                    └──────────────────────┘

                    ┌──────────────┐
                    │     Seq      │  OTLP traces, metrics, logs
                    │  (port 8081) │
                    └──────────────┘
```

## Services

| Service | Type | Port | Purpose |
|---------|------|------|---------|
| **Gateway.API** | ASP.NET Core (YARP) | 8080 | Reverse proxy, rate limiting, single entry point |
| **Orders.API** | ASP.NET Core Web API | 5001 (dev) | Order management, CQRS via MediatR |
| **Inventory.API** | ASP.NET Core Web API | 5002 (dev) | Stock management, reservation workflow |
| **Notifications.Worker** | .NET Worker Service | — | Event-driven notification dispatch |

## Infrastructure

| Component | Image | Ports | Purpose |
|-----------|-------|-------|---------|
| **RabbitMQ** | `rabbitmq:3.13-management` | 5672 / 15672 | Message broker (AMQP + Management UI) |
| **Seq** | `datalust/seq:latest` | 5341 / 8081 | Centralized logs, traces, metrics (OTLP) |
| **orders-db** | `mcr.microsoft.com/mssql/server:2022-latest` | — | Orders database (SQL Server) |
| **inventory-db** | `mcr.microsoft.com/mssql/server:2022-latest` | — | Inventory database (SQL Server) |

## Architecture Patterns

### Per-Service Clean Architecture
Each service follows a 4-layer structure:
```
API → Application → Domain → Infrastructure
```
- **Domain**: Entities, value objects, repository interfaces. Zero external dependencies.
- **Application**: Command/query handlers (MediatR), validators (FluentValidation), DTOs, mappings, metrics.
- **Infrastructure**: EF Core persistence, MassTransit consumers/publishers, repository implementations.
- **API**: Controllers, health checks, Program.cs composition root.

### CQRS (Command Query Responsibility Segregation)
- Commands: `PlaceOrderCommand`, `UpdateOrderStatusCommand`, `ReserveStockCommand`
- Queries: `GetOrderByIdQuery`, `GetAllOrdersQuery`, `GetInventoryItemQuery`, `GetAllInventoryQuery`
- Pipeline: `ValidationBehavior<TRequest, TResponse>` runs FluentValidation before every handler

### Event-Driven Communication
Services communicate exclusively via integration events over RabbitMQ (MassTransit). See [events.md](events.md) for the full event catalog.

### Shared Libraries
| Library | Purpose |
|---------|---------|
| `Shared.Contracts` | Integration event definitions (`IIntegrationEvent` + event records) |
| `Shared.Observability` | OpenTelemetry + Serilog configuration |
| `Shared.Application` | Cross-cutting MediatR behaviors (`ValidationBehavior`) |

## Technology Stack

| Category | Technology | Version |
|----------|-----------|---------|
| Runtime | .NET | 8.0 |
| Language | C# | 12.0 |
| API Gateway | YARP | 2.1.0 |
| CQRS | MediatR | 14.1.0 |
| Validation | FluentValidation | 12.1.1 |
| ORM | Entity Framework Core | 8.0.11 |
| Messaging | MassTransit + RabbitMQ | 9.1.0 |
| Logging | Serilog | 8.0.0 |
| Tracing/Metrics | OpenTelemetry | 1.15.x |
| Log Aggregator | Seq | latest |
| Database | SQL Server | 2022 |
| Testing | xUnit + Moq | 2.5.3 / 4.20.72 |
| Containers | Docker Compose | 3.8 |
