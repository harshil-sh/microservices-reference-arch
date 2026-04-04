namespace Inventory.Application.Interfaces;

public interface IEventPublisher
{
    Task PublishStockReservedAsync(
        Guid orderId,
        DateTime reservedAt,
        List<(Guid ProductId, int Quantity)> items,
        string correlationId,
        CancellationToken cancellationToken = default);

    Task PublishStockInsufficientAsync(
        Guid orderId,
        DateTime failedAt,
        string reason,
        string correlationId,
        CancellationToken cancellationToken = default);
}
