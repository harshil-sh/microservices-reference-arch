using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Application.Commands.UpdateOrderStatus;
using Orders.Domain.Enums;
using Shared.Contracts.Events;

namespace Orders.Infrastructure.Messaging.Consumers;

public class StockReservedConsumer : IConsumer<StockReserved>
{
    private readonly IMediator _mediator;
    private readonly ILogger<StockReservedConsumer> _logger;

    public StockReservedConsumer(IMediator mediator, ILogger<StockReservedConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockReserved> context)
    {
        var message = context.Message;

        _logger.LogInformation(
            "Received StockReserved event for order {OrderId}. CorrelationId: {CorrelationId}",
            message.OrderId, message.CorrelationId);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = message.OrderId,
            NewStatus = OrderStatus.Confirmed,
            CorrelationId = message.CorrelationId
        };

        await _mediator.Send(command, context.CancellationToken);
    }
}
