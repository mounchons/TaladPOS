using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Auth;
using TaladPOS.Domain.Members;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Promotions;
using TaladPOS.Domain.Sales;
using TaladPOS.Domain.Staff;
using TaladPOS.Infrastructure.Persistence;
using TaladPOS.Infrastructure.Repositories;
using TaladPOS.Infrastructure.Seed;

namespace TaladPOS.TestData;

internal sealed record PlannedLine(string Slug, int Quantity);

internal sealed record PlannedSale(
    DateTime CreatedAtUtc, bool SoldByManager, int? MemberIndex, IReadOnlyList<PlannedLine> Lines);

internal sealed record TestDataSummary(
    IReadOnlyList<Product> Products,
    IReadOnlyList<Promotion> Promotions,
    IReadOnlyList<Member> Members,
    IReadOnlyList<Sale> Sales,
    IReadOnlyList<Staff> Staff,
    IReadOnlyList<string> Warnings);

/// <summary>
/// Builds the QA dataset through the domain constructors, so every row passes
/// the same validation the API applies. Expects the business tables to be
/// empty; the caller owns the transaction.
/// </summary>
internal sealed class TestDataBuilder
{
    private readonly TaladPOSDbContext _db;
    private readonly Random _random = new(TestDataSpec.RandomSeed);

    public TestDataBuilder(TaladPOSDbContext db) => _db = db;

    public async Task<TestDataSummary> BuildAsync(
        IReadOnlyList<CatalogProduct> catalog,
        IReadOnlyDictionary<string, string> imageUrls,
        DateTime utcNow,
        CancellationToken ct = default)
    {
        var warnings = new List<string>();

        // Planned before any product exists, so each product can be created
        // with exactly what its history sells plus the stock it should end with.
        var plannedSales = PlanSales(catalog, utcNow);
        var soldBySlug = plannedSales
            .SelectMany(s => s.Lines)
            .GroupBy(l => l.Slug)
            .ToDictionary(g => g.Key, g => g.Sum(l => l.Quantity));

        var products = new Dictionary<string, Product>();
        foreach (var item in catalog)
        {
            if (!TestDataSpec.Products.ContainsKey(item.Slug))
            {
                warnings.Add($"No ProductSpec for '{item.Slug}' in TestDataSpec.cs - used defaults.");
            }

            var spec = TestDataSpec.For(item.Slug);
            products[item.Slug] = new Product(
                item.NameTh,
                imageUrls[item.Slug],
                spec.Price,
                spec.HasBarcode ? Ean13(item.Id) : null,
                spec.FinalStock + soldBySlug.GetValueOrDefault(item.Slug),
                spec.LowStockThreshold);
        }

        _db.Products.AddRange(products.Values);
        await _db.SaveChangesAsync(ct);

        // Runs after the products exist so it only restores missing staff
        // accounts and never adds its two placeholder products.
        await DevelopmentSeeder.SeedAsync(_db, new StaffAuthenticator(new StaffRepository(_db)));
        var staff = await _db.Staff.OrderBy(s => s.Username).ToListAsync(ct);
        var manager = staff.FirstOrDefault(s => s.Username == "manager")
            ?? staff.FirstOrDefault(s => s.Role == StaffRole.Manager)
            ?? staff.First();
        var cashier = staff.FirstOrDefault(s => s.Username == "cashier")
            ?? staff.FirstOrDefault(s => s.Role == StaffRole.Cashier)
            ?? manager;

        var today = DateOnly.FromDateTime(utcNow);
        var promotions = new List<Promotion>();
        foreach (var spec in TestDataSpec.Promotions)
        {
            Guid? productId = null;
            if (spec.ProductSlug is not null)
            {
                if (!products.TryGetValue(spec.ProductSlug, out var product))
                {
                    warnings.Add($"Skipped a promotion for '{spec.ProductSlug}', which is not in products.json.");
                    continue;
                }

                productId = product.Id;
            }

            promotions.Add(new Promotion(
                spec.Scope,
                spec.DiscountPercentage,
                productId,
                spec.AppliesToMembersOnly,
                today.AddDays(spec.StartOffsetDays),
                today.AddDays(spec.EndOffsetDays)));
        }

        _db.Promotions.AddRange(promotions);

        var members = TestDataSpec.Members.Select(m => new Member(m.Name, m.PhoneNumber)).ToList();
        _db.Members.AddRange(members);

        var sales = new List<Sale>(plannedSales.Count);
        foreach (var plan in plannedSales)
        {
            var member = plan.MemberIndex is int index ? members[index] : null;
            var sale = BuildSale(plan, plan.SoldByManager ? manager : cashier, member, products, promotions);

            _db.Sales.Add(sale);

            // Sale stamps CreatedAtUtc with the current time and deliberately
            // has no API to change it, so back-dating goes through EF directly.
            _db.Entry(sale).Property(s => s.CreatedAtUtc).CurrentValue = plan.CreatedAtUtc;

            member?.IncreaseAccumulatedPurchaseTotal(sale.TotalAmount);
            sales.Add(sale);
        }

        await _db.SaveChangesAsync(ct);

        return new TestDataSummary(products.Values.ToList(), promotions, members, sales, staff, warnings);
    }

