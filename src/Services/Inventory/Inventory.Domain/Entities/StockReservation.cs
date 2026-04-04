using Inventory.Domain.Enums;

namespace Inventory.Domain.Entities;

public class StockReservation
{
    public Guid Id { get; private set; }
    public Guid OrderId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public ReservationStatus Status { get; private set; }
    public DateTime ReservedAt { get; private set; }
    public DateTime? ReleasedAt { get; private set; }

    private StockReservation() { }

    public static StockReservation Create(Guid orderId, Guid productId, int quantity)
    {
        if (orderId == Guid.Empty)
            throw new ArgumentException("Order ID is required.", nameof(orderId));

        if (productId == Guid.Empty)
            throw new ArgumentException("Product ID is required.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be greater than zero.");

        return new StockReservation
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ProductId = productId,
            Quantity = quantity,
            Status = ReservationStatus.Reserved,
            ReservedAt = DateTime.UtcNow
        };
    }

    public void Release()
    {
        if (Status != ReservationStatus.Reserved)
            throw new InvalidOperationException($"Cannot release reservation in '{Status}' status.");

        Status = ReservationStatus.Released;
        ReleasedAt = DateTime.UtcNow;
    }

    public void Confirm()
    {
        if (Status != ReservationStatus.Reserved)
            throw new InvalidOperationException($"Cannot confirm reservation in '{Status}' status.");

        Status = ReservationStatus.Confirmed;
    }
}
