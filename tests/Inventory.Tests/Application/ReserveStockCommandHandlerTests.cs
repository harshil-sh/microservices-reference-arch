using Inventory.Application.Commands.ReserveStock;
using Inventory.Application.Interfaces;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace Inventory.Tests.Application;

public class ReserveStockCommandHandlerTests
{
    private readonly Mock<IInventoryRepository> _repositoryMock = new();
    private readonly Mock<IEventPublisher> _publisherMock = new();
    private readonly Mock<ILogger<ReserveStockCommandHandler>> _loggerMock = new();
    private readonly ReserveStockCommandHandler _handler;

    public ReserveStockCommandHandlerTests()
    {
        _handler = new ReserveStockCommandHandler(
            _repositoryMock.Object,
            _publisherMock.Object,
            _loggerMock.Object);
    }

    private static ReserveStockCommand CreateCommand(params (Guid productId, int quantity)[] items) => new()
    {
        OrderId = Guid.NewGuid(),
        Items = items.Select(i => new ReserveStockItem
        {
            ProductId = i.productId,
            ProductName = $"Product-{i.productId.ToString()[..4]}",
            Quantity = i.quantity
        }).ToList(),
        CorrelationId = "test-corr"
    };

    [Fact]
    public async Task Handle_AllItemsAvailable_ReservesAll_PublishesStockReserved()
    {
        var productId = Guid.NewGuid();
        var inventoryItem = InventoryItem.Create(productId, "Widget", 100);
        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);
        _repositoryMock
            .Setup(r => r.GetReservationsByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockReservation>());

        var command = CreateCommand((productId, 10));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(90, inventoryItem.AvailableStock);
        Assert.Equal(10, inventoryItem.ReservedStock);
        _repositoryMock.Verify(r => r.UpdateAsync(inventoryItem, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(r => r.AddReservationAsync(It.IsAny<StockReservation>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishStockReservedAsync(
            command.OrderId,
            It.IsAny<DateTime>(),
            It.Is<List<(Guid, int)>>(items => items.Count == 1 && items[0].Item1 == productId && items[0].Item2 == 10),
            "test-corr",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ProductNotFound_PublishesStockInsufficient()
    {
        var productId = Guid.NewGuid();
        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);
        _repositoryMock
            .Setup(r => r.GetReservationsByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockReservation>());

        var command = CreateCommand((productId, 5));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result);
        _publisherMock.Verify(p => p.PublishStockInsufficientAsync(
            command.OrderId,
            It.IsAny<DateTime>(),
            It.Is<string>(s => s.Contains("not found")),
            "test-corr",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientStock_PublishesStockInsufficient()
    {
        var productId = Guid.NewGuid();
        var inventoryItem = InventoryItem.Create(productId, "Widget", 3);
        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryItem);
        _repositoryMock
            .Setup(r => r.GetReservationsByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockReservation>());

        var command = CreateCommand((productId, 10));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result);
        _publisherMock.Verify(p => p.PublishStockInsufficientAsync(
            command.OrderId,
            It.IsAny<DateTime>(),
            It.Is<string>(s => s.Contains("Insufficient stock")),
            "test-corr",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_PartialFailure_RollsBackPreviousReservations()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var inventoryA = InventoryItem.Create(productA, "A", 100);
        var inventoryB = InventoryItem.Create(productB, "B", 2); // not enough

        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryA);
        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryB);
        _repositoryMock
            .Setup(r => r.GetReservationsByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockReservation>());

        var command = CreateCommand((productA, 10), (productB, 5));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result);
        // Product A should be rolled back: 100 - 10 + 10 = 100
        Assert.Equal(100, inventoryA.AvailableStock);
        Assert.Equal(0, inventoryA.ReservedStock);
        // Verify rollback update was called for product A
        _repositoryMock.Verify(
            r => r.UpdateAsync(inventoryA, It.IsAny<CancellationToken>()),
            Times.Exactly(2)); // once for reserve, once for rollback
    }

    [Fact]
    public async Task Handle_DuplicateOrder_SkipsProcessing_ReturnsTrue()
    {
        var existingReservation = StockReservation.Create(Guid.NewGuid(), Guid.NewGuid(), 5);
        _repositoryMock
            .Setup(r => r.GetReservationsByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockReservation> { existingReservation });

        var command = CreateCommand((Guid.NewGuid(), 10));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<InventoryItem>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_MultipleItems_CreatesReservationForEach()
    {
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();
        var inventoryA = InventoryItem.Create(productA, "A", 100);
        var inventoryB = InventoryItem.Create(productB, "B", 100);

        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productA, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryA);
        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productB, It.IsAny<CancellationToken>()))
            .ReturnsAsync(inventoryB);
        _repositoryMock
            .Setup(r => r.GetReservationsByOrderIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<StockReservation>());

        var command = CreateCommand((productA, 10), (productB, 20));

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        _repositoryMock.Verify(
            r => r.AddReservationAsync(It.IsAny<StockReservation>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2));
        Assert.Equal(90, inventoryA.AvailableStock);
        Assert.Equal(80, inventoryB.AvailableStock);
    }
}
