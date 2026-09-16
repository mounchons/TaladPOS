using FluentAssertions;
using TaladPOS.Application.Common;
using TaladPOS.Application.Reports;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Sales;
using Xunit;

namespace TaladPOS.Application.Tests.Reports;

/// <summary>
/// 003/tasks.md T068, T071 - what the existing reports mean once a bill can
/// contain a giveaway (003/FR-025, 003/FR-027).
///
/// No report code changed for this feature. These tests pin the behaviour that
/// already falls out of the data model, so that if someone later "tidies up" a
/// sum, the decision recorded in FR-027 fails loudly instead of quietly
/// changing what the shop's numbers mean.
/// </summary>
public class GiftLinesInReportsTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);
    private static readonly Guid Noodles = Guid.Parse("11111111-0000-0000-0000-000000000001");
    private static readonly Guid Snack = Guid.Parse("22222222-0000-0000-0000-000000000002");

    /// <summary>
    /// A bill for two 20-baht noodles that also gave away one 100-baht snack:
    /// the snack is on the bill at its real price with a discount that cancels
    /// it, which is what FR-016 requires of the receipt.
    /// </summary>
    private static Sale BillWithOneGift() =>
        new(
            Guid.NewGuid(),
            null,
            new[]
            {
                new SaleLineItem(Noodles, "บะหมี่", 20m, 2),
                new SaleLineItem(Snack, "ขนม", 100m, 1, 100m, isGift: true),
            },
            new[]
            {
                new SaleAppliedPromotion(Guid.NewGuid(), "ซื้อ บะหมี่ 2 แถม ขนม 1", 1, 100m),
            });

    [Fact]
    public async Task SalesReport_CountsTheGiftAsDiscountButNotAsRevenue()
    {
        var query = new GetDailyOrMonthlySalesReportQuery(new FakeSaleRepository(BillWithOneGift()));

        var report = await query.ExecuteAsync("daily", Today);

        report.TotalSalesAmount.Should().Be(40m, "only the noodles were paid for");
        report.TotalDiscountAmount.Should().Be(
            100m, "FR-027: the value handed over as a gift is counted as a discount, at list price");
        report.BillCount.Should().Be(1);
    }

    [Fact]
    public async Task BestSelling_CountsGiftUnitsSoTheFigureMatchesWhatLeftTheShelf()
    {
        var query = new GetBestSellingProductsReportQuery(new FakeSaleRepository(BillWithOneGift()));

        var rows = await query.ExecuteAsync(Today, Today, limit: 10);

        var snack = rows.Single(r => r.ProductId == Snack);
        snack.QuantitySold.Should().Be(
            1,
            "FR-027: FR-017 takes the gift out of stock, so a report that ignored it "
                + "could never be reconciled against stock movement");
        snack.TotalSalesAmount.Should().Be(0m, "it earned nothing, and the report says so");

        rows.Single(r => r.ProductId == Noodles).QuantitySold.Should().Be(2);
    }

    [Fact]
    public async Task BestSelling_CountsPaidAndGiftUnitsOfTheSameProductTogether()
    {
        // "ซื้อ 2 แถม 1": three units left the shelf, the customer paid for two.
        var sale = new Sale(
            Guid.NewGuid(),
            null,
            new[]
            {
                new SaleLineItem(Noodles, "บะหมี่", 20m, 2),
                new SaleLineItem(Noodles, "บะหมี่", 20m, 1, 20m, isGift: true),
            });

        var rows = await new GetBestSellingProductsReportQuery(new FakeSaleRepository(sale))
            .ExecuteAsync(Today, Today, limit: 10);

        rows.Single().QuantitySold.Should().Be(3);
        rows.Single().TotalSalesAmount.Should().Be(40m);
    }

    [Fact]
    public async Task ABillWithoutGifts_ReportsExactlyAsItDidBeforeThisFeature()
    {
        var sale = new Sale(
            Guid.NewGuid(),
            null,
            new[] { new SaleLineItem(Noodles, "บะหมี่", 20m, 3, 6m) });

        var report = await new GetDailyOrMonthlySalesReportQuery(new FakeSaleRepository(sale))
            .ExecuteAsync("daily", Today);

        report.TotalSalesAmount.Should().Be(54m);
        report.TotalDiscountAmount.Should().Be(6m);

        sale.LineItems.Should().OnlyContain(li => !li.IsGift, "IsGift defaults to false");
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        private readonly List<Sale> _sales;

        public FakeSaleRepository(params Sale[] sales) => _sales = sales.ToList();

        public Task AddAsync(Sale sale, CancellationToken ct = default)
        {
            _sales.Add(sale);
            return Task.CompletedTask;
        }

        public Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_sales.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<Sale>> SearchAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Sale>>(_sales);

        public Task<PagedResult<Sale>> SearchPagedAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, PageRequest page,
            CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");
    }
}
