using Inventory.Application.Commands.ReserveStock;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Shared.Contracts.Events;

namespace Inventory.Infrastructure.Messaging.Consumers;

public class OrderPlacedConsumer : IConsumer<OrderPlaced>
{
    private readonly IMediator _mediator;
    private readonly ILogger<OrderPlacedConsumer> _logger;

    public OrderPlacedConsumer(IMediator mediator, ILogger<OrderPlacedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<OrderPlaced> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received OrderPlaced event for order {OrderId} with {ItemCount} items. CorrelationId: {CorrelationId}",
            message.OrderId, message.Items.Count, message.CorrelationId);

        var command = new ReserveStockCommand
        {
            OrderId = message.OrderId,
            Items = message.Items.Select(i => new ReserveStockItem
            {
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity
            }).ToList(),
            CorrelationId = message.CorrelationId
        };

        await _mediator.Send(command, context.CancellationToken);
    }
}
