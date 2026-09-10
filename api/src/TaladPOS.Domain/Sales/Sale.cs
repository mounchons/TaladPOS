namespace TaladPOS.Domain.Sales;

/// <summary>
/// A completed, paid sale (FR-023-FR-024). Only ever created after payment
/// succeeds - the pre-payment cart is transient client-side state, never
/// persisted. Append-only: this version has no void/refund/edit path
/// (clarify session #2), so there is deliberately no mutation API here
/// beyond construction.
/// </summary>
public class Sale
{
    private readonly List<SaleLineItem> _lineItems = new();

    public Guid Id { get; private set; }

    public Guid StaffId { get; private set; }

    public Guid? MemberId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<SaleLineItem> LineItems => _lineItems.AsReadOnly();

    public decimal SubtotalAmount => _lineItems.Sum(item => item.UnitPriceSnapshot * item.Quantity);

    public decimal DiscountAmount => _lineItems.Sum(item => item.DiscountAmount);

    public decimal TotalAmount => SubtotalAmount - DiscountAmount;

    // EF Core materialization constructor.
    private Sale()
    {
    }

    public Sale(Guid staffId, Guid? memberId, IEnumerable<SaleLineItem> lineItems)
    {
        var items = lineItems?.ToList() ?? new List<SaleLineItem>();
        if (items.Count == 0)
        {
            throw new ArgumentException("A sale must contain at least one line item.", nameof(lineItems));
        }

        Id = Guid.NewGuid();
        StaffId = staffId;
        MemberId = memberId;
        CreatedAtUtc = DateTime.UtcNow;

        foreach (var item in items)
        {
            item.SaleId = Id;
            _lineItems.Add(item);
        }
    }
}
