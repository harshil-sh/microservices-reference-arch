namespace Orders.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishOrderPlacedAsync(
        Guid orderId,
        Guid customerId,
        List<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> items,
        decimal totalAmount,
        DateTime placedAt,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task PublishOrderConfirmedAsync(
        Guid orderId,
        DateTime confirmedAt,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task PublishOrderFailedAsync(
        Guid orderId,
        DateTime failedAt,
        string reason,
        string correlationId,
        CancellationToken cancellationToken = default);
}
