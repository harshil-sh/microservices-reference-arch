using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Inventory.Infrastructure.Persistence;

public class InventoryDbContextFactory : IDesignTimeDbContextFactory<InventoryDbContext>
{
    public InventoryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<InventoryDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=inventory_db;Trusted_Connection=True",
            b => b.MigrationsAssembly(typeof(InventoryDbContext).Assembly.GetName().Name));

        return new InventoryDbContext(optionsBuilder.Options);
    }
}
