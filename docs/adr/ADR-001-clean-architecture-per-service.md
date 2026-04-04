# ADR-001: Clean Architecture Per Service

## Status
**Accepted**

## Date
2025-01-01

## Context
We need a consistent internal structure for each microservice that enforces separation of concerns, supports testability, and prevents infrastructure details from leaking into business logic.

## Decision
Each service follows a 4-layer Clean Architecture:

```
API → Application → Domain → Infrastructure
```

- **Domain**: Entities with encapsulated behavior, enums, repository interfaces. Zero external package dependencies.
- **Application**: MediatR handlers (CQRS), FluentValidation validators, DTOs, mappings, metrics. Depends only on Domain.
- **Infrastructure**: EF Core DbContext/repositories, MassTransit consumers/publishers. Implements interfaces defined in Application/Domain.
- **API**: ASP.NET Core controllers, health checks, `Program.cs` composition root. References all layers.

Dependency rule: inner layers never reference outer layers. Infrastructure implements abstractions defined in inner layers.

## Consequences
- **Positive**: Business logic is fully testable without database or message broker. Handlers can be unit-tested with mocked repositories.
- **Positive**: Infrastructure can be swapped (e.g., SQL Server → PostgreSQL) without touching domain/application code.
- **Negative**: More projects per service (4 vs 1). Acceptable overhead for a reference architecture.
- **Negative**: Mapping layer between domain entities and DTOs adds boilerplate.
