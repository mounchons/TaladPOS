using TaladPOS.Application.Common;
using TaladPOS.Application.Members;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Promotions;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Application.Sales;

public record CompleteSaleLineItemRequest(Guid ProductId, int Quantity);

public record CompleteSaleRequest(
    Guid StaffId, Guid? MemberId, IReadOnlyList<CompleteSaleLineItemRequest> LineItems);

/// <summary>
/// Checkout (contracts/sales.md - POST /api/v1/sales). Runs stock decrements,
/// the Sale insert and the member accumulation update (US4, FR-014) as one
/// database transaction so none of them can land without the others (FR-016).
///
/// Pricing moved out to <see cref="CartPricingService"/> for 003: the preview
/// endpoint has to produce the same numbers, and the only way to guarantee that
/// is for both to run the same code (003/FR-012).
///
/// That also forced the order to change. Stock used to be decremented line by
/// line while walking the request; it now happens after pricing, because how
/// many units are free - and therefore how many come off the shelf for nothing -
/// is a pricing outcome, not something the request states (003/research.md #8).
/// </summary>
public class CompleteSaleUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly CartPricingService _pricing;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSaleUseCase(
        IProductRepository productRepository,
        ISaleRepository saleRepository,
        IMemberRepository memberRepository,
        CartPricingService pricing,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _memberRepository = memberRepository;
        _pricing = pricing;
        _unitOfWork = unitOfWork;
    }

    public async Task<Sale> ExecuteAsync(CompleteSaleRequest request, CancellationToken ct = default)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            throw new ArgumentException("A sale must contain at least one line item.", nameof(request));
        }

        foreach (var line in request.LineItems)
        {
            if (line.Quantity <= 0)
            {
                throw new ArgumentException(
                    $"Quantity for product {line.ProductId} must be greater than 0.", nameof(request));
            }
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        if (request.MemberId is Guid requestedMemberId
            && await _memberRepository.GetByIdAsync(requestedMemberId, ct) is null)
        {
            throw new KeyNotFoundException($"Member {requestedMemberId} was not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var cartLines = request.LineItems
            .Select(line => new CartLine(line.ProductId, line.Quantity))
            .ToList();

        var priced = await _pricing.PriceAsync(cartLines, request.MemberId is not null, today, ct);

        // 003/FR-017: gifts leave the shelf like anything else, so every line -
        // gift or paid - is decremented. Grouped back per product because a
        // product may now span a paid line and a gift line.
        foreach (var group in priced.Cart.Lines.GroupBy(line => line.ProductId))
        {
            var quantity = group.Sum(line => line.Quantity);
            var decreased = await _productRepository.TryDecreaseStockAsync(group.Key, quantity, ct);
            if (!decreased)
            {
                var product = priced.Products[group.Key];
                throw new InsufficientStockException(group.Key, quantity, product.StockQuantity);
            }
        }

        var saleLineItems = priced.Cart.Lines
            .Select(line => new SaleLineItem(
                line.ProductId, line.ProductName, line.UnitPrice, line.Quantity, line.DiscountAmount, line.IsGift))
            .ToList();

        var appliedPromotions = priced.Cart.AppliedPromotions
            .Select(promotion => new SaleAppliedPromotion(
                promotion.PromotionId, promotion.Description, promotion.SetCount, promotion.DiscountAmount))
            .ToList();

        var sale = new Sale(request.StaffId, request.MemberId, saleLineItems, appliedPromotions);
        await _saleRepository.AddAsync(sale, ct);

        if (request.MemberId is Guid memberId)
        {
            // 003/FR-022: the accumulated total follows what the customer actually
            // paid, so a gift line (net zero) adds nothing to it by construction.
            await _memberRepository.IncreaseAccumulatedPurchaseTotalAsync(memberId, sale.TotalAmount, ct);
        }

        await transaction.CommitAsync(ct);
        return sale;
    }
}
