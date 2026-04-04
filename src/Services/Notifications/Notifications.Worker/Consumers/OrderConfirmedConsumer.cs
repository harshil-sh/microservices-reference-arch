using MassTransit;
using Notifications.Worker.Services;
using Shared.Contracts.Events;

namespace Notifications.Worker.Consumers;

public class OrderConfirmedConsumer : IConsumer<OrderConfirmed>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderConfirmedConsumer> _logger;

    public OrderConfirmedConsumer(INotificationService notificationService, ILogger<OrderConfirmedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderConfirmed> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received OrderConfirmed event for order {OrderId}. CorrelationId: {CorrelationId}",
            message.OrderId, message.CorrelationId);

        await _notificationService.SendOrderConfirmedAsync(
            message.OrderId,
            message.CorrelationId,
            context.CancellationToken);
    }
}
