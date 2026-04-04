using MassTransit;
using Moq;
using Orders.Infrastructure.Messaging.Publishers;
using Shared.Contracts.Events;

namespace Orders.Tests.Infrastructure;

public class OrderEventPublisherTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly OrderEventPublisher _publisher;

    public OrderEventPublisherTests()
    {
        _publisher = new OrderEventPublisher(_publishEndpointMock.Object);
    }

    [Fact]
    public async Task PublishOrderPlacedAsync_PublishesCorrectEvent_WithMappedItems()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var items = new List<(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice)>
        {
            (productId, "Widget", 2, 10.00m)
        };

        await _publisher.PublishOrderPlacedAsync(orderId, customerId, items, 20.00m, DateTime.UtcNow, "corr-1");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<OrderPlaced>(e =>
                e.OrderId == orderId &&
                e.CustomerId == customerId &&
                e.TotalAmount == 20.00m &&
                e.CorrelationId == "corr-1" &&
                e.Items.Count == 1 &&
                e.Items[0].ProductId == productId &&
                e.Items[0].Quantity == 2),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishOrderConfirmedAsync_PublishesCorrectEvent()
    {
        var orderId = Guid.NewGuid();
        var confirmedAt = DateTime.UtcNow;

        await _publisher.PublishOrderConfirmedAsync(orderId, confirmedAt, "corr-2");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<OrderConfirmed>(e =>
                e.OrderId == orderId &&
                e.ConfirmedAt == confirmedAt &&
                e.CorrelationId == "corr-2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishOrderFailedAsync_PublishesCorrectEvent_WithReason()
    {
        var orderId = Guid.NewGuid();
        var failedAt = DateTime.UtcNow;

        await _publisher.PublishOrderFailedAsync(orderId, failedAt, "Out of stock", "corr-3");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<OrderFailed>(e =>
                e.OrderId == orderId &&
                e.FailedAt == failedAt &&
                e.Reason == "Out of stock" &&
                e.CorrelationId == "corr-3"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
