namespace TaladPOS.Domain.Sales;

/// <summary>
/// A conditional promotion that fired on this bill, recorded as a snapshot
/// (003/FR-024). The description is frozen at sale time and there is no
/// foreign key back to the promotion, because the promotion may be edited or
/// deleted later and the receipt still has to explain where the discount came
/// from - the same reason <see cref="SaleLineItem"/> snapshots the product
/// name and price.
///
/// This is provenance, not money: the discount itself already lives in the
/// line items, so <see cref="Sale"/>'s totals are unaffected by this table.
/// </summary>
public class SaleAppliedPromotion
{
    public Guid Id { get; private set; }

    public Guid SaleId { get; internal set; }

    public Guid PromotionId { get; private set; }

    public string DescriptionSnapshot { get; private set; }

    public int SetCount { get; private set; }

    public decimal DiscountAmount { get; private set; }

    // EF Core materialization constructor.
    private SaleAppliedPromotion()
    {
        DescriptionSnapshot = string.Empty;
    }

    public SaleAppliedPromotion(
        Guid promotionId, string descriptionSnapshot, int setCount, decimal discountAmount)
    {
        if (string.IsNullOrWhiteSpace(descriptionSnapshot))
        {
            throw new ArgumentException("DescriptionSnapshot is required.", nameof(descriptionSnapshot));
        }

        if (setCount < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(setCount), setCount, "SetCount must be at least 1.");
        }

        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount), discountAmount, "DiscountAmount cannot be negative.");
        }

        Id = Guid.NewGuid();
        PromotionId = promotionId;
        DescriptionSnapshot = descriptionSnapshot;
        SetCount = setCount;
        DiscountAmount = discountAmount;
    }
}
