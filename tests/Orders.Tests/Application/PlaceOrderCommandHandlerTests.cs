using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Orders.Application.Commands.PlaceOrder;
using Orders.Application.DTOs;
using Orders.Application.Interfaces;
using Orders.Application.Metrics;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Repositories;

namespace Orders.Tests.Application;

public class PlaceOrderCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IEventPublisher> _publisherMock = new();
    private readonly Mock<ILogger<PlaceOrderCommandHandler>> _loggerMock = new();
    private readonly OrdersMetrics _metrics;
    private readonly PlaceOrderCommandHandler _handler;

    public PlaceOrderCommandHandlerTests()
    {
        var meterFactory = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider()
            .GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>();
        _metrics = new OrdersMetrics(meterFactory);

        _handler = new PlaceOrderCommandHandler(
            _repositoryMock.Object,
            _publisherMock.Object,
            _loggerMock.Object,
            _metrics);
    }

    private static PlaceOrderCommand CreateValidCommand() => new()
    {
        CustomerId = Guid.NewGuid(),
        Items =
        [
            new OrderItemRequest { ProductId = Guid.NewGuid(), ProductName = "Widget", Quantity = 2, UnitPrice = 10.00m }
        ],
        CorrelationId = "test-correlation"
    };

    [Fact]
    public async Task Handle_CreatesOrderAndCallsRepositoryAdd()
    {
        var command = CreateValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(
            r => r.AddAsync(It.Is<Order>(o =>
                o.CustomerId == command.CustomerId &&
                o.Status == OrderStatus.Pending),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PublishesOrderPlacedEvent_WithCorrectData()
    {
        var command = CreateValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        _publisherMock.Verify(
            p => p.PublishOrderPlacedAsync(
                It.IsAny<Guid>(),
                command.CustomerId,
                It.IsAny<List<(Guid, string, int, decimal)>>(),
                It.Is<decimal>(d => d == 20.00m),
                It.IsAny<DateTime>(),
                "test-correlation",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_ReturnsOrderResponse_WithMappedFields()
    {
        var command = CreateValidCommand();

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.Equal(command.CustomerId, result.CustomerId);
        Assert.Equal(OrderStatus.Pending, result.Status);
        Assert.Equal(20.00m, result.TotalAmount);
        Assert.Single(result.Items);
    }

    [Fact]
    public async Task Handle_WithMultipleItems_CalculatesCorrectTotal()
    {
        var command = new PlaceOrderCommand
        {
            CustomerId = Guid.NewGuid(),
            Items =
            [
                new OrderItemRequest { ProductId = Guid.NewGuid(), ProductName = "A", Quantity = 2, UnitPrice = 10.00m },
                new OrderItemRequest { ProductId = Guid.NewGuid(), ProductName = "B", Quantity = 3, UnitPrice = 5.00m }
            ],
            CorrelationId = "test"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.Equal(35.00m, result.TotalAmount);
        Assert.Equal(2, result.Items.Count);
    }

    [Fact]
    public async Task Handle_PassesCorrelationId_ToEventPublisher()
    {
        var command = CreateValidCommand();

        await _handler.Handle(command, CancellationToken.None);

        _publisherMock.Verify(
            p => p.PublishOrderPlacedAsync(
                It.IsAny<Guid>(),
                It.IsAny<Guid>(),
                It.IsAny<List<(Guid, string, int, decimal)>>(),
                It.IsAny<decimal>(),
                It.IsAny<DateTime>(),
                command.CorrelationId,
                It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
