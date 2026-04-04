using FluentValidation;
using FluentValidation.Results;
using MediatR;
using Moq;
using Shared.Application.Behaviors;
using Orders.Application.Commands.PlaceOrder;
using Orders.Application.DTOs;

namespace Orders.Tests.Application;

public class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithNoValidators_CallsNextDelegate()
    {
        var validators = Enumerable.Empty<IValidator<PlaceOrderCommand>>();
        var behavior = new ValidationBehavior<PlaceOrderCommand, OrderResponse>(validators);
        var expectedResponse = new OrderResponse { Id = Guid.NewGuid() };

        var result = await behavior.Handle(
            new PlaceOrderCommand(),
            (ct) => Task.FromResult(expectedResponse),
            CancellationToken.None);

        Assert.Equal(expectedResponse.Id, result.Id);
    }

    [Fact]
    public async Task Handle_WithPassingValidation_CallsNextDelegate()
    {
        var validatorMock = new Mock<IValidator<PlaceOrderCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<PlaceOrderCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult());

        var behavior = new ValidationBehavior<PlaceOrderCommand, OrderResponse>([validatorMock.Object]);
        var expectedResponse = new OrderResponse { Id = Guid.NewGuid() };

        var result = await behavior.Handle(
            new PlaceOrderCommand(),
            (ct) => Task.FromResult(expectedResponse),
            CancellationToken.None);

        Assert.Equal(expectedResponse.Id, result.Id);
    }

    [Fact]
    public async Task Handle_WithFailingValidation_ThrowsValidationException()
    {
        var validatorMock = new Mock<IValidator<PlaceOrderCommand>>();
        validatorMock
            .Setup(v => v.ValidateAsync(It.IsAny<ValidationContext<PlaceOrderCommand>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ValidationResult(
            [
                new ValidationFailure("CustomerId", "Customer ID is required.")
            ]));

        var behavior = new ValidationBehavior<PlaceOrderCommand, OrderResponse>([validatorMock.Object]);

        await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(
                new PlaceOrderCommand(),
                (ct) => Task.FromResult(new OrderResponse()),
                CancellationToken.None));
    }
}
