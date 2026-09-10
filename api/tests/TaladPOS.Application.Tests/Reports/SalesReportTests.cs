using FluentAssertions;
using TaladPOS.Application.Reports;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Sales;
using Xunit;

namespace TaladPOS.Application.Tests.Reports;

/// <summary>tasks.md T067 - US6: daily/monthly sales report totals TotalSalesAmount and BillCount for the requested range.</summary>
public class SalesReportTests
{
    private static Sale NewSale(DateTime createdAtUtc, decimal unitPrice, int quantity)
    {
        var sale = new Sale(Guid.NewGuid(), null, [new SaleLineItem(Guid.NewGuid(), "มะม่วง", unitPrice, quantity)]);
        typeof(Sale).GetProperty(nameof(Sale.CreatedAtUtc))!.SetValue(sale, createdAtUtc);
        return sale;
    }

    [Fact]
    public async Task ExecuteAsync_Daily_SumsOnlySalesWithinThatSingleDay()
    {
        var sales = new FakeSaleRepository(
            NewSale(new DateTime(2026, 9, 10, 8, 0, 0, DateTimeKind.Utc), 45m, 2),   // in range: 90.00
            NewSale(new DateTime(2026, 9, 10, 23, 59, 0, DateTimeKind.Utc), 60m, 1), // in range: 60.00
            NewSale(new DateTime(2026, 9, 11, 0, 1, 0, DateTimeKind.Utc), 100m, 1)); // out of range
        var query = new GetDailyOrMonthlySalesReportQuery(sales);

        var result = await query.ExecuteAsync("daily", new DateOnly(2026, 9, 10));

        result.BillCount.Should().Be(2);
        result.TotalSalesAmount.Should().Be(150.00m);
        result.RangeStart.Should().Be(new DateOnly(2026, 9, 10));
        result.RangeEnd.Should().Be(new DateOnly(2026, 9, 10));
    }

    [Fact]
    public async Task ExecuteAsync_Monthly_SumsAllSalesWithinThatCalendarMonth()
    {
        var sales = new FakeSaleRepository(
            NewSale(new DateTime(2026, 9, 1, 0, 0, 0, DateTimeKind.Utc), 45m, 1),  // in range
            NewSale(new DateTime(2026, 9, 30, 23, 0, 0, DateTimeKind.Utc), 45m, 1), // in range
            NewSale(new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc), 45m, 1)); // out of range
        var query = new GetDailyOrMonthlySalesReportQuery(sales);

        var result = await query.ExecuteAsync("monthly", new DateOnly(2026, 9, 15));

        result.BillCount.Should().Be(2);
        result.TotalSalesAmount.Should().Be(90.00m);
        result.RangeStart.Should().Be(new DateOnly(2026, 9, 1));
        result.RangeEnd.Should().Be(new DateOnly(2026, 9, 30));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoMatchingSales_ReturnsZeroedResult()
    {
        var sales = new FakeSaleRepository();
        var query = new GetDailyOrMonthlySalesReportQuery(sales);

        var result = await query.ExecuteAsync("daily", new DateOnly(2026, 9, 10));

        result.BillCount.Should().Be(0);
        result.TotalSalesAmount.Should().Be(0m);
        result.TotalDiscountAmount.Should().Be(0m);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPeriod_Throws()
    {
        var query = new GetDailyOrMonthlySalesReportQuery(new FakeSaleRepository());

        var act = async () => await query.ExecuteAsync("yearly", new DateOnly(2026, 9, 10));

        await act.Should().ThrowAsync<ArgumentException>();
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        private readonly List<Sale> _sales;

        public FakeSaleRepository(params Sale[] sales) => _sales = sales.ToList();

        public Task AddAsync(Sale sale, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Sale>> SearchAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default)
        {
            var result = _sales.Where(s =>
                (from is null || s.CreatedAtUtc >= from)
                && (to is null || s.CreatedAtUtc <= to)
                && (staffId is null || s.StaffId == staffId)
                && (memberId is null || s.MemberId == memberId));

            return Task.FromResult<IReadOnlyList<Sale>>(result.ToList());
        }
    }
}
