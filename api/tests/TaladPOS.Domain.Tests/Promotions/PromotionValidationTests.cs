using FluentAssertions;
using TaladPOS.Domain.Promotions;
using Xunit;

namespace TaladPOS.Domain.Tests.Promotions;

/// <summary>
/// tasks.md T056 - US5: Promotion rejects EndDate &lt; StartDate,
/// DiscountPercentage outside (0, 100], and a Scope/ProductId mismatch.
/// </summary>
public class PromotionValidationTests
{
    private static readonly DateOnly Today = new(2026, 9, 10);

    [Fact]
    public void Constructor_WithEndDateBeforeStartDate_Throws()
    {
        var act = () => new Promotion(
            PromotionScope.Bill, 10m, null, false, Today, Today.AddDays(-1));

        act.Should().Throw<ArgumentException>().WithParameterName("endDate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.01)]
    [InlineData(150)]
    public void Constructor_WithDiscountPercentageOutOfRange_Throws(decimal percentage)
    {
        var act = () => new Promotion(
            PromotionScope.Bill, percentage, null, false, Today, Today);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("discountPercentage");
    }

    [Fact]
    public void Constructor_WithItemScopeAndNoProductId_Throws()
    {
        var act = () => new Promotion(
            PromotionScope.Item, 10m, null, false, Today, Today);

        act.Should().Throw<ArgumentException>().WithParameterName("productId");
    }

    [Fact]
    public void Constructor_WithBillScopeAndProductId_Throws()
    {
        var act = () => new Promotion(
            PromotionScope.Bill, 10m, Guid.NewGuid(), false, Today, Today);

        act.Should().Throw<ArgumentException>().WithParameterName("productId");
    }

    [Fact]
    public void Constructor_WithValidItemScope_Succeeds()
    {
        var promotion = new Promotion(PromotionScope.Item, 10m, Guid.NewGuid(), false, Today, Today);

        promotion.DiscountPercentage.Should().Be(10m);
    }

    [Fact]
    public void Constructor_WithDiscountPercentageAt100_Succeeds()
    {
        var promotion = new Promotion(PromotionScope.Bill, 100m, null, false, Today, Today);

        promotion.DiscountPercentage.Should().Be(100m);
    }
}
