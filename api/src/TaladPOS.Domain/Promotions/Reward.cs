namespace TaladPOS.Domain.Promotions;

public enum RewardKind
{
    Gift,
    Percentage,
}

/// <summary>
/// What a customer gets for one completed set of a
/// <see cref="ConditionalPromotion"/>'s buy condition (003/FR-001). Exactly one
/// of the two shapes is populated; the other side's fields must be null, the
/// same shape of rule <see cref="Promotion"/> already uses for Scope/ProductId.
/// </summary>
public class Reward
{
    public RewardKind Kind { get; private set; }

    public Guid? GiftProductId { get; private set; }

    public int? GiftQuantity { get; private set; }

    public decimal? DiscountPercentage { get; private set; }

    // EF Core materialization constructor.
    private Reward()
    {
    }

    private Reward(RewardKind kind, Guid? giftProductId, int? giftQuantity, decimal? discountPercentage)
    {
        Kind = kind;
        GiftProductId = giftProductId;
        GiftQuantity = giftQuantity;
        DiscountPercentage = discountPercentage;
    }

    /// <summary>003/FR-004: a gift reward names one product and how many of it.</summary>
    public static Reward Gift(Guid giftProductId, int giftQuantity)
    {
        if (giftProductId == Guid.Empty)
        {
            throw new ArgumentException("GiftProductId is required for a Gift reward.", nameof(giftProductId));
        }

        if (giftQuantity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(giftQuantity), giftQuantity, "GiftQuantity must be at least 1.");
        }

        return new Reward(RewardKind.Gift, giftProductId, giftQuantity, null);
    }

    /// <summary>003/FR-005: a percentage reward is in the range (0, 100].</summary>
    public static Reward Percentage(decimal discountPercentage)
    {
        if (discountPercentage <= 0 || discountPercentage > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercentage), discountPercentage,
                "DiscountPercentage must be in the range (0, 100].");
        }

        return new Reward(RewardKind.Percentage, null, null, discountPercentage);
    }

    /// <summary>
    /// The monetary value of one set, used to order competing promotions
    /// deterministically (003/FR-019, research.md #9).
    /// </summary>
    public decimal ValuePerSet(
        IReadOnlyDictionary<Guid, decimal> prices, IReadOnlyList<ConditionLine> conditionLines)
    {
        if (Kind == RewardKind.Gift)
        {
            return prices.TryGetValue(GiftProductId!.Value, out var giftPrice)
                ? giftPrice * GiftQuantity!.Value
                : 0m;
        }

        var conditionValue = conditionLines.Sum(line =>
            prices.TryGetValue(line.ProductId, out var price) ? price * line.MinimumQuantity : 0m);

        return Math.Round(conditionValue * DiscountPercentage!.Value / 100m, 2);
    }
}
