using Inventory.Application.Interfaces;
using Inventory.Domain.Repositories;
using Inventory.Infrastructure.Messaging.Consumers;
using Inventory.Infrastructure.Messaging.Publishers;
using Inventory.Infrastructure.Persistence;
using Inventory.Infrastructure.Persistence.Repositories;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Inventory.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInventoryInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<InventoryDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("InventoryDb"),
                b => b.MigrationsAssembly(typeof(InventoryDbContext).Assembly.GetName().Name)));

        services.AddScoped<IInventoryRepository, InventoryRepository>();
        services.AddScoped<IEventPublisher, InventoryEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<OrderPlacedConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMq") ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(configuration["RabbitMq:Password"] ?? "guest");
                });

                cfg.UseDelayedRedelivery(r => r.Intervals(
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(60),
                    TimeSpan.FromSeconds(120)));
                cfg.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));

                cfg.ReceiveEndpoint("inventory-order-placed", e =>
                {
                    e.ConfigureConsumer<OrderPlacedConsumer>(context);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
