using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Orders.Application.Interfaces;
using Orders.Domain.Repositories;
using Orders.Infrastructure.Messaging.Consumers;
using Orders.Infrastructure.Messaging.Publishers;
using Orders.Infrastructure.Persistence;
using Orders.Infrastructure.Persistence.Repositories;

namespace Orders.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddOrdersInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<OrdersDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("OrdersDb"),
                b => b.MigrationsAssembly(typeof(OrdersDbContext).Assembly.GetName().Name)));

        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IEventPublisher, OrderEventPublisher>();

        services.AddMassTransit(bus =>
        {
            bus.AddConsumer<StockReservedConsumer>();
            bus.AddConsumer<StockInsufficientConsumer>();

            bus.UsingRabbitMq((context, cfg) =>
            {
                cfg.Host(configuration.GetConnectionString("RabbitMq") ?? "localhost", "/", h =>
                {
                    h.Username(configuration["RabbitMq:Username"] ?? "guest");
                    h.Password(configuration["RabbitMq:Password"] ?? "guest");
                });

                cfg.ReceiveEndpoint("orders-stock-reserved", e =>
                {
                    e.ConfigureConsumer<StockReservedConsumer>(context);
                    e.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(60),
                        TimeSpan.FromSeconds(120)));
                    e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));
                });

                cfg.ReceiveEndpoint("orders-stock-insufficient", e =>
                {
                    e.ConfigureConsumer<StockInsufficientConsumer>(context);
                    e.UseDelayedRedelivery(r => r.Intervals(
                        TimeSpan.FromSeconds(30),
                        TimeSpan.FromSeconds(60),
                        TimeSpan.FromSeconds(120)));
                    e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
