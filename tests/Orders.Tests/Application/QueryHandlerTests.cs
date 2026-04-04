using Moq;
using Orders.Application.DTOs;
using Orders.Application.Queries.GetAllOrders;
using Orders.Application.Queries.GetOrderById;
using Orders.Domain.Entities;
using Orders.Domain.Repositories;

namespace Orders.Tests.Application;

public class QueryHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();

    private static Order CreateSampleOrder()
    {
        return Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create(Guid.NewGuid(), "Widget", 1, 10.00m)]);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderExists_ReturnsMappedResponse()
    {
        var order = CreateSampleOrder();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var handler = new GetOrderByIdQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetOrderByIdQuery(order.Id), CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(order.Id, result!.Id);
        Assert.Equal(order.CustomerId, result.CustomerId);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task GetOrderById_WhenOrderNotFound_ReturnsNull()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var handler = new GetOrderByIdQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetOrderByIdQuery(Guid.NewGuid()), CancellationToken.None);

        Assert.Null(result);
    }

    [Fact]
    public async Task GetAllOrders_WhenOrdersExist_ReturnsMappedList()
    {
        var orders = new List<Order> { CreateSampleOrder(), CreateSampleOrder() };
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(orders);

        var handler = new GetAllOrdersQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetAllOrdersQuery(), CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetAllOrders_WhenNoOrders_ReturnsEmptyList()
    {
        _repositoryMock
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order>());

        var handler = new GetAllOrdersQueryHandler(_repositoryMock.Object);

        var result = await handler.Handle(new GetAllOrdersQuery(), CancellationToken.None);

        Assert.Empty(result);
    }
}
