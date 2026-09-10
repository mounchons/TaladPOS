using FluentAssertions;
using TaladPOS.Domain.Products;
using Xunit;

namespace TaladPOS.Domain.Tests.Products;

/// <summary>tasks.md T038 - US3: Product rejects Price &lt;= 0 and negative StockQuantity.</summary>
public class ProductValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_WithNonPositivePrice_Throws(decimal price)
    {
        var act = () => new Product("มะม่วง", "https://example.com/mango.jpg", price, barcode: null, stockQuantity: 1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("price");
    }

    [Fact]
    public void Constructor_WithNegativeStockQuantity_Throws()
    {
        var act = () => new Product("มะม่วง", "https://example.com/mango.jpg", 45m, barcode: null, stockQuantity: -1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("stockQuantity");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void SetPrice_WithNonPositivePrice_Throws(decimal price)
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, barcode: null, stockQuantity: 1);

        var act = () => product.SetPrice(price);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("price");
        product.Price.Should().Be(45m, "a rejected update must not partially mutate the entity");
    }

    [Fact]
    public void SetStockQuantity_WithNegativeValue_Throws()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, barcode: null, stockQuantity: 5);

        var act = () => product.SetStockQuantity(-1);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("stockQuantity");
        product.StockQuantity.Should().Be(5);
    }
}
