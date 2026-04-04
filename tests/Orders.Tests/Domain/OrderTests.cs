using Orders.Domain.Entities;
using Orders.Domain.Enums;

namespace Orders.Tests.Domain;

public class OrderTests
{
    private static List<OrderItem> CreateValidItems(int count = 1)
    {
        return Enumerable.Range(1, count)
            .Select(i => OrderItem.Create(Guid.NewGuid(), $"Product {i}", i, 10.00m * i))
            .ToList();
    }

    [Fact]
    public void Create_WithValidInputs_ReturnsOrderInPendingStatus()
    {
        var customerId = Guid.NewGuid();
        var items = CreateValidItems();

        var order = Order.Create(customerId, items);

        Assert.Equal(customerId, order.CustomerId);
        Assert.Equal(OrderStatus.Pending, order.Status);
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Null(order.ConfirmedAt);
        Assert.Null(order.FailedAt);
        Assert.Null(order.FailureReason);
    }

    [Fact]
    public void Create_WithEmptyCustomerId_ThrowsArgumentException()
    {
        var items = CreateValidItems();

        var ex = Assert.Throws<ArgumentException>(() => Order.Create(Guid.Empty, items));
        Assert.Contains("Customer ID", ex.Message);
    }

    [Fact]
    public void Create_WithNullItems_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Order.Create(Guid.NewGuid(), null!));
        Assert.Contains("order item", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyItemsList_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => Order.Create(Guid.NewGuid(), []));
        Assert.Contains("order item", ex.Message);
    }

    [Fact]
    public void Create_CalculatesTotalAmount_FromItemQuantityTimesUnitPrice()
    {
        var items = new List<OrderItem>
        {
            OrderItem.Create(Guid.NewGuid(), "A", 2, 10.00m),
            OrderItem.Create(Guid.NewGuid(), "B", 3, 5.00m)
        };

        var order = Order.Create(Guid.NewGuid(), items);

        Assert.Equal(35.00m, order.TotalAmount); // (2*10) + (3*5)
    }

    [Fact]
    public void Create_SetsPlacedAtToApproximateUtcNow()
    {
        var before = DateTime.UtcNow;
        var order = Order.Create(Guid.NewGuid(), CreateValidItems());
        var after = DateTime.UtcNow;

        Assert.InRange(order.PlacedAt, before, after);
    }

    [Fact]
    public void Create_AssignsOrderIdToAllItems()
    {
        var items = CreateValidItems(3);

        var order = Order.Create(Guid.NewGuid(), items);

        Assert.All(order.Items, item => Assert.Equal(order.Id, item.OrderId));
    }

    [Fact]
    public void Create_WithMultipleItems_AllItemsIncluded()
    {
        var items = CreateValidItems(5);

        var order = Order.Create(Guid.NewGuid(), items);

        Assert.Equal(5, order.Items.Count);
    }

    [Fact]
    public void Create_GeneratesUniqueId()
    {
        var items1 = CreateValidItems();
        var items2 = CreateValidItems();

        var order1 = Order.Create(Guid.NewGuid(), items1);
        var order2 = Order.Create(Guid.NewGuid(), items2);

        Assert.NotEqual(order1.Id, order2.Id);
    }

    [Fact]
    public void Confirm_WhenPending_SetsConfirmedStatusAndTimestamp()
    {
        var order = Order.Create(Guid.NewGuid(), CreateValidItems());
        var before = DateTime.UtcNow;

        order.Confirm();

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.NotNull(order.ConfirmedAt);
        Assert.InRange(order.ConfirmedAt.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void Confirm_WhenAlreadyConfirmed_IsIdempotent()
    {
        var order = Order.Create(Guid.NewGuid(), CreateValidItems());
        order.Confirm();
        var firstConfirmedAt = order.ConfirmedAt;

        order.Confirm(); // should not throw

        Assert.Equal(OrderStatus.Confirmed, order.Status);
        Assert.Equal(firstConfirmedAt, order.ConfirmedAt);
    }

    [Fact]
    public void Confirm_WhenFailed_ThrowsInvalidOperationException()
    {
        var order = Order.Create(Guid.NewGuid(), CreateValidItems());
        order.Fail("some reason");

        Assert.Throws<InvalidOperationException>(() => order.Confirm());
    }

    [Fact]
    public void Fail_WhenPending_SetsFailedStatusReasonAndTimestamp()
    {
        var order = Order.Create(Guid.NewGuid(), CreateValidItems());
        var before = DateTime.UtcNow;

        order.Fail("Insufficient stock");

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal("Insufficient stock", order.FailureReason);
        Assert.NotNull(order.FailedAt);
        Assert.InRange(order.FailedAt.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void Fail_WhenAlreadyFailed_IsIdempotent()
    {
        var order = Order.Create(Guid.NewGuid(), CreateValidItems());
        order.Fail("First reason");
        var firstFailedAt = order.FailedAt;

        order.Fail("Second reason"); // should not throw

        Assert.Equal(OrderStatus.Failed, order.Status);
        Assert.Equal(firstFailedAt, order.FailedAt);
    }

    [Fact]
    public void Fail_WhenConfirmed_ThrowsInvalidOperationException()
    {
        var order = Order.Create(Guid.NewGuid(), CreateValidItems());
        order.Confirm();

        Assert.Throws<InvalidOperationException>(() => order.Fail("reason"));
    }
}
