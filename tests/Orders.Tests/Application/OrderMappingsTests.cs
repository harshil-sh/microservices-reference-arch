using Orders.Application.DTOs;
using Orders.Application.Mappings;
using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Tests.Application;

public class OrderMappingsTests
{
    [Fact]
    public void ToResponse_MapsAllOrderFieldsCorrectly()
    {
        var order = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create(Guid.NewGuid(), "Widget", 2, 15.00m)]);

        var response = order.ToResponse();

        Assert.Equal(order.Id, response.Id);
        Assert.Equal(order.CustomerId, response.CustomerId);
        Assert.Equal(order.Status, response.Status);
        Assert.Equal(order.TotalAmount, response.TotalAmount);
        Assert.Equal(order.PlacedAt, response.PlacedAt);
        Assert.Equal(order.ConfirmedAt, response.ConfirmedAt);
        Assert.Equal(order.FailedAt, response.FailedAt);
        Assert.Equal(order.FailureReason, response.FailureReason);
    }

    [Fact]
    public void ToResponse_MapsAllItemFieldsCorrectly()
    {
        var productId = Guid.NewGuid();
        var order = Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create(productId, "Widget", 3, 9.99m)]);

        var response = order.ToResponse();
        var item = Assert.Single(response.Items);

        Assert.Equal(productId, item.ProductId);
        Assert.Equal("Widget", item.ProductName);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(9.99m, item.UnitPrice);
    }
}
