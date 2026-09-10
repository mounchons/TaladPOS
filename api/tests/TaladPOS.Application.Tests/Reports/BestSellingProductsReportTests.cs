using FluentAssertions;
using TaladPOS.Application.Reports;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Sales;
using Xunit;

namespace TaladPOS.Application.Tests.Reports;

/// <summary>tasks.md T068 - US6: best-selling products report is sorted by QuantitySold descending.</summary>
public class BestSellingProductsReportTests
{
    private static readonly DateOnly Today = new(2026, 9, 10);

    [Fact]
    public async Task ExecuteAsync_SortsByQuantitySoldDescending()
    {
        var mangoId = Guid.NewGuid();
        var appleId = Guid.NewGuid();
        var orangeId = Guid.NewGuid();

        var sales = new FakeSaleRepository(
            new Sale(Guid.NewGuid(), null, [new SaleLineItem(mangoId, "มะม่วง", 45m, 5)]),
            new Sale(Guid.NewGuid(), null, [new SaleLineItem(mangoId, "มะม่วง", 45m, 3)]), // มะม่วง total: 8
            new Sale(Guid.NewGuid(), null, [new SaleLineItem(appleId, "แอปเปิ้ล", 60m, 10)]), // แอปเปิ้ล total: 10
            new Sale(Guid.NewGuid(), null, [new SaleLineItem(orangeId, "ส้ม", 30m, 1)])); // ส้ม total: 1

        var query = new GetBestSellingProductsReportQuery(sales);

        var result = await query.ExecuteAsync(Today.AddDays(-1), Today.AddDays(1), limit: 10);

        result.Should().HaveCount(3);
        result[0].ProductId.Should().Be(appleId);
        result[0].QuantitySold.Should().Be(10);
        result[1].ProductId.Should().Be(mangoId);
        result[1].QuantitySold.Should().Be(8);
        result[2].ProductId.Should().Be(orangeId);
        result[2].QuantitySold.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_RespectsLimit()
    {
        var sales = new FakeSaleRepository(
            new Sale(Guid.NewGuid(), null, [new SaleLineItem(Guid.NewGuid(), "A", 10m, 3)]),
            new Sale(Guid.NewGuid(), null, [new SaleLineItem(Guid.NewGuid(), "B", 10m, 2)]),
            new Sale(Guid.NewGuid(), null, [new SaleLineItem(Guid.NewGuid(), "C", 10m, 1)]));

        var query = new GetBestSellingProductsReportQuery(sales);

        var result = await query.ExecuteAsync(Today.AddDays(-1), Today.AddDays(1), limit: 2);

        result.Should().HaveCount(2);
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        private readonly List<Sale> _sales;

        public FakeSaleRepository(params Sale[] sales) => _sales = sales.ToList();

        public Task AddAsync(Sale sale, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default) => throw new NotSupportedException();

        public Task<IReadOnlyList<Sale>> SearchAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Sale>>(_sales.ToList());
    }
}
