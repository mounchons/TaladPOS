namespace TaladPOS.Domain.Promotions;

public enum PromotionScope
{
    Item,
    Bill,
}

/// <summary>Percentage discount promotion (FR-019-FR-022).</summary>
public class Promotion
{
    public Guid Id { get; private set; }

    public PromotionScope Scope { get; private set; }

    public decimal DiscountPercentage { get; private set; }

    public Guid? ProductId { get; private set; }

    public bool AppliesToMembersOnly { get; private set; }

    public DateOnly StartDate { get; private set; }

    public DateOnly EndDate { get; private set; }

    // EF Core materialization constructor.
    private Promotion()
    {
    }

    public Promotion(
        PromotionScope scope,
        decimal discountPercentage,
        Guid? productId,
        bool appliesToMembersOnly,
        DateOnly startDate,
        DateOnly endDate)
    {
        Validate(scope, discountPercentage, productId, startDate, endDate);

        Id = Guid.NewGuid();
        Scope = scope;
        DiscountPercentage = discountPercentage;
        ProductId = productId;
        AppliesToMembersOnly = appliesToMembersOnly;
        StartDate = startDate;
        EndDate = endDate;
    }

    public void Update(
        PromotionScope scope,
        decimal discountPercentage,
        Guid? productId,
        bool appliesToMembersOnly,
        DateOnly startDate,
        DateOnly endDate)
    {
        Validate(scope, discountPercentage, productId, startDate, endDate);

        Scope = scope;
        DiscountPercentage = discountPercentage;
        ProductId = productId;
        AppliesToMembersOnly = appliesToMembersOnly;
        StartDate = startDate;
        EndDate = endDate;
    }

    /// <summary>FR-021: active exactly when today falls within [StartDate, EndDate], inclusive.</summary>
    public bool IsActive(DateOnly date) => date >= StartDate && date <= EndDate;

    private static void Validate(
        PromotionScope scope, decimal discountPercentage, Guid? productId, DateOnly startDate, DateOnly endDate)
    {
        if (discountPercentage <= 0 || discountPercentage > 100)
        {
            throw new ArgumentOutOfRangeException(
                nameof(discountPercentage), discountPercentage, "DiscountPercentage must be in the range (0, 100].");
        }

        if (endDate < startDate)
        {
            throw new ArgumentException("EndDate must be greater than or equal to StartDate.", nameof(endDate));
        }

        if (scope == PromotionScope.Item && productId is null)
        {
            throw new ArgumentException("ProductId is required when Scope is Item.", nameof(productId));
        }

        if (scope == PromotionScope.Bill && productId is not null)
        {
            throw new ArgumentException("ProductId must be null when Scope is Bill.", nameof(productId));
        }
    }
}
