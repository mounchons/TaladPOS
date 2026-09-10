using FluentAssertions;
using TaladPOS.Domain.Promotions;
using Xunit;

namespace TaladPOS.Domain.Tests.Promotions;

/// <summary>tasks.md T057 - US5: Promotion.IsActive(date) is true only within [StartDate, EndDate], inclusive.</summary>
public class PromotionActiveTests
{
    private static Promotion NewPromotion(DateOnly start, DateOnly end) =>
        new(PromotionScope.Bill, 10m, null, false, start, end);

    [Fact]
    public void IsActive_OnStartDate_IsTrue()
    {
        var promotion = NewPromotion(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        promotion.IsActive(new DateOnly(2026, 9, 1)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_OnEndDate_IsTrue()
    {
        var promotion = NewPromotion(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        promotion.IsActive(new DateOnly(2026, 9, 30)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_BetweenStartAndEnd_IsTrue()
    {
        var promotion = NewPromotion(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        promotion.IsActive(new DateOnly(2026, 9, 15)).Should().BeTrue();
    }

    [Fact]
    public void IsActive_BeforeStartDate_IsFalse()
    {
        var promotion = NewPromotion(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        promotion.IsActive(new DateOnly(2026, 8, 31)).Should().BeFalse();
    }

    [Fact]
    public void IsActive_AfterEndDate_IsFalse()
    {
        var promotion = NewPromotion(new DateOnly(2026, 9, 1), new DateOnly(2026, 9, 30));

        promotion.IsActive(new DateOnly(2026, 10, 1)).Should().BeFalse();
    }

    [Fact]
    public void IsActive_ForSingleDayPromotion_IsTrueOnlyOnThatDate()
    {
        var promotion = NewPromotion(new DateOnly(2026, 9, 10), new DateOnly(2026, 9, 10));

        promotion.IsActive(new DateOnly(2026, 9, 10)).Should().BeTrue();
        promotion.IsActive(new DateOnly(2026, 9, 9)).Should().BeFalse();
        promotion.IsActive(new DateOnly(2026, 9, 11)).Should().BeFalse();
    }
}
