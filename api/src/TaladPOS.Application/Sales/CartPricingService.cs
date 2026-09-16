using TaladPOS.Application.Members;
using TaladPOS.Application.Products;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Promotions;

namespace TaladPOS.Application.Sales;

/// <summary>
/// Gathers everything <see cref="CartPricer"/> needs and calls it. Checkout and
/// the preview endpoint both go through here, which is what makes the total the
/// cashier sees identical to the total that gets charged (003/FR-012, SC-006).
///
/// The loading rule is the interesting part: products are fetched for the cart
/// *and* for every product the active conditional promotions mention. Without
/// the second half, a "buy A+B get C" promotion could not be ordered by value
/// or produce a "you have not taken C yet" hint before C is scanned, which is
/// the main flow of User Story 1 (003/research.md #7).
/// </summary>
public sealed class CartPricingService
{
    private readonly IProductRepository _products;
    private readonly IPromotionRepository _percentagePromotions;
    private readonly IConditionalPromotionRepository _conditionalPromotions;

    public CartPricingService(
        IProductRepository products,
        IPromotionRepository percentagePromotions,
        IConditionalPromotionRepository conditionalPromotions)
    {
        _products = products;
        _percentagePromotions = percentagePromotions;
        _conditionalPromotions = conditionalPromotions;
    }

    public sealed record Result(PricedCart Cart, IReadOnlyDictionary<Guid, Domain.Products.Product> Products);

    public async Task<Result> PriceAsync(
        IReadOnlyList<CartLine> cartLines, bool hasMember, DateOnly date, CancellationToken ct = default)
    {
        var percentage = await _percentagePromotions.GetActiveOnAsync(date, ct);
        var conditional = await _conditionalPromotions.GetActiveOnAsync(date, ct);

        var wantedIds = cartLines.Select(line => line.ProductId)
            .Concat(conditional.SelectMany(promotion => promotion.ReferencedProductIds))
            .Distinct()
            .ToList();

        var loaded = await _products.GetByIdsAsync(wantedIds, ct);
        var byId = loaded.ToDictionary(product => product.Id);

        // A product in the cart that does not exist is the caller's problem, not
        // the pricer's - surface it the same way checkout always has.
        foreach (var line in cartLines)
        {
            if (!byId.ContainsKey(line.ProductId))
            {
                throw new KeyNotFoundException($"Product {line.ProductId} was not found.");
            }
        }

        var catalogue = byId.ToDictionary(
            pair => pair.Key,
            pair => new PricedProduct(pair.Key, pair.Value.Name, pair.Value.Price));

        var priced = CartPricer.Price(cartLines, catalogue, percentage, conditional, hasMember, date);
        return new Result(priced, byId);
    }
}

public sealed record PreviewSaleRequest(Guid? MemberId, IReadOnlyList<CompleteSaleLineItemRequest> LineItems);

/// <summary>
/// contracts/sales-preview.md - POST /api/v1/sales/preview. Prices the cart and
/// nothing else: no transaction, no stock decrement, no rows written.
/// </summary>
public sealed class PreviewSaleUseCase
{
    private readonly CartPricingService _pricing;
    private readonly IMemberRepository _members;

    public PreviewSaleUseCase(CartPricingService pricing, IMemberRepository members)
    {
        _pricing = pricing;
        _members = members;
    }

    public async Task<PricedCart> ExecuteAsync(PreviewSaleRequest request, CancellationToken ct = default)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            throw new ArgumentException("A cart must contain at least one line item.", nameof(request));
        }

        if (request.LineItems.Any(line => line.Quantity <= 0))
        {
            throw new ArgumentException("Quantity must be greater than 0.", nameof(request));
        }

        if (request.MemberId is Guid memberId && await _members.GetByIdAsync(memberId, ct) is null)
        {
            throw new KeyNotFoundException($"Member {memberId} was not found.");
        }

        var cartLines = request.LineItems
            .Select(line => new CartLine(line.ProductId, line.Quantity))
            .ToList();

        var result = await _pricing.PriceAsync(
            cartLines, request.MemberId is not null, DateOnly.FromDateTime(DateTime.UtcNow), ct);

        return result.Cart;
    }
}
