using MassTransit;
using Notifications.Worker.Consumers;
using Notifications.Worker.Services;
using Shared.Observability;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.AddObservability("Notifications.Worker", builder.Configuration);

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

        cfg.ReceiveEndpoint("notifications-order-placed", e =>
        {
            e.ConfigureConsumer<OrderPlacedConsumer>(context);
            e.UseDelayedRedelivery(r => r.Intervals(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(120)));
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));
        });

        cfg.ReceiveEndpoint("notifications-stock-reserved", e =>
        {
            e.ConfigureConsumer<StockReservedConsumer>(context);
            e.UseDelayedRedelivery(r => r.Intervals(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(120)));
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));
        });

        cfg.ReceiveEndpoint("notifications-stock-insufficient", e =>
        {
            e.ConfigureConsumer<StockInsufficientConsumer>(context);
            e.UseDelayedRedelivery(r => r.Intervals(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(120)));
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));
        });

        cfg.ReceiveEndpoint("notifications-order-confirmed", e =>
        {
            e.ConfigureConsumer<OrderConfirmedConsumer>(context);
            e.UseDelayedRedelivery(r => r.Intervals(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(120)));
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));
        });

        cfg.ReceiveEndpoint("notifications-order-failed", e =>
        {
            e.ConfigureConsumer<OrderFailedConsumer>(context);
            e.UseDelayedRedelivery(r => r.Intervals(
                TimeSpan.FromSeconds(30),
                TimeSpan.FromSeconds(60),
                TimeSpan.FromSeconds(120)));
            e.UseMessageRetry(r => r.Exponential(3, TimeSpan.FromSeconds(5), TimeSpan.FromSeconds(125), TimeSpan.FromSeconds(5)));
        });

        cfg.ConfigureEndpoints(context);
    });
});

var host = builder.Build();
host.Run();
