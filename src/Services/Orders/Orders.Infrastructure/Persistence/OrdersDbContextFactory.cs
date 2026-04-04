using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Orders.Infrastructure.Persistence;

public class OrdersDbContextFactory : IDesignTimeDbContextFactory<OrdersDbContext>
{
    public OrdersDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OrdersDbContext>();
        optionsBuilder.UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=orders_db;Trusted_Connection=True",
            b => b.MigrationsAssembly(typeof(OrdersDbContext).Assembly.GetName().Name));

        return new OrdersDbContext(optionsBuilder.Options);
    }
}
