using Inventory.Domain.Entities;

namespace Inventory.Domain.Repositories;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default);
    Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    Task AddReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StockReservation>> GetReservationsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default);
}
