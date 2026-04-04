using Orders.Domain.Entities;

namespace Orders.Tests.Domain;

public class OrderItemTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsOrderItem()
    {
        var productId = Guid.NewGuid();

        var item = OrderItem.Create(productId, "Widget", 5, 9.99m);

        Assert.NotEqual(Guid.Empty, item.Id);
        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Widget", item.ProductName);
        Assert.Equal(5, item.Quantity);
        Assert.Equal(9.99m, item.UnitPrice);
    }

    [Fact]
    public void Create_WithEmptyProductId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => OrderItem.Create(Guid.Empty, "Widget", 1, 1.00m));
        Assert.Contains("Product ID", ex.Message);
    }

    [Fact]
    public void Create_WithNullProductName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => OrderItem.Create(Guid.NewGuid(), null!, 1, 1.00m));
        Assert.Contains("Product name", ex.Message);
    }

    [Fact]
    public void Create_WithWhitespaceProductName_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => OrderItem.Create(Guid.NewGuid(), "   ", 1, 1.00m));
        Assert.Contains("Product name", ex.Message);
    }

    [Fact]
    public void Create_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OrderItem.Create(Guid.NewGuid(), "Widget", 0, 1.00m));
    }

    [Fact]
    public void Create_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OrderItem.Create(Guid.NewGuid(), "Widget", -1, 1.00m));
    }

    [Fact]
    public void Create_WithZeroUnitPrice_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OrderItem.Create(Guid.NewGuid(), "Widget", 1, 0m));
    }

    [Fact]
    public void Create_WithNegativeUnitPrice_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => OrderItem.Create(Guid.NewGuid(), "Widget", 1, -5.00m));
    }
}
