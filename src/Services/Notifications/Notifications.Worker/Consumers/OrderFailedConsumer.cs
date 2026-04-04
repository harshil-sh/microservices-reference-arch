using MassTransit;
using Notifications.Worker.Services;
using Shared.Contracts.Events;

namespace Notifications.Worker.Consumers;

public class OrderFailedConsumer : IConsumer<OrderFailed>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderFailedConsumer> _logger;

    public OrderFailedConsumer(INotificationService notificationService, ILogger<OrderFailedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderFailed> context)
    {
        var message = context.Message;

        _logger.LogWarning(
            "Received OrderFailed event for order {OrderId}: {Reason}. CorrelationId: {CorrelationId}",
            message.OrderId, message.Reason, message.CorrelationId);

        await _notificationService.SendOrderFailedAsync(
            message.OrderId,
            message.Reason,
            message.CorrelationId,
            context.CancellationToken);
    }
}
