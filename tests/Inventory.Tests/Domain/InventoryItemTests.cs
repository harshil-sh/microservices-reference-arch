using Inventory.Domain.Entities;

namespace Inventory.Tests.Domain;

public class InventoryItemTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsItem()
    {
        var productId = Guid.NewGuid();

        var item = InventoryItem.Create(productId, "Widget", 100);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Widget", item.ProductName);
        Assert.Equal(100, item.AvailableStock);
        Assert.Equal(0, item.ReservedStock);
    }

    [Fact]
    public void Create_WithEmptyProductId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => InventoryItem.Create(Guid.Empty, "Widget", 100));
        Assert.Contains("Product ID", ex.Message);
    }

    [Fact]
    public void Create_WithNullProductName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => InventoryItem.Create(Guid.NewGuid(), null!, 100));
        Assert.Contains("Product name", ex.Message);
    }

    [Fact]
    public void Create_WithWhitespaceProductName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => InventoryItem.Create(Guid.NewGuid(), "   ", 100));
        Assert.Contains("Product name", ex.Message);
    }

    [Fact]
    public void Create_WithNegativeStock_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => InventoryItem.Create(Guid.NewGuid(), "Widget", -1));
    }

    [Fact]
    public void Create_WithZeroStock_Succeeds()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 0);

        Assert.Equal(0, item.AvailableStock);
    }

    [Fact]
    public void CanReserve_WhenSufficientStock_ReturnsTrue()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 10);

        Assert.True(item.CanReserve(5));
    }

    [Fact]
    public void CanReserve_WhenExactStock_ReturnsTrue()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 10);

        Assert.True(item.CanReserve(10));
    }

    [Fact]
    public void CanReserve_WhenInsufficientStock_ReturnsFalse()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 5);

        Assert.False(item.CanReserve(6));
    }

    [Fact]
    public void Reserve_ReducesAvailableAndIncreasesReserved()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 100);

        item.Reserve(30);

        Assert.Equal(70, item.AvailableStock);
        Assert.Equal(30, item.ReservedStock);
    }

    [Fact]
    public void Reserve_WhenInsufficientStock_ThrowsInvalidOperationException()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 5);

        var ex = Assert.Throws<InvalidOperationException>(() => item.Reserve(6));
        Assert.Contains("Insufficient stock", ex.Message);
    }

    [Fact]
    public void Reserve_UpdatesLastUpdatedAt()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 100);
        var before = DateTime.UtcNow;

        item.Reserve(10);

        Assert.InRange(item.LastUpdatedAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void ReleaseReservation_IncreasesAvailableAndReducesReserved()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 100);
        item.Reserve(30);

        item.ReleaseReservation(10);

        Assert.Equal(80, item.AvailableStock);
        Assert.Equal(20, item.ReservedStock);
    }

    [Fact]
    public void ReleaseReservation_WhenInsufficientReserved_ThrowsInvalidOperationException()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 100);
        item.Reserve(5);

        var ex = Assert.Throws<InvalidOperationException>(() => item.ReleaseReservation(6));
        Assert.Contains("Cannot release", ex.Message);
    }

    [Fact]
    public void ReleaseReservation_UpdatesLastUpdatedAt()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 100);
        item.Reserve(20);
        var before = DateTime.UtcNow;

        item.ReleaseReservation(10);

        Assert.InRange(item.LastUpdatedAt, before, DateTime.UtcNow);
    }

    [Fact]
    public void AddStock_WithPositiveQuantity_IncreasesAvailable()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 50);

        item.AddStock(25);

        Assert.Equal(75, item.AvailableStock);
    }

    [Fact]
    public void AddStock_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 50);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.AddStock(0));
    }

    [Fact]
    public void AddStock_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        var item = InventoryItem.Create(Guid.NewGuid(), "Widget", 50);

        Assert.Throws<ArgumentOutOfRangeException>(() => item.AddStock(-5));
    }
}
