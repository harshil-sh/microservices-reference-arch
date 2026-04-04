namespace Shared.Contracts.Events;

public record OrderPlaced : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    public int Version { get; init; } = 1;

    public Guid OrderId { get; init; }
    public Guid CustomerId { get; init; }
    public List<OrderPlacedItem> Items { get; init; } = [];
    public decimal TotalAmount { get; init; }
    public DateTime PlacedAt { get; init; }
}

public record OrderPlacedItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
    public decimal UnitPrice { get; init; }
}
