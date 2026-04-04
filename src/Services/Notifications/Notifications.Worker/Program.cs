using MassTransit;
using Notifications.Worker.Consumers;
using Notifications.Worker.Metrics;
using Notifications.Worker.Services;
using Shared.Observability;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservability("Notifications.Worker", builder.Configuration,
    additionalMeterNames: [NotificationsMetrics.MeterName]);

builder.Services.AddSingleton<NotificationsMetrics>();
builder.Services.AddScoped<INotificationService, NotificationService>();

builder.Services.AddMassTransit(bus =>
{
    bus.AddConsumer<OrderPlacedConsumer>();
    bus.AddConsumer<StockReservedConsumer>();
    bus.AddConsumer<StockInsufficientConsumer>();
    bus.AddConsumer<OrderConfirmedConsumer>();
    bus.AddConsumer<OrderFailedConsumer>();

    bus.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();

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

        cfg.ReceiveEndpoint("notifications-order-placed", e =>
        {
            e.PrefetchCount = 16;
            e.ConcurrentMessageLimit = 8;
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-stock-reserved", e =>
        {
            e.PrefetchCount = 16;
            e.ConcurrentMessageLimit = 8;
            e.ConfigureConsumer<StockReservedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-stock-insufficient", e =>
        {
            e.PrefetchCount = 16;
            e.ConcurrentMessageLimit = 8;
            e.ConfigureConsumer<StockInsufficientConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-order-confirmed", e =>
        {
            e.PrefetchCount = 16;
            e.ConcurrentMessageLimit = 8;
            e.ConfigureConsumer<OrderConfirmedConsumer>(context);
        });

        cfg.ReceiveEndpoint("notifications-order-failed", e =>
        {
            e.PrefetchCount = 16;
            e.ConcurrentMessageLimit = 8;
            e.ConfigureConsumer<OrderFailedConsumer>(context);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
