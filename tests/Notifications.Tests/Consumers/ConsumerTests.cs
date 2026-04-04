using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Notifications.Worker.Consumers;
using Notifications.Worker.Services;
using Shared.Contracts.Events;

namespace Notifications.Tests.Consumers;

public class OrderPlacedConsumerTests
{
    [Fact]
    public async Task Consume_CallsSendOrderReceived_WithCorrectData()
    {
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<OrderPlacedConsumer>>();
        var consumer = new OrderPlacedConsumer(notificationMock.Object, loggerMock.Object);

        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var message = new OrderPlaced
        {
            OrderId = orderId,
            CustomerId = customerId,
            TotalAmount = 99.99m,
            CorrelationId = "corr-1"
        };
        var contextMock = new Mock<ConsumeContext<OrderPlaced>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        notificationMock.Verify(n => n.SendOrderReceivedAsync(
            orderId, customerId, 99.99m, "corr-1", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class StockReservedConsumerTests
{
    [Fact]
    public async Task Consume_CallsSendStockReserved_WithCorrectOrderId()
    {
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<StockReservedConsumer>>();
        var consumer = new StockReservedConsumer(notificationMock.Object, loggerMock.Object);

        var orderId = Guid.NewGuid();
        var message = new StockReserved { OrderId = orderId, CorrelationId = "corr-2" };
        var contextMock = new Mock<ConsumeContext<StockReserved>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        notificationMock.Verify(n => n.SendStockReservedAsync(
            orderId, "corr-2", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class StockInsufficientConsumerTests
{
    [Fact]
    public async Task Consume_CallsSendOrderFailed_WithReason()
    {
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<StockInsufficientConsumer>>();
        var consumer = new StockInsufficientConsumer(notificationMock.Object, loggerMock.Object);

        var orderId = Guid.NewGuid();
        var message = new StockInsufficient
        {
            OrderId = orderId,
            Reason = "Not enough stock",
            CorrelationId = "corr-3"
        };
        var contextMock = new Mock<ConsumeContext<StockInsufficient>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        notificationMock.Verify(n => n.SendOrderFailedAsync(
            orderId, "Not enough stock", "corr-3", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class OrderConfirmedConsumerTests
{
    [Fact]
    public async Task Consume_CallsSendOrderConfirmed_WithCorrectOrderId()
    {
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<OrderConfirmedConsumer>>();
        var consumer = new OrderConfirmedConsumer(notificationMock.Object, loggerMock.Object);

        var orderId = Guid.NewGuid();
        var message = new OrderConfirmed { OrderId = orderId, CorrelationId = "corr-4" };
        var contextMock = new Mock<ConsumeContext<OrderConfirmed>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        notificationMock.Verify(n => n.SendOrderConfirmedAsync(
            orderId, "corr-4", It.IsAny<CancellationToken>()), Times.Once);
    }
}

public class OrderFailedConsumerTests
{
    [Fact]
    public async Task Consume_CallsSendOrderFailed_WithReason()
    {
        var notificationMock = new Mock<INotificationService>();
        var loggerMock = new Mock<ILogger<OrderFailedConsumer>>();
        var consumer = new OrderFailedConsumer(notificationMock.Object, loggerMock.Object);

        var orderId = Guid.NewGuid();
        var message = new OrderFailed
        {
            OrderId = orderId,
            Reason = "Stock unavailable",
            CorrelationId = "corr-5"
        };
        var contextMock = new Mock<ConsumeContext<OrderFailed>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await consumer.Consume(contextMock.Object);

        notificationMock.Verify(n => n.SendOrderFailedAsync(
            orderId, "Stock unavailable", "corr-5", It.IsAny<CancellationToken>()), Times.Once);
    }
}
