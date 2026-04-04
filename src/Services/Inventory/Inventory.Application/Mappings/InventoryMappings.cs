using Inventory.Application.DTOs;
using Inventory.Domain.Entities;

namespace Inventory.Application.Mappings;

public static class InventoryMappings
{
    public static InventoryItemResponse ToResponse(this InventoryItem item)
    {
        return new InventoryItemResponse
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductName = item.ProductName,
            AvailableStock = item.AvailableStock,
            ReservedStock = item.ReservedStock,
            LastUpdatedAt = item.LastUpdatedAt
        };
    }
}
