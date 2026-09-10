using TaladPOS.Application.Sales;

namespace TaladPOS.Application.Reports;

public sealed record BestSellingProductRow(Guid ProductId, string ProductName, int QuantitySold, decimal TotalSalesAmount);

/// <summary>contracts/reports.md - GET /api/reports/best-selling-products (FR-026), sorted by QuantitySold desc.</summary>
public sealed class GetBestSellingProductsReportQuery
{
    private readonly ISaleRepository _saleRepository;

    public GetBestSellingProductsReportQuery(ISaleRepository saleRepository) => _saleRepository = saleRepository;

    public async Task<IReadOnlyList<BestSellingProductRow>> ExecuteAsync(
        DateOnly from, DateOnly to, int limit, CancellationToken ct = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var sales = await _saleRepository.SearchAsync(fromUtc, toUtc, staffId: null, memberId: null, ct);

        return sales
            .SelectMany(s => s.LineItems)
            .GroupBy(li => li.ProductId)
            .Select(g => new BestSellingProductRow(
                g.Key, g.First().ProductNameSnapshot, g.Sum(li => li.Quantity), g.Sum(li => li.LineTotal)))
            .OrderByDescending(r => r.QuantitySold)
            .Take(limit)
            .ToList();
    }
}