    private List<PlannedSale> PlanSales(IReadOnlyList<CatalogProduct> catalog, DateTime utcNow)
    {
        var localNow = utcNow + TestDataSpec.StoreUtcOffset;
        var pool = catalog.Select(p => (p.Slug, Weight: TestDataSpec.For(p.Slug).Popularity)).ToList();
        var sales = new List<PlannedSale>();

        for (var daysAgo = TestDataSpec.HistoryDays; daysAgo >= 0; daysAgo--)
        {
            var localDate = localNow.Date.AddDays(-daysAgo);
            var opens = localDate + TestDataSpec.StoreOpens;
            var closes = daysAgo == 0 ? localNow : localDate + TestDataSpec.StoreCloses;

            // Today's bills must not be in the future; before opening time
            // they are spread over the hours since midnight instead.
            if (closes <= opens)
            {
                opens = localDate;
            }

            if (closes <= opens)
            {
                continue;
            }

            var billCount = daysAgo == 0 ? TestDataSpec.BillsToday : _random.Next(1, 4);
            for (var i = 0; i < billCount; i++)
            {
                var localTime = opens + TimeSpan.FromTicks((long)(_random.NextDouble() * (closes - opens).Ticks));
                var soldByManager = _random.NextDouble() < 0.35;
                int? memberIndex = _random.NextDouble() < 0.4
                    ? _random.Next(TestDataSpec.PurchasingMemberCount)
                    : null;
                var lines = PickWeighted(pool, _random.Next(1, 5))
                    .Select(slug => new PlannedLine(slug, _random.Next(1, 4)))
                    .ToList();

                sales.Add(new PlannedSale(
                    DateTime.SpecifyKind(localTime - TestDataSpec.StoreUtcOffset, DateTimeKind.Utc),
                    soldByManager,
                    memberIndex,
                    lines));
            }
        }

        return sales.OrderBy(s => s.CreatedAtUtc).ToList();
    }

    /// <summary>Distinct slugs, each draw weighted by popularity among those not yet picked.</summary>
    private List<string> PickWeighted(IReadOnlyList<(string Slug, int Weight)> pool, int count)
    {
        var remaining = pool.Where(p => p.Weight > 0).ToList();
        var picked = new List<string>(count);

        while (picked.Count < count && remaining.Count > 0)
        {
            var roll = _random.Next(remaining.Sum(p => p.Weight));
            var index = 0;
            while (roll >= remaining[index].Weight)
            {
                roll -= remaining[index].Weight;
                index++;
            }

            picked.Add(remaining[index].Slug);
            remaining.RemoveAt(index);
        }

        return picked;
    }

    /// <summary>
    /// Prices a planned bill the way CompleteSaleUseCase does, but on the
    /// bill's own date instead of today: the best Item discount per line,
    /// then the best Bill discount on the pre-discount subtotal, prorated
    /// across lines with the rounding remainder on the last one
    /// (CompleteSaleUseCase.DistributeBillDiscount). Keep the two in step if
    /// checkout pricing changes.
    /// </summary>
    private static Sale BuildSale(
        PlannedSale plan,
        Staff seller,
        Member? member,
        IReadOnlyDictionary<string, Product> products,
        IReadOnlyList<Promotion> promotions)
    {
        var date = DateOnly.FromDateTime(plan.CreatedAtUtc);
        var hasMember = member is not null;

        var priced = new List<(Product Product, int Quantity, decimal Subtotal, decimal ItemDiscount)>();
        foreach (var line in plan.Lines)
        {
            var product = products[line.Slug];
            product.DecreaseStock(line.Quantity);

            var subtotal = product.Price * line.Quantity;
            var itemDiscount = DiscountResolver.ResolveItemDiscount(promotions, product.Id, hasMember, subtotal, date);
            priced.Add((product, line.Quantity, subtotal, itemDiscount));
        }

        var billSubtotal = priced.Sum(l => l.Subtotal);
        var billDiscount = DiscountResolver.ResolveBillDiscount(promotions, hasMember, billSubtotal, date);

        var lineItems = new List<SaleLineItem>(priced.Count);
        var distributed = 0m;
        for (var i = 0; i < priced.Count; i++)
        {
            var (product, quantity, subtotal, itemDiscount) = priced[i];
            var share = billDiscount == 0m ? 0m
                : i == priced.Count - 1 ? billDiscount - distributed
                : Math.Round(billDiscount * (subtotal / billSubtotal), 2);
            distributed += share;

            lineItems.Add(new SaleLineItem(product.Id, product.Name, product.Price, quantity, itemDiscount + share));
        }

        return new Sale(seller.Id, member?.Id, lineItems);
    }

    /// <summary>EAN-13 in a made-up 885-1234 range, clear of DevelopmentSeeder's 8850000000012.</summary>
    private static string Ean13(int catalogId)
    {
        var body = $"8851234{catalogId:D5}";
        var sum = body.Select((digit, i) => (digit - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return body + (10 - (sum % 10)) % 10;
    }
}
