using Inventory.Application.Mappings;
using Inventory.Domain.Entities;

namespace Inventory.Tests.Application;

public class InventoryMappingsTests
{
    [Fact]
    public void ToResponse_MapsAllFieldsCorrectly()
    {
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "Widget", 100);
        item.Reserve(20);

        var response = item.ToResponse();

        Assert.Equal(item.Id, response.Id);
        Assert.Equal(productId, response.ProductId);
        Assert.Equal("Widget", response.ProductName);
        Assert.Equal(80, response.AvailableStock);
        Assert.Equal(20, response.ReservedStock);
        Assert.Equal(item.LastUpdatedAt, response.LastUpdatedAt);
    }
}
