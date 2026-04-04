# ADR-004: Shared Libraries Strategy

## Status
**Accepted**

## Date
2025-01-01

## Context
Multiple services need identical infrastructure code (observability setup, integration event contracts, MediatR pipeline behaviors). We need to decide what to share and how.

## Decision
Use **shared projects within the monorepo** (not NuGet packages). Three shared libraries:

| Library | Content | Dependency Profile |
|---------|---------|-------------------|
| `Shared.Contracts` | `IIntegrationEvent` interface + event records | Zero dependencies |
| `Shared.Observability` | OpenTelemetry + Serilog configuration | OTEL, Serilog packages |
| `Shared.Application` | `ValidationBehavior<TRequest, TResponse>` | MediatR, FluentValidation |

### What is NOT shared
- Domain entities (each service owns its domain)
- Repository interfaces (internal to each service)
- DTOs and mappings (service-specific)
- EF Core configurations (database-per-service)

### Why monorepo shared projects over NuGet packages
1. **No versioning overhead** — changes are atomic across all consumers in a single commit.
2. **Coupling already exists** — all services use identical MediatR 14.1.0 and FluentValidation 12.1.1 versions.
3. **Two services** — the scale doesn't justify NuGet packaging, publishing, and version management.
4. **Follows existing pattern** — mirrors the existing `Shared.Contracts` and `Shared.Observability` structure.

### When to switch to NuGet packages
- 10+ services consuming the shared code
- Polyrepo setup (services in separate repositories)
- Different teams owning different services with independent release cycles

## Consequences
- **Positive**: Zero publishing friction — edit the shared file, all consumers pick it up immediately.
- **Positive**: Single PR to change shared behavior across all services.
- **Negative**: All services must use the same MediatR/FluentValidation version (acceptable in a monorepo).
- **Negative**: Not suitable for polyrepo architectures.
