using Inventory.Domain.Entities;
using Inventory.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Inventory.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly InventoryDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(InventoryDbContext context, ILogger<DatabaseHealthCheck> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var testItem = InventoryItem.Create(
                Guid.NewGuid(),
                "Test",
                1);

            await _context.InventoryItems.AddAsync(testItem, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var retrieved = await _context.InventoryItems
                .FirstOrDefaultAsync(i => i.Id == testItem.Id, cancellationToken);

            if (retrieved is null)
                return HealthCheckResult.Unhealthy("Failed to read test inventory item from database.");

            _context.InventoryItems.Remove(retrieved);
            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogDebug("Database health check passed (create-read-delete)");

            return HealthCheckResult.Healthy("Database is responsive. Create-read-delete cycle completed.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database health check failed");
            return HealthCheckResult.Unhealthy("Database health check failed.", ex);
        }
    }
}
