using MassTransit;
using Notifications.Worker.Services;
using Shared.Contracts.Events;

namespace Notifications.Worker.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(INotificationService notificationService, ILogger<OrderPlacedConsumer> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received OrderPlaced event for order {OrderId}, customer {CustomerId}. CorrelationId: {CorrelationId}",
            message.OrderId, message.CustomerId, message.CorrelationId);

        await _notificationService.SendOrderReceivedAsync(
            message.OrderId,
            message.CustomerId,
            message.TotalAmount,
            message.CorrelationId,
            context.CancellationToken);
    }
}
