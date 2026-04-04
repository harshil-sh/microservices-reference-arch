using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Orders.Domain.Entities;
using Orders.Infrastructure.Persistence;

namespace Orders.API.HealthChecks;

public class DatabaseHealthCheck : IHealthCheck
{
    private readonly OrdersDbContext _context;
    private readonly ILogger<DatabaseHealthCheck> _logger;

    public DatabaseHealthCheck(OrdersDbContext context, ILogger<DatabaseHealthCheck> logger)
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
            var testCustomerId = Guid.Parse("00000000-0000-0000-0000-000000000001");

            var testItem = OrderItem.Create(
                Guid.Parse("00000000-0000-0000-0000-000000000002"),
                "Test",
                1,
                1.00m);

            var testOrder = Order.Create(testCustomerId, [testItem]);

            await _context.Orders.AddAsync(testOrder, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);

            var retrieved = await _context.Orders
                .Include(o => o.Items)
                .FirstOrDefaultAsync(o => o.Id == testOrder.Id, cancellationToken);

            if (retrieved is null)
                return HealthCheckResult.Unhealthy("Failed to read test order from database.");

            _context.Orders.Remove(retrieved);
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
