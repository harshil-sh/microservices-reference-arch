using MassTransit;
using Shared.Contracts.Events;

namespace Notifications.Worker.Services;

public class NotificationService : INotificationService
{
    private readonly IPublishEndpoint _publishEndpoint;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(IPublishEndpoint publishEndpoint, ILogger<NotificationService> logger)
    {
        _publishEndpoint = publishEndpoint;
        _logger = logger;
    }

    public async Task SendOrderReceivedAsync(Guid orderId, Guid customerId, decimal totalAmount, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending 'order received' notification for order {OrderId} to customer {CustomerId}. Total: {TotalAmount:C}",
            orderId, customerId, totalAmount);

        // In production, this would dispatch via email, SMS, push notification, etc.
        await PublishNotificationSentAsync(orderId, customerId, "Email", correlationId, cancellationToken);
    }

    public async Task SendStockReservedAsync(Guid orderId, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending 'stock reserved' notification for order {OrderId}",
            orderId);

        await PublishNotificationSentAsync(orderId, Guid.Empty, "Email", correlationId, cancellationToken);
    }

    public async Task SendOrderConfirmedAsync(Guid orderId, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation(
            "Sending 'order confirmed' notification for order {OrderId}",
            orderId);

        await PublishNotificationSentAsync(orderId, Guid.Empty, "Email", correlationId, cancellationToken);
    }

    public async Task SendOrderFailedAsync(Guid orderId, string reason, string correlationId, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning(
            "Sending 'order failed' notification for order {OrderId}. Reason: {Reason}",
            orderId, reason);

        await PublishNotificationSentAsync(orderId, Guid.Empty, "Email", correlationId, cancellationToken);
    }

    private async Task PublishNotificationSentAsync(Guid orderId, Guid recipientId, string channel, string correlationId, CancellationToken cancellationToken)
    {
        var @event = new NotificationSent
        {
            OrderId = orderId,
            Channel = channel,
            RecipientId = recipientId,
            SentAt = DateTime.UtcNow,
            CorrelationId = correlationId
        };

        await _publishEndpoint.Publish(@event, cancellationToken);

        _logger.LogInformation(
            "NotificationSent event published for order {OrderId} via {Channel}",
            orderId, channel);
    }
}
