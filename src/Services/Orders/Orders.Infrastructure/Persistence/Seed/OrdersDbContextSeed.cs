using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Infrastructure.Persistence.Seed;

public static class OrdersDbContextSeed
{
    public static async Task SeedAsync(OrdersDbContext context, ILogger logger)
    {
        if (await context.Orders.AnyAsync())
        {
            logger.LogInformation("Orders database already seeded");
            return;
        }

        logger.LogInformation("Seeding orders database...");

        var customerId = Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890");

        var order1Items = new List<OrderItem>
        {
            OrderItem.Create(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Mechanical Keyboard", 1, 149.99m),
            OrderItem.Create(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Wireless Mouse", 2, 49.99m)
        };

        var order1 = Order.Create(customerId, order1Items);

        var order2Items = new List<OrderItem>
        {
            OrderItem.Create(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "USB-C Hub", 1, 79.99m)
        };

        var order2 = Order.Create(customerId, order2Items);

        context.Orders.AddRange(order1, order2);
        await context.SaveChangesAsync();

        logger.LogInformation("Orders database seeded with {Count} orders", 2);
    }
}
