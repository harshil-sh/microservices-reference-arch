namespace Inventory.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty;
    public int AvailableStock { get; private set; }
    public int ReservedStock { get; private set; }
    public DateTime LastUpdatedAt { get; private set; }

    private InventoryItem() { }

    public static InventoryItem Create(Guid productId, string productName, int availableStock)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));

        if (string.IsNullOrWhiteSpace(productName))
            throw new ArgumentException("Product name is required.", nameof(productName));

        if (availableStock < 0)
            throw new ArgumentOutOfRangeException(nameof(availableStock), "Available stock cannot be negative.");

        return new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            ProductName = productName,
            AvailableStock = availableStock,
            ReservedStock = 0,
            LastUpdatedAt = DateTime.UtcNow
        };
    }

    public bool CanReserve(int quantity)
    {
        return AvailableStock >= quantity;
    }

    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new InvalidOperationException(
                $"Insufficient stock for product '{ProductName}'. Available: {AvailableStock}, Requested: {quantity}");

        AvailableStock -= quantity;
        ReservedStock += quantity;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void ReleaseReservation(int quantity)
    {
        if (ReservedStock < quantity)
            throw new InvalidOperationException(
                $"Cannot release {quantity} units. Only {ReservedStock} reserved for product '{ProductName}'.");

        ReservedStock -= quantity;
        AvailableStock += quantity;
        LastUpdatedAt = DateTime.UtcNow;
    }

    public void AddStock(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be positive.");

        AvailableStock += quantity;
        LastUpdatedAt = DateTime.UtcNow;
    }
}
