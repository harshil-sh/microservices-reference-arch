using MassTransit;
using Orders.Application.Interfaces;
using Shared.Contracts.Events;

namespace Orders.Infrastructure.Messaging.Publishers;

public class OrderEventPublisher : IEventPublisher
{
    private readonly IPublishEndpoint _publishEndpoint;

    public OrderEventPublisher(IPublishEndpoint publishEndpoint)
    {
        _publishEndpoint = publishEndpoint;
    }

    public async Task PublishOrderPlacedAsync(
        Guid orderId,
        Guid customerId,
        List<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)> items,
        decimal totalAmount,
        DateTime placedAt,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var @event = new OrderPlaced
        {
            OrderId = orderId,
            CustomerId = customerId,
            Items = items.Select(i => new OrderPlacedItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList(),
            TotalAmount = totalAmount,
            PlacedAt = placedAt,
            CorrelationId = correlationId
        };

        await _publishEndpoint.Publish(@event, cancellationToken);
    }

    public async Task PublishOrderConfirmedAsync(
        Guid orderId,
        DateTime confirmedAt,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var @event = new OrderConfirmed
        {
            OrderId = orderId,
            ConfirmedAt = confirmedAt,
            CorrelationId = correlationId
        };

        await _publishEndpoint.Publish(@event, cancellationToken);
    }

    public async Task PublishOrderFailedAsync(
        Guid orderId,
        DateTime failedAt,
        string reason,
        string correlationId,
        CancellationToken cancellationToken = default)
    {
        var @event = new OrderFailed
        {
            OrderId = orderId,
            FailedAt = failedAt,
            Reason = reason,
            CorrelationId = correlationId
        };

        await _publishEndpoint.Publish(@event, cancellationToken);
    }
}
