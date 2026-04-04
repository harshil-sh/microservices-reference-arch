using Orders.Domain.Entities;
using Orders.Application.DTOs;

namespace Orders.Application.Mappings;

public static class OrderMappings
{
    public static OrderResponse ToResponse(this Order order)
    {
        return new OrderResponse
        {
            Id = order.Id,
            CustomerId = order.CustomerId,
            Status = order.Status,
            TotalAmount = order.TotalAmount,
            PlacedAt = order.PlacedAt,
            ConfirmedAt = order.ConfirmedAt,
            FailedAt = order.FailedAt,
            FailureReason = order.FailureReason,
            Items = order.Items.Select(i => new OrderItemResponse
            {
                Id = i.Id,
                ProductId = i.ProductId,
                ProductName = i.ProductName,
                Quantity = i.Quantity,
                UnitPrice = i.UnitPrice
            }).ToList()
        };
    }
}
