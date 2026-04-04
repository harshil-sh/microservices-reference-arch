using Orders.Domain.Enums;

namespace Orders.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid CustomerId { get; private set; }
    public OrderStatus Status { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateTime PlacedAt { get; private set; }
    public DateTime? ConfirmedAt { get; private set; }
    public DateTime? FailedAt { get; private set; }
    public string? FailureReason { get; private set; }

    private readonly List<OrderItem> _items = [];
    public IReadOnlyCollection<OrderItem> Items => _items.AsReadOnly();

    private Order() { }

    public static Order Create(Guid customerId, List<OrderItem> items)
    {
        if (customerId == Guid.Empty)
            throw new ArgumentException("Customer ID is required.", nameof(customerId));

        if (items is null || items.Count == 0)
            throw new ArgumentException("At least one order item is required.", nameof(items));

        var order = new Order
        {
            Id = Guid.NewGuid(),
            CustomerId = customerId,
            Status = OrderStatus.Pending,
            PlacedAt = DateTime.UtcNow
        };

        foreach (var item in items)
        {
            item.SetOrderId(order.Id);
            order._items.Add(item);
        }

        order.TotalAmount = order._items.Sum(i => i.Quantity * i.UnitPrice);

        return order;
    }

    public void Confirm()
    {
        if (Status == OrderStatus.Confirmed)
            return;

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot confirm order in '{Status}' status.");

        Status = OrderStatus.Confirmed;
        ConfirmedAt = DateTime.UtcNow;
    }

    public void Fail(string reason)
    {
        if (Status == OrderStatus.Failed)
            return;

        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot fail order in '{Status}' status.");

        Status = OrderStatus.Failed;
        FailedAt = DateTime.UtcNow;
        FailureReason = reason;
    }
}
