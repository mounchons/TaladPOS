using TaladPOS.Application.Products;
using TaladPOS.Domain.Promotions;

namespace TaladPOS.Application.Promotions;

public sealed record ConditionLineView(Guid ProductId, string? ProductName, int MinimumQuantity);

public sealed record RewardView(
    RewardKind Kind,
    Guid? GiftProductId,
    string? GiftProductName,
    int? GiftQuantity,
    decimal? DiscountPercentage);

public sealed record ConditionalPromotionView(
    Guid Id,
    string Name,
    IReadOnlyList<ConditionLineView> ConditionLines,
    RewardView Reward,
    bool AppliesToMembersOnly,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsActive,
    string Description,
    bool IsUsable,
    string? UnusableReason);

/// <summary>
/// contracts/conditional-promotions.md - GET. Resolves product names once for
/// the whole page, composes the one-line description server-side so the web app
/// never has to (003/FR-008), and flags promotions that can no longer fire
/// because a product they point at was deleted (003/FR-023).
/// </summary>
public sealed class ListConditionalPromotionsQuery
{
    public const string DeletedProductReason = "สินค้าที่อ้างถึงถูกลบออกจากระบบแล้ว";

    private readonly IConditionalPromotionRepository _promotions;
    private readonly IProductRepository _products;

    public ListConditionalPromotionsQuery(
        IConditionalPromotionRepository promotions, IProductRepository products)
    {
        _promotions = promotions;
        _products = products;
    }

    public async Task<IReadOnlyList<ConditionalPromotionView>> ExecuteAsync(
        bool activeOnly, DateOnly today, CancellationToken ct = default)
    {
        var promotions = await _promotions.ListAsync(activeOnly, today, ct);
        if (promotions.Count == 0)
        {
            return Array.Empty<ConditionalPromotionView>();
        }

        var referenced = promotions.SelectMany(p => p.ReferencedProductIds).Distinct();
        var products = await _products.GetByIdsAsync(referenced, ct);
        var names = products.ToDictionary(product => product.Id, product => product.Name);

        return promotions.Select(promotion => ToView(promotion, names, today)).ToList();
    }

    public static ConditionalPromotionView ToView(
        ConditionalPromotion promotion, IReadOnlyDictionary<Guid, string> names, DateOnly today)
    {
        var isUsable = promotion.ReferencedProductIds.All(names.ContainsKey);

        var reward = promotion.Reward;
        var rewardView = new RewardView(
            reward.Kind,
            reward.GiftProductId,
            reward.GiftProductId is Guid giftId && names.TryGetValue(giftId, out var giftName)
                ? giftName
                : null,
            reward.GiftQuantity,
            reward.DiscountPercentage);

        return new ConditionalPromotionView(
            promotion.Id,
            promotion.Name,
            promotion.ConditionLines
                .Select(line => new ConditionLineView(
                    line.ProductId,
                    names.TryGetValue(line.ProductId, out var name) ? name : null,
                    line.MinimumQuantity))
                .ToList(),
            rewardView,
            promotion.AppliesToMembersOnly,
            promotion.StartDate,
            promotion.EndDate,
            promotion.IsActive(today),
            promotion.Describe(names),
            isUsable,
            isUsable ? null : DeletedProductReason);
    }
}
