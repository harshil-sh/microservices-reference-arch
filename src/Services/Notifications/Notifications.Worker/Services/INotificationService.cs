namespace Notifications.Worker.Services;

public interface INotificationService
{
    Task SendOrderReceivedAsync(Guid orderId, Guid customerId, decimal totalAmount, string correlationId, CancellationToken cancellationToken = default);
    Task SendStockReservedAsync(Guid orderId, string correlationId, CancellationToken cancellationToken = default);
    Task SendOrderConfirmedAsync(Guid orderId, string correlationId, CancellationToken cancellationToken = default);
    Task SendOrderFailedAsync(Guid orderId, string reason, string correlationId, CancellationToken cancellationToken = default);
}
