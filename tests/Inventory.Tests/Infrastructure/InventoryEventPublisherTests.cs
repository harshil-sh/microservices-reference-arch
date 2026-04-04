using Inventory.Infrastructure.Messaging.Publishers;
using MassTransit;
using Moq;
using Shared.Contracts.Events;

namespace Inventory.Tests.Infrastructure;

public class InventoryEventPublisherTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly InventoryEventPublisher _publisher;

    public InventoryEventPublisherTests()
    {
        _publisher = new InventoryEventPublisher(_publishEndpointMock.Object);
    }

    [Fact]
    public async Task PublishStockReservedAsync_PublishesCorrectEvent_WithItems()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var items = new List<(Guid ProductId, int Quantity)> { (productId, 10) };

        await _publisher.PublishStockReservedAsync(orderId, DateTime.UtcNow, items, "corr-1");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<StockReserved>(e =>
                e.OrderId == orderId &&
                e.CorrelationId == "corr-1" &&
                e.Items.Count == 1 &&
                e.Items[0].ProductId == productId &&
                e.Items[0].Quantity == 10),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PublishStockInsufficientAsync_PublishesCorrectEvent_WithReason()
    {
        var orderId = Guid.NewGuid();

        await _publisher.PublishStockInsufficientAsync(orderId, DateTime.UtcNow, "Not enough", "corr-2");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<StockInsufficient>(e =>
                e.OrderId == orderId &&
                e.Reason == "Not enough" &&
                e.CorrelationId == "corr-2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
