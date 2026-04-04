using Orders.Application.Commands.PlaceOrder;
using Orders.Application.DTOs;

namespace Orders.Tests.Application;

public class PlaceOrderCommandValidatorTests
{
    private readonly PlaceOrderCommandValidator _validator = new();

    private static PlaceOrderCommand CreateValidCommand() => new()
    {
        CustomerId = Guid.NewGuid(),
        Items =
        [
            new OrderItemRequest { ProductId = Guid.NewGuid(), ProductName = "Widget", Quantity = 2, UnitPrice = 10.00m }
        ],
        CorrelationId = "test"
    };

    [Fact]
    public void Validate_WithValidCommand_IsValid()
    {
        var result = _validator.Validate(CreateValidCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithEmptyCustomerId_HasError()
    {
        var command = CreateValidCommand() with { CustomerId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "CustomerId");
    }

    [Fact]
    public void Validate_WithEmptyItemsList_HasError()
    {
        var command = CreateValidCommand() with { Items = [] };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_WithNullItemsList_HasError()
    {
        var command = CreateValidCommand() with { Items = null! };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithItemMissingProductId_HasError()
    {
        var command = CreateValidCommand() with
        {
            Items = [new OrderItemRequest { ProductId = Guid.Empty, ProductName = "A", Quantity = 1, UnitPrice = 1m }]
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Product ID"));
    }

    [Fact]
    public void Validate_WithItemEmptyProductName_HasError()
    {
        var command = CreateValidCommand() with
        {
            Items = [new OrderItemRequest { ProductId = Guid.NewGuid(), ProductName = "", Quantity = 1, UnitPrice = 1m }]
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Product name"));
    }

    [Fact]
    public void Validate_WithItemZeroQuantity_HasError()
    {
        var command = CreateValidCommand() with
        {
            Items = [new OrderItemRequest { ProductId = Guid.NewGuid(), ProductName = "A", Quantity = 0, UnitPrice = 1m }]
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Quantity"));
    }

    [Fact]
    public void Validate_WithItemZeroUnitPrice_HasError()
    {
        var command = CreateValidCommand() with
        {
            Items = [new OrderItemRequest { ProductId = Guid.NewGuid(), ProductName = "A", Quantity = 1, UnitPrice = 0m }]
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Unit price"));
    }
}
