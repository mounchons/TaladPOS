using TaladPOS.Application.Products;

namespace TaladPOS.Application.Reports;

public sealed record StockReportRow(Guid ProductId, string ProductName, int StockQuantity, int LowStockThreshold, bool IsLowStock);

/// <summary>contracts/reports.md - GET /api/reports/stock (FR-028)</summary>
public sealed class GetStockReportQuery
{
    private readonly IProductRepository _productRepository;

    public GetStockReportQuery(IProductRepository productRepository) => _productRepository = productRepository;

    public async Task<IReadOnlyList<StockReportRow>> ExecuteAsync(CancellationToken ct = default)
    {
        var products = await _productRepository.SearchAsync(search: null, barcode: null, lowStockOnly: false, ct);
        return products
            .Select(p => new StockReportRow(p.Id, p.Name, p.StockQuantity, p.LowStockThreshold, p.IsLowStock))
            .ToList();
    }
}
