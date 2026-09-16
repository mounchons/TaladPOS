namespace TaladPOS.Domain.Promotions;

/// <summary>
/// One row of a <see cref="ConditionalPromotion"/>'s buy condition: this
/// product must be in the cart at least this many times for one set
/// (003/FR-003).
/// </summary>
public class ConditionLine
{
    internal void PlaceAt(int sortOrder) => SortOrder = sortOrder;

    /// <summary>
    /// A surrogate key, not a natural one. Editing a promotion replaces the
    /// whole condition list, so keying the rows on (promotion, product) would
    /// make EF Core see a retained product as one row deleted and another added
    /// with the same key, and refuse to track both. FR-003's "each product at
    /// most once" is enforced by a unique index instead.
    /// </summary>
    public Guid Id { get; private set; }

    public Guid ProductId { get; private set; }

    public int MinimumQuantity { get; private set; }

    /// <summary>
    /// The position the manager put this row in. Without it the rows come back
    /// in whatever order the database felt like, so "ซื้อ มะม่วง 1 + แตงโม 1"
    /// could render with its halves swapped - both confusing to read and, since
    /// the text is snapshotted onto the bill (003/FR-024), not reproducible.
    /// </summary>
    public int SortOrder { get; private set; }

    // EF Core materialization constructor.
    private ConditionLine()
    {
    }

    public ConditionLine(Guid productId, int minimumQuantity)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("ProductId is required.", nameof(productId));
        }

        if (minimumQuantity < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(minimumQuantity), minimumQuantity, "MinimumQuantity must be at least 1.");
        }

        Id = Guid.NewGuid();
        ProductId = productId;
        MinimumQuantity = minimumQuantity;
    }
}
