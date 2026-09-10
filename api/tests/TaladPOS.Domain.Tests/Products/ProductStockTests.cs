using FluentAssertions;
using TaladPOS.Domain.Products;
using Xunit;

namespace TaladPOS.Domain.Tests.Products;

public class ProductStockTests
{
    [Fact]
    public void DecreaseStock_WhenQuantityExceedsAvailable_ThrowsInsufficientStockException()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, barcode: null, stockQuantity: 3);

        var act = () => product.DecreaseStock(5);

        act.Should().Throw<InsufficientStockException>();
        product.StockQuantity.Should().Be(3, "a failed decrease must not partially mutate stock");
    }

    [Fact]
    public void DecreaseStock_WhenQuantityIsAvailable_ReducesStockQuantity()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, barcode: null, stockQuantity: 10);

        product.DecreaseStock(4);

        product.StockQuantity.Should().Be(6);
    }

    [Fact]
    public void DecreaseStock_WhenQuantityEqualsAvailable_ReducesStockToZero()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, barcode: null, stockQuantity: 5);

        product.DecreaseStock(5);

        product.StockQuantity.Should().Be(0);
        product.IsOutOfStock.Should().BeTrue();
    }
}
