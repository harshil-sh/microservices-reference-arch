using MassTransit;
using Inventory.Application.Interfaces;
using Shared.Contracts.Events;

namespace Inventory.Infrastructure.Messaging.Publishers;

public class InventoryEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public InventoryEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishStockReservedAsync(
        Guid orderId,
        DateTime reservedAt,
        List<(Guid ProductId, int Quantity)> items,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var @event = new StockReserved
        {
            OrderId = orderId,
            ReservedAt = reservedAt,
            Items = items.Select(i => new ReservedItem
            {
                ProductId = i.ProductId,
                Quantity = i.Quantity
            }).ToList(),
            CorrelationId = correlationId
        };

        await _publishEndpoint.Publish(@event, cancellationToken);
    }

    public async Task PublishStockInsufficientAsync(
        Guid orderId,
        DateTime failedAt,
        string reason,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var @event = new StockInsufficient
        {
            OrderId = orderId,
            FailedAt = failedAt,
            Reason = reason,
            CorrelationId = correlationId
        };

        await _publishEndpoint.Publish(@event, cancellationToken);
    }
}
