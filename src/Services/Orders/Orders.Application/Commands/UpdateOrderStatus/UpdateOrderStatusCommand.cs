using MediatR;
using Orders.Domain.Enums;

namespace Orders.Application.Commands.UpdateOrderStatus;

public record UpdateOrderStatusCommand : IRequest<bool>
{
    public Guid OrderId { get; init; }
    public OrderStatus NewStatus { get; init; }
    public string? Reason { get; init; }
    public string CorrelationId { get; init; } = string.Empty;
}
