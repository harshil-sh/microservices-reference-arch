using MediatR;
using Orders.Application.DTOs;

namespace Orders.Application.Queries.GetAllOrders;

public record GetAllOrdersQuery : IRequest<IReadOnlyList<OrderResponse>>;
