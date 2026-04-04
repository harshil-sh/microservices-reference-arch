using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Application.Interfaces;
using Orders.Application.Metrics;
using Orders.Domain.Enums;
using Orders.Domain.Repositories;

namespace Orders.Application.Commands.UpdateOrderStatus;

public class UpdateOrderStatusCommandHandler : IRequestHandler<UpdateOrderStatusCommand, bool>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<UpdateOrderStatusCommandHandler> _logger;
    private readonly OrdersMetrics _metrics;

    public UpdateOrderStatusCommandHandler(
        IOrderRepository orderRepository,
        IEventPublisher eventPublisher,
        ILogger<UpdateOrderStatusCommandHandler> logger,
        OrdersMetrics metrics)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool> Handle(UpdateOrderStatusCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdAsync(request.OrderId, cancellationToken);
        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for status update", request.OrderId);
            return false;
        }

        // Idempotency: if order is already in the target state, skip processing
        if (order.Status == request.NewStatus)
        {
            _logger.LogInformation(
                "Order {OrderId} already in {Status} status — skipping duplicate event",
                request.OrderId, request.NewStatus);
            return true;
        }

        switch (request.NewStatus)
        {
            case OrderStatus.Confirmed:
                order.Confirm();
                await _orderRepository.UpdateAsync(order, cancellationToken);

                _metrics.OrderConfirmed();
                _logger.LogInformation("Order {OrderId} confirmed", order.Id);

                await _eventPublisher.PublishOrderConfirmedAsync(
                    order.Id,
                    order.ConfirmedAt!.Value,
                    request.CorrelationId,
                    cancellationToken);
                break;

            case OrderStatus.Failed:
                order.Fail(request.Reason ?? "Unknown reason");
                await _orderRepository.UpdateAsync(order, cancellationToken);

                _metrics.OrderFailed();
                _logger.LogInformation("Order {OrderId} failed: {Reason}", order.Id, request.Reason);

                await _eventPublisher.PublishOrderFailedAsync(
                    order.Id,
                    order.FailedAt!.Value,
                    request.Reason ?? "Unknown reason",
                    request.CorrelationId,
                    cancellationToken);
                break;

            default:
                _logger.LogWarning("Invalid status transition to {Status} for order {OrderId}",
                    request.NewStatus, request.OrderId);
                return false;
        }

        return true;
    }
}
