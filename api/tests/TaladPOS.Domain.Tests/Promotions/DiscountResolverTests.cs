using FluentAssertions;
using TaladPOS.Domain.Promotions;
using Xunit;

namespace TaladPOS.Domain.Tests.Promotions;

/// <summary>
/// tasks.md T058 - US5: DiscountResolver always picks exactly one
/// highest-value candidate (never stacks), covering: no promotions,
/// promotion only, member discount only, both tied in value, and
/// promotions outside the active date range being excluded entirely.
/// </summary>
public class DiscountResolverTests
{
    private static readonly Guid ProductId = Guid.NewGuid();
    private static readonly DateOnly Today = new(2026, 9, 10);

    private static Promotion ItemPromotion(decimal percentage, bool membersOnly = false, DateOnly? start = null, DateOnly? end = null) =>
        new(PromotionScope.Item, percentage, ProductId, membersOnly, start ?? Today, end ?? Today);

    private static Promotion BillPromotion(decimal percentage, bool membersOnly = false, DateOnly? start = null, DateOnly? end = null) =>
        new(PromotionScope.Bill, percentage, null, membersOnly, start ?? Today, end ?? Today);

    [Fact]
    public void ResolveItemDiscount_WithNoPromotions_ReturnsZero()
    {
        var result = DiscountResolver.ResolveItemDiscount([], ProductId, hasMember: false, baseAmount: 100m, Today);

        result.Should().Be(0m);
    }

    [Fact]
    public void ResolveItemDiscount_WithOnlyGeneralPromotion_ReturnsItsAmount()
    {
        var promotions = new[] { ItemPromotion(10m) };

        var result = DiscountResolver.ResolveItemDiscount(promotions, ProductId, hasMember: false, baseAmount: 100m, Today);

        result.Should().Be(10m);
    }

    [Fact]
    public void ResolveItemDiscount_WithOnlyMemberDiscount_AppliesOnlyWhenSaleHasMember()
    {
        var promotions = new[] { ItemPromotion(15m, membersOnly: true) };

        DiscountResolver.ResolveItemDiscount(promotions, ProductId, hasMember: true, baseAmount: 100m, Today)
            .Should().Be(15m);
        DiscountResolver.ResolveItemDiscount(promotions, ProductId, hasMember: false, baseAmount: 100m, Today)
            .Should().Be(0m, "a members-only promotion must not apply to a sale with no member");
    }

    [Fact]
    public void ResolveItemDiscount_WithGeneralAndMemberPromotionsTiedInValue_ReturnsThatValueOnce()
    {
        var promotions = new[] { ItemPromotion(10m), ItemPromotion(10m, membersOnly: true) };

        var result = DiscountResolver.ResolveItemDiscount(promotions, ProductId, hasMember: true, baseAmount: 100m, Today);

        result.Should().Be(10m, "tied candidates must not be summed together");
    }

    [Fact]
    public void ResolveItemDiscount_WithBothApplicable_PicksTheLargerOne()
    {
        var promotions = new[] { ItemPromotion(10m), ItemPromotion(25m, membersOnly: true) };

        var result = DiscountResolver.ResolveItemDiscount(promotions, ProductId, hasMember: true, baseAmount: 100m, Today);

        result.Should().Be(25m);
    }

    [Fact]
    public void ResolveItemDiscount_WithPromotionOutsideDateRange_ExcludesIt()
    {
        var expired = ItemPromotion(50m, start: Today.AddDays(-10), end: Today.AddDays(-1));
        var active = ItemPromotion(10m);

        var result = DiscountResolver.ResolveItemDiscount([expired, active], ProductId, hasMember: false, baseAmount: 100m, Today);

        result.Should().Be(10m, "an expired promotion must never be a candidate, regardless of its percentage");
    }

    [Fact]
    public void ResolveItemDiscount_IgnoresPromotionsForOtherProducts()
    {
        var otherProduct = new Promotion(PromotionScope.Item, 90m, Guid.NewGuid(), false, Today, Today);

        var result = DiscountResolver.ResolveItemDiscount([otherProduct], ProductId, hasMember: false, baseAmount: 100m, Today);

        result.Should().Be(0m);
    }

    [Fact]
    public void ResolveBillDiscount_WithNoPromotions_ReturnsZero()
    {
        var result = DiscountResolver.ResolveBillDiscount([], hasMember: false, baseAmount: 200m, Today);

        result.Should().Be(0m);
    }

    [Fact]
    public void ResolveBillDiscount_PicksTheLargerOfGeneralAndMemberPromotions()
    {
        var promotions = new[] { BillPromotion(5m), BillPromotion(20m, membersOnly: true) };

        var result = DiscountResolver.ResolveBillDiscount(promotions, hasMember: true, baseAmount: 200m, Today);

        result.Should().Be(40m);
    }
}
