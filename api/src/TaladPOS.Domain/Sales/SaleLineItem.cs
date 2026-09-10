namespace TaladPOS.Domain.Sales;

/// <summary>
/// One product line within a <see cref="Sale"/>. Stores a snapshot of the
/// product's name and price at the time of sale (data-model.md) so a bill's
/// history stays accurate even if the product is later edited or deleted.
/// </summary>
public class SaleLineItem
{
    public Guid Id { get; private set; }

    public Guid SaleId { get; internal set; }

    public Guid ProductId { get; private set; }

    public string ProductNameSnapshot { get; private set; }

    public decimal UnitPriceSnapshot { get; private set; }

    public int Quantity { get; private set; }

    public decimal DiscountAmount { get; private set; }

    public decimal LineTotal => (UnitPriceSnapshot * Quantity) - DiscountAmount;

    // EF Core materialization constructor.
    private SaleLineItem()
    {
        ProductNameSnapshot = string.Empty;
    }

    public SaleLineItem(
        Guid productId,
        string productNameSnapshot,
        decimal unitPriceSnapshot,
        int quantity,
        decimal discountAmount = 0m)
    {
        if (string.IsNullOrWhiteSpace(productNameSnapshot))
        {
            throw new ArgumentException("ProductNameSnapshot is required.", nameof(productNameSnapshot));
        }

        if (unitPriceSnapshot <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPriceSnapshot), unitPriceSnapshot, "UnitPriceSnapshot must be greater than 0.");
        }

        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than 0.");
        }

        if (discountAmount < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountAmount), discountAmount, "DiscountAmount cannot be negative.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        ProductNameSnapshot = productNameSnapshot;
        UnitPriceSnapshot = unitPriceSnapshot;
        Quantity = quantity;
        DiscountAmount = discountAmount;
    }
}
