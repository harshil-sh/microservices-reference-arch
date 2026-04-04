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

                cfg.UseDelayedRedelivery(r => r.Intervals(
                    TimeSpan.FromSeconds(30),
                    TimeSpan.FromSeconds(60),
                    TimeSpan.FromSeconds(120)));
                cfg.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));

                cfg.UseKillSwitch(options => options
                    .SetActivationThreshold(5)
                    .SetTripThreshold(0.15)
                    .SetRestartTimeout(s: 60));

                cfg.ReceiveEndpoint("orders-stock-reserved", e =>
                {
                    e.PrefetchCount = 16;
                    e.ConcurrentMessageLimit = 8;
                    e.ConfigureConsumer<StockReservedConsumer>(context);
                });

                cfg.ReceiveEndpoint("orders-stock-insufficient", e =>
                {
                    e.PrefetchCount = 16;
                    e.ConcurrentMessageLimit = 8;
                    e.ConfigureConsumer<StockInsufficientConsumer>(context);
                });

                cfg.ConfigureEndpoints(context);
            });
        });

        return services;
    }
}
