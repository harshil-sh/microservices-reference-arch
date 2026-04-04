using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Inventory.Infrastructure.Persistence.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly InventoryDbContext _context;

    public InventoryRepository(InventoryDbContext context)
    {
        _context = context;
    }

    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId, CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .FirstOrDefaultAsync(i => i.ProductId == productId, cancellationToken);
    }

    public async Task<IReadOnlyList<InventoryItem>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.InventoryItems
            .OrderBy(i => i.ProductName)
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        await _context.InventoryItems.AddAsync(item, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateAsync(InventoryItem item, CancellationToken cancellationToken = default)
    {
        _context.InventoryItems.Update(item);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var item = await _context.InventoryItems.FindAsync([id], cancellationToken);
        if (item is not null)
        {
            _context.InventoryItems.Remove(item);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task AddReservationAsync(StockReservation reservation, CancellationToken cancellationToken = default)
    {
        await _context.StockReservations.AddAsync(reservation, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<StockReservation>> GetReservationsByOrderIdAsync(Guid orderId, CancellationToken cancellationToken = default)
    {
        return await _context.StockReservations
            .Where(r => r.OrderId == orderId)
            .ToListAsync(cancellationToken);
    }
}
