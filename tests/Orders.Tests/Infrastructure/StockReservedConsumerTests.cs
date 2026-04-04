using MassTransit;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Orders.Application.Commands.UpdateOrderStatus;
using Orders.Domain.Enums;
using Orders.Infrastructure.Messaging.Consumers;
using Shared.Contracts.Events;

namespace Orders.Tests.Infrastructure;

public class StockReservedConsumerTests
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ILogger<StockReservedConsumer>> _loggerMock = new();
    private readonly StockReservedConsumer _consumer;

    public StockReservedConsumerTests()
    {
        _consumer = new StockReservedConsumer(_mediatorMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Consume_SendsUpdateOrderStatusCommand_WithConfirmedStatus()
    {
        var orderId = Guid.NewGuid();
        var message = new StockReserved { OrderId = orderId, CorrelationId = "corr-1" };
        var contextMock = new Mock<ConsumeContext<StockReserved>>();
        contextMock.Setup(c => c.Message).Returns(message);
        contextMock.Setup(c => c.CancellationToken).Returns(CancellationToken.None);

        await _consumer.Consume(contextMock.Object);

        _mediatorMock.Verify(m => m.Send(
            It.Is<UpdateOrderStatusCommand>(cmd =>
                cmd.OrderId == orderId &&
                cmd.NewStatus == OrderStatus.Confirmed &&
                cmd.CorrelationId == "corr-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
