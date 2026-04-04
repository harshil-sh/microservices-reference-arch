namespace Inventory.Application.DTOs;

public record StockReservationResponse
{
    public Guid Id { get; init; }
    public Guid OrderId { get; init; }
    public Guid ProductId { get; init; }
    public int Quantity { get; init; }
    public string Status { get; init; } = string.Empty;
    public DateTime ReservedAt { get; init; }
    public DateTime? ReleasedAt { get; init; }
}
