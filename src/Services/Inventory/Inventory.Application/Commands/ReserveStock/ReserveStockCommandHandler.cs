using MediatR;
using Microsoft.Extensions.Logging;
using Inventory.Application.Interfaces;
using Inventory.Application.Metrics;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;

namespace Inventory.Application.Commands.ReserveStock;

public class ReserveStockCommandHandler : IRequestHandler<ReserveStockCommand, bool>
{
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IEventPublisher _eventPublisher;
    private readonly ILogger<ReserveStockCommandHandler> _logger;
    private readonly InventoryMetrics _metrics;

    public ReserveStockCommandHandler(
        IInventoryRepository inventoryRepository,
        IEventPublisher eventPublisher,
        ILogger<ReserveStockCommandHandler> logger,
        InventoryMetrics metrics)
    {
        _inventoryRepository = inventoryRepository;
        _eventPublisher = eventPublisher;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<bool> Handle(ReserveStockCommand request, CancellationToken cancellationToken)
    {
        // Idempotency: if reservations already exist for this order, skip processing
        var existingReservations = await _inventoryRepository.GetReservationsByOrderIdAsync(request.OrderId, cancellationToken);
        if (existingReservations.Count > 0)
        {
            _logger.LogInformation(
                "Reservations already exist for order {OrderId} — skipping duplicate event",
                request.OrderId);
            return true;
        }

        var reservedItems = new List<(Guid ProductId, int Quantity)>();
        var reservations = new List<StockReservation>();

        foreach (var item in request.Items)
        {
            var inventoryItem = await _inventoryRepository.GetByProductIdAsync(item.ProductId, cancellationToken);

            if (inventoryItem is null || !inventoryItem.CanReserve(item.Quantity))
            {
                _logger.LogWarning(
                    "Insufficient stock for product {ProductId} (order {OrderId}). Available: {Available}, Requested: {Requested}",
                    item.ProductId, request.OrderId,
                    inventoryItem?.AvailableStock ?? 0, item.Quantity);

                foreach (var reserved in reservedItems)
                {
                    var rollbackItem = await _inventoryRepository.GetByProductIdAsync(reserved.ProductId, cancellationToken);
                    rollbackItem?.ReleaseReservation(reserved.Quantity);
                    if (rollbackItem is not null)
                        await _inventoryRepository.UpdateAsync(rollbackItem, cancellationToken);
                }

                _metrics.StockRollback();

                var reason = inventoryItem is null
                    ? $"Product {item.ProductId} not found in inventory"
                    : $"Insufficient stock for product '{inventoryItem.ProductName}'. Available: {inventoryItem.AvailableStock}, Requested: {item.Quantity}";

                await _eventPublisher.PublishStockInsufficientAsync(
                    request.OrderId,
                    DateTime.UtcNow,
                    reason,
                    request.CorrelationId,
                    cancellationToken);

                _metrics.StockInsufficient();

                return false;
            }

            inventoryItem.Reserve(item.Quantity);
            await _inventoryRepository.UpdateAsync(inventoryItem, cancellationToken);

            var reservation = StockReservation.Create(request.OrderId, item.ProductId, item.Quantity);
            await _inventoryRepository.AddReservationAsync(reservation, cancellationToken);

            reservedItems.Add((item.ProductId, item.Quantity));
            reservations.Add(reservation);
        }

        _logger.LogInformation(
            "Stock reserved for order {OrderId} — {ItemCount} items",
            request.OrderId, reservedItems.Count);

        _metrics.StockReserved();

        await _eventPublisher.PublishStockReservedAsync(
            request.OrderId,
            DateTime.UtcNow,
            reservedItems,
            request.CorrelationId,
            cancellationToken);

        return true;
    }
}
