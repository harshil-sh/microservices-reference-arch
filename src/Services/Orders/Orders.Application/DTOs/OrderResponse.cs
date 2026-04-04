using Orders.Domain.Enums;

namespace Orders.Application.DTOs;

public record OrderResponse
{
    public Guid Id { get; init; }
    public Guid CustomerId { get; init; }
    public OrderStatus Status { get; init; }
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; }
    public DateTime? ConfirmedAt { get; init; }
    public DateTime? FailedAt { get; init; }
    public string? FailureReason { get; init; }
    public List<OrderItemResponse> Items { get; init; } = [];
}

public record OrderItemResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
