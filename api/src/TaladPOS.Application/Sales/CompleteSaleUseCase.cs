using TaladPOS.Application.Common;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Application.Sales;

public record CompleteSaleLineItemRequest(Guid ProductId, int Quantity);

public record CompleteSaleRequest(
    Guid StaffId, Guid? MemberId, IReadOnlyList<CompleteSaleLineItemRequest> LineItems);

/// <summary>
/// Checkout (contracts/sales.md - POST /api/sales). Runs stock decrements,
/// the Sale insert, and (from US4 onward) the member accumulation update as
/// one database transaction so a decrement can never land without its Sale,
/// or vice versa (FR-016).
/// </summary>
public class CompleteSaleUseCase
{
    private readonly IProductRepository _productRepository;
    private readonly ISaleRepository _saleRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CompleteSaleUseCase(
        IProductRepository productRepository, ISaleRepository saleRepository, IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _saleRepository = saleRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Sale> ExecuteAsync(CompleteSaleRequest request, CancellationToken ct = default)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            throw new ArgumentException("A sale must contain at least one line item.", nameof(request));
        }

        await using var transaction = await _unitOfWork.BeginTransactionAsync(ct);

        var saleLineItems = new List<SaleLineItem>();
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

            saleLineItems.Add(new SaleLineItem(product.Id, product.Name, product.Price, line.Quantity));
        }

        var sale = new Sale(request.StaffId, request.MemberId, saleLineItems);
        await _saleRepository.AddAsync(sale, ct);

        await transaction.CommitAsync(ct);
        return sale;
    }
}
