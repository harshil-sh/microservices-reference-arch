using Inventory.Application.DTOs;
using Inventory.Application.Queries.GetAllInventory;
using Inventory.Application.Queries.GetInventoryItem;
using Inventory.Domain.Entities;
using Inventory.Domain.Repositories;
using Moq;

namespace Inventory.Tests.Application;

public class QueryHandlerTests
{
    private readonly Mock<IInventoryRepository> _repositoryMock = new();

    [Fact]
    public async Task GetInventoryItem_WhenExists_ReturnsMappedResponse()
    {
        var productId = Guid.NewGuid();
        var item = InventoryItem.Create(productId, "Widget", 50);
        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(productId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(item);

        var handler = new GetInventoryItemQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetInventoryItemQuery(productId), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(productId, result!.ProductId);
        Assert.Equal("Widget", result.ProductName);
        Assert.Equal(50, result.AvailableStock);
    }

    [Fact]
    public async Task GetInventoryItem_WhenNotFound_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByProductIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((InventoryItem?)null);

        var handler = new GetInventoryItemQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetInventoryItemQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllInventory_WhenItemsExist_ReturnsMappedList()
    {
        var items = new List<InventoryItem>
        {
            InventoryItem.Create(Guid.NewGuid(), "A", 10),
            InventoryItem.Create(Guid.NewGuid(), "B", 20)
        };
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(items);

        var handler = new GetAllInventoryQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetAllInventoryQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllInventory_WhenEmpty_ReturnsEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<InventoryItem>());

        var handler = new GetAllInventoryQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetAllInventoryQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
