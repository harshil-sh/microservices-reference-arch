using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Orders.Application.Commands.UpdateOrderStatus;
using Orders.Application.Interfaces;
using Orders.Application.Metrics;
using Orders.Domain.Entities;
using Orders.Domain.Enums;
using Orders.Domain.Repositories;

namespace Orders.Tests.Application;

public class UpdateOrderStatusCommandHandlerTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IEventPublisher> _publisherMock = new();
    private readonly Mock<ILogger<UpdateOrderStatusCommandHandler>> _loggerMock = new();
    private readonly OrdersMetrics _metrics;
    private readonly UpdateOrderStatusCommandHandler _handler;

    public UpdateOrderStatusCommandHandlerTests()
    {
        var meterFactory = new ServiceCollection()
            .AddMetrics()
            .BuildServiceProvider()
            .GetRequiredService<System.Diagnostics.Metrics.IMeterFactory>();
        _metrics = new OrdersMetrics(meterFactory);

        _handler = new UpdateOrderStatusCommandHandler(
            _repositoryMock.Object,
            _publisherMock.Object,
            _loggerMock.Object,
            _metrics);
    }

    private static Order CreatePendingOrder()
    {
        return Order.Create(
            Guid.NewGuid(),
            [OrderItem.Create(Guid.NewGuid(), "Widget", 1, 10.00m)]);
    }

    [Fact]
    public async Task Handle_WhenOrderNotFound_ReturnsFalse()
    {
        _repositoryMock
            .Setup(r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order?)null);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = Guid.NewGuid(),
            NewStatus = OrderStatus.Confirmed,
            CorrelationId = "test"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result);
        _publisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_ConfirmPendingOrder_ConfirmsAndPublishesOrderConfirmed()
    {
        var order = CreatePendingOrder();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            NewStatus = OrderStatus.Confirmed,
            CorrelationId = "test-corr"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(OrderStatus.Confirmed, order.Status);
        _repositoryMock.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishOrderConfirmedAsync(
            order.Id,
            It.IsAny<DateTime>(),
            "test-corr",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_FailPendingOrder_FailsAndPublishesOrderFailed()
    {
        var order = CreatePendingOrder();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            NewStatus = OrderStatus.Failed,
            Reason = "Out of stock",
            CorrelationId = "test-corr"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        Assert.Equal(OrderStatus.Failed, order.Status);
        _repositoryMock.Verify(r => r.UpdateAsync(order, It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(p => p.PublishOrderFailedAsync(
            order.Id,
            It.IsAny<DateTime>(),
            "Out of stock",
            "test-corr",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderAlreadyInTargetStatus_ReturnsTrue_SkipsProcessing()
    {
        var order = CreatePendingOrder();
        order.Confirm();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            NewStatus = OrderStatus.Confirmed,
            CorrelationId = "test"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.True(result);
        _repositoryMock.Verify(r => r.UpdateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()), Times.Never);
        _publisherMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_InvalidStatusTransition_ToPending_ReturnsFalse()
    {
        var order = CreatePendingOrder();
        order.Confirm(); // order is now Confirmed
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            NewStatus = OrderStatus.Pending, // Pending is not handled in switch
            CorrelationId = "test"
        };

        var result = await _handler.Handle(command, CancellationToken.None);

        Assert.False(result);
    }

    [Fact]
    public async Task Handle_FailWithNullReason_UsesDefaultUnknownReason()
    {
        var order = CreatePendingOrder();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            NewStatus = OrderStatus.Failed,
            Reason = null,
            CorrelationId = "test"
        };

        await _handler.Handle(command, CancellationToken.None);

        Assert.Equal("Unknown reason", order.FailureReason);
        _publisherMock.Verify(p => p.PublishOrderFailedAsync(
            order.Id,
            It.IsAny<DateTime>(),
            "Unknown reason",
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ConfirmOrder_CallsRepositoryUpdate()
    {
        var order = CreatePendingOrder();
        _repositoryMock
            .Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(order);

        var command = new UpdateOrderStatusCommand
        {
            OrderId = order.Id,
            NewStatus = OrderStatus.Confirmed,
            CorrelationId = "test"
        };

        await _handler.Handle(command, CancellationToken.None);

        _repositoryMock.Verify(
            r => r.UpdateAsync(It.Is<Order>(o => o.Status == OrderStatus.Confirmed), It.IsAny<CancellationToken>()),
            Times.Once);
    }
}
