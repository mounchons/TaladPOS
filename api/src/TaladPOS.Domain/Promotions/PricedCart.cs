namespace TaladPOS.Domain.Promotions;

/// <summary>One product and how many of it the cashier put in the cart.</summary>
public sealed record CartLine(Guid ProductId, int Quantity);

/// <summary>Name and price of a product the pricer may need to reason about.</summary>
public sealed record PricedProduct(Guid Id, string Name, decimal Price);

/// <summary>
/// One line of the priced result. A product can produce two of these - the
/// paid line and the gift line - which is why <see cref="IsGift"/> exists
/// rather than being inferred from the amounts (003/FR-016, research.md #4).
/// </summary>
public sealed record PricedLine(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    int Quantity,
    decimal DiscountAmount,
    bool IsGift)
{
    public decimal Subtotal => UnitPrice * Quantity;

    public decimal LineTotal => Subtotal - DiscountAmount;
}

/// <summary>A conditional promotion that fired, and what it was worth (003/FR-024).</summary>
public sealed record AppliedPromotionResult(
    Guid PromotionId,
    string Description,
    int SetCount,
    decimal DiscountAmount);

/// <summary>
/// The cart qualified for a gift the cashier has not put in the cart yet
/// (003/FR-015). Display only - it never moves any money.
/// </summary>
public sealed record UnclaimedGift(
    Guid PromotionId,
    string Description,
    Guid GiftProductId,
    string GiftProductName,
    int MissingQuantity);

/// <summary>
/// What <see cref="CartPricer"/> returns. Checkout turns this into a Sale;
/// the preview endpoint hands it straight to the register, which is how the
/// two can never disagree about the total (003/FR-012, research.md #2).
/// </summary>
public sealed record PricedCart(
    IReadOnlyList<PricedLine> Lines,
    IReadOnlyList<AppliedPromotionResult> AppliedPromotions,
    IReadOnlyList<UnclaimedGift> UnclaimedGifts)
{
    public decimal SubtotalAmount => Lines.Sum(line => line.Subtotal);

    public decimal DiscountAmount => Lines.Sum(line => line.DiscountAmount);

    public decimal TotalAmount => SubtotalAmount - DiscountAmount;
}
