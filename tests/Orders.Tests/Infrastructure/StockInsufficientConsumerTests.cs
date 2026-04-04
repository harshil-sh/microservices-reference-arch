using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Orders.Application.Commands.UpdateOrderStatus;
using Orders.Domain.Enums;
using Orders.Infrastructure.Messaging.Consumers;
using Shared.Contracts.Events;

namespace Orders.Tests.Infrastructure;

public class StockInsufficientConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<StockInsufficientConsumer>> _loggerMock = new();
    private readonly StockInsufficientConsumer _consumer;

    public StockInsufficientConsumerTests()
    {
        _consumer = new StockInsufficientConsumer(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_SendsUpdateOrderStatusCommand_WithFailedStatus()
    {
        var orderId = Guid.NewGuid();
        var message = new StockInsufficient
        {
            OrderId = orderId,
            Reason = "Not enough stock",
            CorrelationId = "corr-2"
        };
        var contextMock = new Mock<ConsumeContext<StockInsufficient>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await _consumer.Consume(contextMock.Object);

        _mediatorMock.Verify(m => m.Send(
            It.Is<UpdateOrderStatusCommand>(cmd =>
                cmd.OrderId == orderId &&
                cmd.NewStatus == OrderStatus.Failed &&
                cmd.Reason == "Not enough stock" &&
                cmd.CorrelationId == "corr-2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
