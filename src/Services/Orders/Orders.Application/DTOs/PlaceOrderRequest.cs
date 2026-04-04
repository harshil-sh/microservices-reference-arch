namespace Orders.Application.DTOs;

public record PlaceOrderRequest
{
    public Guid CustomerId { get; init; }
    public List<OrderItemRequest> Items { get; init; } = [];
}

public record OrderItemRequest
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
