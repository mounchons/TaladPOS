using TaladPOS.Application.Sales;

namespace TaladPOS.Application.Reports;

public sealed record SalesReportResult(
    string Period, DateOnly RangeStart, DateOnly RangeEnd, decimal TotalSalesAmount, decimal TotalDiscountAmount,
    int BillCount);

/// <summary>contracts/reports.md - GET /api/reports/sales (FR-025)</summary>
public sealed class GetDailyOrMonthlySalesReportQuery
{
    private readonly ISaleRepository _saleRepository;

    public GetDailyOrMonthlySalesReportQuery(ISaleRepository saleRepository) => _saleRepository = saleRepository;

    public async Task<SalesReportResult> ExecuteAsync(string period, DateOnly date, CancellationToken ct = default)
    {
        var (rangeStart, rangeEnd) = period switch
        {
            "daily" => (date, date),
            "monthly" => (
                new DateOnly(date.Year, date.Month, 1),
                new DateOnly(date.Year, date.Month, DateTime.DaysInMonth(date.Year, date.Month))),
            _ => throw new ArgumentException($"period must be 'daily' or 'monthly', got '{period}'.", nameof(period)),
        };

        var fromUtc = rangeStart.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = rangeEnd.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);

        var sales = await _saleRepository.SearchAsync(fromUtc, toUtc, staffId: null, memberId: null, ct);

        return new SalesReportResult(
            period, rangeStart, rangeEnd, sales.Sum(s => s.TotalAmount), sales.Sum(s => s.DiscountAmount), sales.Count);
    }
}
