using MediatR;

namespace Inventory.Application.Commands.ReserveStock;

public record ReserveStockCommand : IRequest<bool>
{
    public Guid OrderId { get; init; }
    public List<ReserveStockItem> Items { get; init; } = [];
    public string CorrelationId { get; init; } = string.Empty;
}

public record ReserveStockItem
{
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int Quantity { get; init; }
}
