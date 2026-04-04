namespace Inventory.Application.DTOs;

public record InventoryItemResponse
{
    public Guid Id { get; init; }
    public Guid ProductId { get; init; }
    public string ProductName { get; init; } = string.Empty;
    public int AvailableStock { get; init; }
    public int ReservedStock { get; init; }
    public DateTime LastUpdatedAt { get; init; }
}
