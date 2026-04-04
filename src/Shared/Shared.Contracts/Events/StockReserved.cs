namespace Shared.Contracts.Events;

public record StockReserved : IIntegrationEvent
{
    public Guid EventId { get; init; } = Guid.NewGuid();
    public DateTime OccurredAt { get; init; } = DateTime.UtcNow;
    public string CorrelationId { get; init; } = string.Empty;
    public int Version { get; init; } = 1;

    public Guid OrderId { get; init; }
    public DateTime ReservedAt { get; init; }
    public List<ReservedItem> Items { get; init; } = [];
}

public record ReservedItem
{
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
}
