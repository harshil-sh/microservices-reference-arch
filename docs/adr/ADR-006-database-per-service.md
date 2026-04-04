# ADR-006: Database-Per-Service with EF Core

## Status
**Accepted**

## Date
2025-01-01

## Context
Microservices should own their data to avoid tight coupling through shared databases. We need to decide on database strategy and ORM.

## Decision
- **Database-per-service**: Each service has its own SQL Server instance (Docker container).
- **ORM**: Entity Framework Core 8.0.11 with code-first migrations.
- **Migration strategy**: Auto-migrate on startup (`context.Database.MigrateAsync()`).

| Service | Database | Container |
|---------|----------|-----------|
| Orders.API | `orders_db` | `orders-db` |
| Inventory.API | `inventory_db` | `inventory-db` |
| Notifications.Worker | None | — (stateless) |

### Schema Ownership
- Orders service owns: `Orders`, `OrderItems` tables
- Inventory service owns: `InventoryItems`, `StockReservations` tables
- No cross-database queries or joins

### Seed Data
Both services include a `DbContextSeed` class that populates initial data on first startup.

## Consequences
- **Positive**: Services can evolve schemas independently without coordinating migrations.
- **Positive**: No shared database lock contention.
- **Positive**: Auto-migration simplifies development workflow.
- **Negative**: No cross-service queries — must use events for data that spans services.
- **Negative**: Auto-migration on startup is not recommended for production (would use a migration job or CI/CD step).
- **Negative**: Two SQL Server containers consume more memory than a single shared instance.
