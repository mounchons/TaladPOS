namespace TaladPOS.Domain.Promotions;

/// <summary>
/// FR-022 (research.md #3): when multiple active promotions could apply to
/// the same target (a line item, or the whole bill), exactly one - whichever
/// yields the largest monetary discount - is used. Discounts are never
/// stacked/summed.
/// </summary>
public static class DiscountResolver
{
    /// <summary>Best discount amount among active Item-scope promotions for this specific product.</summary>
    public static decimal ResolveItemDiscount(
        IEnumerable<Promotion> promotions, Guid productId, bool hasMember, decimal baseAmount, DateOnly date)
    {
        var eligible = promotions.Where(p =>
            p.Scope == PromotionScope.Item
            && p.ProductId == productId
            && (!p.AppliesToMembersOnly || hasMember)
            && p.IsActive(date));

        return BestDiscount(eligible, baseAmount);
    }

    /// <summary>Best discount amount among active Bill-scope promotions.</summary>
    public static decimal ResolveBillDiscount(
        IEnumerable<Promotion> promotions, bool hasMember, decimal baseAmount, DateOnly date)
    {
        var eligible = promotions.Where(p =>
            p.Scope == PromotionScope.Bill
            && (!p.AppliesToMembersOnly || hasMember)
            && p.IsActive(date));

        return BestDiscount(eligible, baseAmount);
    }

    private static decimal BestDiscount(IEnumerable<Promotion> eligible, decimal baseAmount) =>
        eligible
            .Select(p => Math.Round(baseAmount * p.DiscountPercentage / 100m, 2))
            .DefaultIfEmpty(0m)
            .Max();
}
