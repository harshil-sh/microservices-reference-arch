using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Application.Commands.UpdateOrderStatus;
using Orders.Domain.Enums;
using Shared.Contracts.Events;

namespace Orders.Infrastructure.Messaging.Consumers;

public class StockInsufficientConsumer : IConsumer<StockInsufficient>
{
    private readonly IMediator _mediator;
    private readonly ILogger<StockInsufficientConsumer> _logger;

    public StockInsufficientConsumer(IMediator mediator, ILogger<StockInsufficientConsumer> logger)
    {
        _mediator = mediator;
        _logger = logger;
    }

    public async Task Consume(ConsumeContext<StockInsufficient> context)
    {
        var message = context.Message;

        _logger.LogWarning(
            "Received StockInsufficient event for order {OrderId}: {Reason}. CorrelationId: {CorrelationId}",
            message.OrderId, message.Reason, message.CorrelationId);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = message.OrderId,
            NewStatus = OrderStatus.Failed,
            Reason = message.Reason,
            CorrelationId = message.CorrelationId
        };

        await _mediator.Send(command, context.CancellationToken);
    }
}
