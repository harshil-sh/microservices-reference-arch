using Inventory.Domain.Entities;
using Inventory.Domain.Enums;

namespace Inventory.Tests.Domain;

public class StockReservationTests
{
    [Fact]
    public void Create_WithValidInputs_ReturnsReservation_InReservedStatus()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();

        var reservation = StockReservation.Create(orderId, productId, 5);

        Assert.NotEqual(Guid.Empty, reservation.Id);
        Assert.Equal(orderId, reservation.OrderId);
        Assert.Equal(productId, reservation.ProductId);
        Assert.Equal(5, reservation.Quantity);
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Null(reservation.ReleasedAt);
    }

    [Fact]
    public void Create_WithEmptyOrderId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => StockReservation.Create(Guid.Empty, Guid.NewGuid(), 5));
        Assert.Contains("Order ID", ex.Message);
    }

    [Fact]
    public void Create_WithEmptyProductId_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(
            () => StockReservation.Create(Guid.NewGuid(), Guid.Empty, 5));
        Assert.Contains("Product ID", ex.Message);
    }

    [Fact]
    public void Create_WithZeroQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 0));
    }

    [Fact]
    public void Create_WithNegativeQuantity_ThrowsArgumentOutOfRangeException()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), -1));
    }

    [Fact]
    public void Release_WhenReserved_SetsReleasedStatusAndTimestamp()
    {
        var reservation = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 5);
        var before = DateTime.UtcNow;

        reservation.Release();

        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.NotNull(reservation.ReleasedAt);
        Assert.InRange(reservation.ReleasedAt.Value, before, DateTime.UtcNow);
    }

    [Fact]
    public void Release_WhenAlreadyReleased_ThrowsInvalidOperationException()
    {
        var reservation = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 5);
        reservation.Release();

        Assert.Throws<InvalidOperationException>(() => reservation.Release());
    }

    [Fact]
    public void Confirm_WhenReserved_SetsConfirmedStatus()
    {
        var reservation = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 5);

        reservation.Confirm();

        Assert.Equal(ReservationStatus.Confirmed, reservation.Status);
    }

    [Fact]
    public void Confirm_WhenNotReserved_ThrowsInvalidOperationException()
    {
        var reservation = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 5);
        reservation.Release();

        Assert.Throws<InvalidOperationException>(() => reservation.Confirm());
    }
}
