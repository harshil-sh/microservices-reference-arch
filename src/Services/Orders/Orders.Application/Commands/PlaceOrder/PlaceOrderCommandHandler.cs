using MediatR;
using Microsoft.Extensions.Logging;
using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Application.Mappings;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Application.Commands.PlaceOrder;

public class PlaceOrderCommandHandler : IRequestHandler<PlaceOrderCommand, OrderResponse>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<PlaceOrderCommandHandler> _logger;

    public PlaceOrderCommandHandler(
        IOrderRepository orderRepository,
        IEventPublisher eventPublisher,
        ILogger<PlaceOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
    }

    public async Task<OrderResponse> Handle(PlaceOrderCommand request, CancellationToken cancellationToken)
    {
        var orderItems = request.Items.Select(i =>
            OrderItem.Create(i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        var order = Order.Create(request.CustomerId, orderItems);

        await _orderRepository.AddAsync(order, cancellationToken);

        _logger.LogInformation(
            "Order {OrderId} placed for customer {CustomerId} with total {TotalAmount}",
            order.Id, order.CustomerId, order.TotalAmount);

        var items = order.Items
            .Select(i => (i.ProductId, i.ProductName, i.Quantity, i.UnitPrice))
            .ToList();

        await _eventPublisher.PublishOrderPlacedAsync(
            order.Id,
            order.CustomerId,
            items,
            order.TotalAmount,
            order.PlacedAt,
            request.CorrelationId,
            cancellationToken);

        return order.ToResponse();
    }
}
