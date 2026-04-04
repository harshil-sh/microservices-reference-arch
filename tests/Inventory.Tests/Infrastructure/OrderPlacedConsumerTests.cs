using Inventory.Application.Commands.ReserveStock;
using Inventory.Infrastructure.Messaging.Consumers;
using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Shared.Contracts.Events;

namespace Inventory.Tests.Infrastructure;

public class OrderPlacedConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<OrderPlacedConsumer>> _loggerMock = new();
    private readonly OrderPlacedConsumer _consumer;

    public OrderPlacedConsumerTests()
    {
        _consumer = new OrderPlacedConsumer(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_SendsReserveStockCommand_WithCorrectlyMappedItems()
    {
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var message = new OrderPlaced
        {
            OrderId = orderId,
            CustomerId = Guid.NewGuid(),
            Items =
            [
                new OrderPlacedItem { ProductId = productId, ProductName = "Widget", Quantity = 3, UnitPrice = 10m }
            ],
            TotalAmount = 30m,
            PlacedAt = DateTime.UtcNow,
            CorrelationId = "corr-1"
        };
        var contextMock = new Mock<ConsumeContext<OrderPlaced>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await _consumer.Consume(contextMock.Object);

        _mediatorMock.Verify(m => m.Send(
            It.Is<ReserveStockCommand>(cmd =>
                cmd.OrderId == orderId &&
                cmd.CorrelationId == "corr-1" &&
                cmd.Items.Count == 1 &&
                cmd.Items[0].ProductId == productId &&
                cmd.Items[0].ProductName == "Widget" &&
                cmd.Items[0].Quantity == 3),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
