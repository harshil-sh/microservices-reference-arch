using Inventory.Application.Commands.ReserveStock;

namespace Inventory.Tests.Application;

public class ReserveStockCommandValidatorTests
{
    private readonly ReserveStockCommandValidator _validator = new();

    private static ReserveStockCommand CreateValidCommand() => new()
    {
        OrderId = Guid.NewGuid(),
        Items =
        [
            new ReserveStockItem { ProductId = Guid.NewGuid(), ProductName = "Widget", Quantity = 5 }
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
    public void Validate_WithEmptyOrderId_HasError()
    {
        var command = CreateValidCommand() with { OrderId = Guid.Empty };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "OrderId");
    }

    [Fact]
    public void Validate_WithEmptyItems_HasError()
    {
        var command = CreateValidCommand() with { Items = [] };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.PropertyName == "Items");
    }

    [Fact]
    public void Validate_WithItemMissingProductId_HasError()
    {
        var command = CreateValidCommand() with
        {
            Items = [new ReserveStockItem { ProductId = Guid.Empty, ProductName = "A", Quantity = 1 }]
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Product ID"));
    }

    [Fact]
    public void Validate_WithItemZeroQuantity_HasError()
    {
        var command = CreateValidCommand() with
        {
            Items = [new ReserveStockItem { ProductId = Guid.NewGuid(), ProductName = "A", Quantity = 0 }]
        };

        var result = _validator.Validate(command);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("Quantity"));
    }
}
