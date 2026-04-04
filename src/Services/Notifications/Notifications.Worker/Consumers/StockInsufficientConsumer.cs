using MassTransit;
using Notifications.Worker.Services;
using Shared.Contracts.Events;

namespace Notifications.Worker.Consumers;

public class StockInsufficientConsumer : IConsumer<StockInsufficient>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<StockInsufficientConsumer> _logger;

    public StockInsufficientConsumer(INotificationService notificationService, ILogger<StockInsufficientConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockInsufficient> context)
    {
        var message = context.Message;

        _logger.LogWarning(
            "Received StockInsufficient event for order {OrderId}: {Reason}. CorrelationId: {CorrelationId}",
            message.OrderId, message.Reason, message.CorrelationId);

        await _notificationService.SendOrderFailedAsync(
            message.OrderId,
            message.Reason,
            message.CorrelationId,
            context.CancellationToken);
    }
}
