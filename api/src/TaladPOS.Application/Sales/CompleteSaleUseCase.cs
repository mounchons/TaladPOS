using TaladPOS.Application.Common;
using TaladPOS.Application.Members;
using TaladPOS.Application.Products;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Promotions;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Application.Sales;

public record CompleteSaleLineItemRequest(Guid ProductId, int Quantity);

public record CompleteSaleRequest(
    Guid StaffId, Guid? MemberId, IReadOnlyList<CompleteSaleLineItemRequest> LineItems);

/// <summary>
/// Checkout (contracts/sales.md - POST /api/v1/sales). Runs stock decrements,
/// the Sale insert, the member accumulation update (US4, FR-014), and
/// discount resolution (US5, FR-019-FR-022) as one database transaction so
/// none of them can land without the others (FR-016).
/// </summary>
public class CompleteSaleUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IMemberRepository _memberRepository;
    private readonly IPromotionRepository _promotionRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSaleUseCase(
        IProductRepository productRepository,
        ISaleRepository saleRepository,
        IMemberRepository memberRepository,
        IPromotionRepository promotionRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _memberRepository = memberRepository;
        _promotionRepository = promotionRepository;
        _unitOfWork = unitOfWork;
    }

    private sealed record PendingLine(Guid ProductId, string Name, decimal UnitPrice, int Quantity, decimal ItemDiscount)
    {
        public decimal Subtotal => UnitPrice * Quantity;
    }

    public async Task<Sale> ExecuteAsync(CompleteSaleRequest request, CancellationToken ct = default)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            throw new ArgumentException("A sale must contain at least one line item.", nameof(request));
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        if (request.MemberId is Guid requestedMemberId
            && await _memberRepository.GetByIdAsync(requestedMemberId, ct) is null)
        {
            throw new KeyNotFoundException($"Member {requestedMemberId} was not found.");
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activePromotions = await _promotionRepository.GetActiveOnAsync(today, ct);
        var hasMember = request.MemberId is not null;

        var pendingLines = new List<PendingLine>();
        foreach (var line in request.LineItems)
        {
            if (line.Quantity <= 0)
            {
                throw new ArgumentException(
                    $"Quantity for product {line.ProductId} must be greater than 0.", nameof(request));
            }

            var product = await _productRepository.GetByIdAsync(line.ProductId, ct)
                ?? throw new KeyNotFoundException($"Product {line.ProductId} was not found.");

            var decreased = await _productRepository.TryDecreaseStockAsync(line.ProductId, line.Quantity, ct);
            if (!decreased)
            {
                throw new InsufficientStockException(line.ProductId, line.Quantity, product.StockQuantity);
            }

            var lineSubtotal = product.Price * line.Quantity;
            var itemDiscount = DiscountResolver.ResolveItemDiscount(
                activePromotions, product.Id, hasMember, lineSubtotal, today);

            pendingLines.Add(new PendingLine(product.Id, product.Name, product.Price, line.Quantity, itemDiscount));
        }

        var billSubtotal = pendingLines.Sum(l => l.Subtotal);
        var billDiscount = DiscountResolver.ResolveBillDiscount(activePromotions, hasMember, billSubtotal, today);

        var saleLineItems = DistributeBillDiscount(pendingLines, billDiscount);

        var sale = new Sale(request.StaffId, request.MemberId, saleLineItems);
        await _saleRepository.AddAsync(sale, ct);

        if (request.MemberId is Guid memberId)
        {
            await _memberRepository.IncreaseAccumulatedPurchaseTotalAsync(memberId, sale.TotalAmount, ct);
        }

        await transaction.CommitAsync(ct);
        return sale;
    }

    /// <summary>
    /// A Bill-scope discount (FR-022) has no dedicated column on Sale -
    /// Sale.DiscountAmount is the sum of its line items' DiscountAmount
    /// (data-model.md), by design (keeps the aggregate's invariants derived
    /// rather than duplicated). It's prorated across lines by each line's
    /// share of the subtotal, with the rounding remainder folded into the
    /// last line, so SubtotalAmount - DiscountAmount == TotalAmount exactly.
    /// </summary>
    private static List<SaleLineItem> DistributeBillDiscount(IReadOnlyList<PendingLine> lines, decimal billDiscount)
    {
        var result = new List<SaleLineItem>(lines.Count);

        if (billDiscount == 0m)
        {
            foreach (var line in lines)
            {
                result.Add(new SaleLineItem(line.ProductId, line.Name, line.UnitPrice, line.Quantity, line.ItemDiscount));
            }

            return result;
        }

        var billSubtotal = lines.Sum(l => l.Subtotal);
        var distributed = 0m;
        for (var i = 0; i < lines.Count; i++)
        {
            var line = lines[i];
            var share = i == lines.Count - 1
                ? billDiscount - distributed
                : Math.Round(billDiscount * (line.Subtotal / billSubtotal), 2);
            distributed += share;

            result.Add(new SaleLineItem(line.ProductId, line.Name, line.UnitPrice, line.Quantity, line.ItemDiscount + share));
        }

        return result;
    }
}
