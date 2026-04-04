using MassTransit;
using Notifications.Worker.Services;
using Shared.Contracts.Events;

namespace Notifications.Worker.Consumers;

public class StockReservedConsumer : IConsumer<StockReserved>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<StockReservedConsumer> _logger;

    public StockReservedConsumer(INotificationService notificationService, ILogger<StockReservedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received StockReserved event for order {OrderId}. CorrelationId: {CorrelationId}",
            message.OrderId, message.CorrelationId);

        await _notificationService.SendStockReservedAsync(
            message.OrderId,
            message.CorrelationId,
            context.CancellationToken);
    }
}
