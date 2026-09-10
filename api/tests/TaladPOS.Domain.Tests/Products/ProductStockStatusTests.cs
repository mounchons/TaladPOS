using FluentAssertions;
using TaladPOS.Domain.Products;
using Xunit;

namespace TaladPOS.Domain.Tests.Products;

/// <summary>
/// tasks.md T039 - US3: IsLowStock is true only when 0 &lt; StockQuantity &lt;= LowStockThreshold;
/// StockQuantity == 0 is IsOutOfStock instead, not IsLowStock (SC-003).
/// </summary>
public class ProductStockStatusTests
{
    [Fact]
    public void IsLowStock_WhenStockAboveThreshold_IsFalse()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, null, stockQuantity: 10, lowStockThreshold: 5);

        product.IsLowStock.Should().BeFalse();
        product.IsOutOfStock.Should().BeFalse();
    }

    [Fact]
    public void IsLowStock_WhenStockEqualsThreshold_IsTrue()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, null, stockQuantity: 5, lowStockThreshold: 5);

        product.IsLowStock.Should().BeTrue();
        product.IsOutOfStock.Should().BeFalse();
    }

    [Fact]
    public void IsLowStock_WhenStockBelowThresholdButAboveZero_IsTrue()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, null, stockQuantity: 1, lowStockThreshold: 5);

        product.IsLowStock.Should().BeTrue();
        product.IsOutOfStock.Should().BeFalse();
    }

    [Fact]
    public void IsLowStock_WhenStockIsZero_IsFalseAndIsOutOfStockIsTrueInstead()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, null, stockQuantity: 0, lowStockThreshold: 5);

        product.IsLowStock.Should().BeFalse();
        product.IsOutOfStock.Should().BeTrue();
    }
}
