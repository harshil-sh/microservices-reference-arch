using Inventory.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Inventory.Infrastructure.Persistence.Seed;

public static class InventoryDbContextSeed
{
    public static async Task SeedAsync(InventoryDbContext context, ILogger logger)
    {
        if (await context.InventoryItems.AnyAsync())
        {
            logger.LogInformation("Inventory database already seeded");
            return;
        }

        logger.LogInformation("Seeding inventory database...");

        var items = new List<InventoryItem>
        {
            InventoryItem.Create(
                Guid.Parse("11111111-1111-1111-1111-111111111111"),
                "Mechanical Keyboard",
                100),
            InventoryItem.Create(
                Guid.Parse("22222222-2222-2222-2222-222222222222"),
                "Wireless Mouse",
                250),
            InventoryItem.Create(
                Guid.Parse("33333333-3333-3333-3333-333333333333"),
                "USB-C Hub",
                75),
            InventoryItem.Create(
                Guid.Parse("44444444-4444-4444-4444-444444444444"),
                "27-inch Monitor",
                30),
            InventoryItem.Create(
                Guid.Parse("55555555-5555-5555-5555-555555555555"),
                "Webcam HD",
                150)
        };

        context.InventoryItems.AddRange(items);
        await context.SaveChangesAsync();

        logger.LogInformation("Inventory database seeded with {Count} products", items.Count);
    }
}
