using MassTransit;
using Microsoft.Extensions.Logging;
using Moq;
using Notifications.Worker.Services;
using Shared.Contracts.Events;

namespace Notifications.Tests.Services;

public class NotificationServiceTests
{
    private readonly Mock<IPublishEndpoint> _publishEndpointMock = new();
    private readonly Mock<ILogger<NotificationService>> _loggerMock = new();
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(_publishEndpointMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task SendOrderReceivedAsync_PublishesNotificationSent_WithEmailChannel()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await _service.SendOrderReceivedAsync(orderId, customerId, 100m, "corr-1");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<NotificationSent>(e =>
                e.OrderId == orderId &&
                e.Channel == "Email" &&
                e.RecipientId == customerId &&
                e.CorrelationId == "corr-1"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendStockReservedAsync_PublishesNotificationSent()
    {
        var orderId = Guid.NewGuid();

        await _service.SendStockReservedAsync(orderId, "corr-2");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<NotificationSent>(e =>
                e.OrderId == orderId &&
                e.Channel == "Email" &&
                e.CorrelationId == "corr-2"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendOrderConfirmedAsync_PublishesNotificationSent()
    {
        var orderId = Guid.NewGuid();

        await _service.SendOrderConfirmedAsync(orderId, "corr-3");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<NotificationSent>(e =>
                e.OrderId == orderId &&
                e.Channel == "Email" &&
                e.CorrelationId == "corr-3"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendOrderFailedAsync_PublishesNotificationSent()
    {
        var orderId = Guid.NewGuid();

        await _service.SendOrderFailedAsync(orderId, "Out of stock", "corr-4");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<NotificationSent>(e =>
                e.OrderId == orderId &&
                e.Channel == "Email" &&
                e.CorrelationId == "corr-4"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SendOrderReceivedAsync_SetsCorrectOrderIdAndRecipient()
    {
        var orderId = Guid.NewGuid();
        var customerId = Guid.NewGuid();

        await _service.SendOrderReceivedAsync(orderId, customerId, 50m, "corr-5");

        _publishEndpointMock.Verify(p => p.Publish(
            It.Is<NotificationSent>(e =>
                e.OrderId == orderId &&
                e.RecipientId == customerId),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
