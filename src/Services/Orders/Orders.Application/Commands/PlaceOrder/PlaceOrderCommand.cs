using MediatR;
using Orders.Application.DTOs;

namespace Orders.Application.Commands.PlaceOrder;

public record PlaceOrderCommand : IRequest<OrderResponse>
{
    public Guid CustomerId { get; init; }
    public List<OrderItemRequest> Items { get; init; } = [];
    public string CorrelationId { get; init; } = string.Empty;
}
