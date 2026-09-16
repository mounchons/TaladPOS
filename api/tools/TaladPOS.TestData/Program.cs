using System.Text;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using TaladPOS.Domain.Promotions;
using TaladPOS.Infrastructure.Persistence;
using TaladPOS.TestData;

// Resets the dev database to a known QA dataset: wipes products, promotions,
// members and sales, then rebuilds them from docs/image/product/products.json
// plus TestDataSpec.cs. Staff accounts are kept so logins keep working.
// Usually run through scripts/reset-test-data.ps1.

Console.OutputEncoding = Encoding.UTF8;

ToolOptions options;
try
{
    options = ToolOptions.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    Console.Error.WriteLine(ToolOptions.Usage);
    return 2;
}

if (options.ShowHelp)
{
    Console.WriteLine(ToolOptions.Usage);
    return 0;
}

var database = new NpgsqlConnectionStringBuilder(options.ConnectionString).Database ?? string.Empty;
if (!options.AllowAnyDatabase && database != ToolOptions.DevDatabaseName)
{
    Console.Error.WriteLine(
        $"Refusing to reset database '{database}': only '{ToolOptions.DevDatabaseName}' is reset by default. "
        + "Pass --allow-any-database if it really is disposable.");
    return 2;
}

try
{
    var catalog = ProductCatalog.Load(options.ProductsJsonPath);
    Console.WriteLine($"Catalog: {catalog.Count} products from {Path.GetRelativePath(options.RepoRoot, options.ProductsJsonPath)}");

    var imageUrls = ProductCatalog.PublishImages(catalog, options.RepoRoot, options.ImageOutputDir, options.ImageBaseUrl);
    Console.WriteLine($"Images : {catalog.Count * 2} files in {Path.GetRelativePath(options.RepoRoot, options.ImageOutputDir)}");

    var dbOptions = new DbContextOptionsBuilder<TaladPOSDbContext>().UseNpgsql(options.ConnectionString).Options;
    await using var db = new TaladPOSDbContext(dbOptions);

    await db.Database.MigrateAsync();

    var utcNow = DateTime.UtcNow;
    TestDataSummary summary;

    // One transaction: a failure part-way leaves the previous data untouched
    // instead of a half-empty database.
    await using (var transaction = await db.Database.BeginTransactionAsync())
    {
        // No CASCADE on purpose: a future table referencing these should make
        // this fail loudly rather than be wiped silently. Staff is not listed.
        await db.Database.ExecuteSqlRawAsync(
            "TRUNCATE TABLE sale_applied_promotions, sale_line_items, sales, "
            + "conditional_promotion_lines, conditional_promotions, promotions, members, products;");

        summary = await new TestDataBuilder(db).BuildAsync(catalog, imageUrls, utcNow);
        await transaction.CommitAsync();
    }

    PrintSummary(summary, database, DateOnly.FromDateTime(utcNow));
    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine();
    Console.Error.WriteLine($"Reset failed: {ex.Message}");
    if (ex is NpgsqlException)
    {
        Console.Error.WriteLine("Is PostgreSQL running? (docker compose up -d postgres, from the repository root)");
    }

    Console.Error.WriteLine(ex);
    return 1;
}

static void PrintSummary(TestDataSummary summary, string database, DateOnly today)
{
    static string Names(IEnumerable<TaladPOS.Domain.Products.Product> products)
    {
        var names = products.Select(p => $"{p.Name} ({p.StockQuantity})").ToList();
        return names.Count == 0 ? "-" : string.Join(", ", names);
    }

    var productNames = summary.Products.ToDictionary(p => p.Id, p => p.Name);
    var sales = summary.Sales;
    var first = sales.Min(s => s.CreatedAtUtc) + TestDataSpec.StoreUtcOffset;
    var last = sales.Max(s => s.CreatedAtUtc) + TestDataSpec.StoreUtcOffset;

    Console.WriteLine();
    Console.WriteLine($"Reset complete - database '{database}'");
    Console.WriteLine($"  Products    : {summary.Products.Count}");
    Console.WriteLine($"  Out of stock: {Names(summary.Products.Where(p => p.IsOutOfStock))}");
    Console.WriteLine($"  Low stock   : {Names(summary.Products.Where(p => p.IsLowStock))}");
    Console.WriteLine($"  No barcode  : {Names(summary.Products.Where(p => p.Barcode is null))}");
    Console.WriteLine($"  Members     : {summary.Members.Count}");
    Console.WriteLine(
        $"  Sales       : {sales.Count} bills, {sales.Sum(s => s.LineItems.Count)} line items, "
        + $"{sales.Count(s => s.DiscountAmount > 0)} discounted, total {sales.Sum(s => s.TotalAmount):N2} THB");
    Console.WriteLine($"                {first:yyyy-MM-dd HH:mm} .. {last:yyyy-MM-dd HH:mm} (store time, UTC+7)");
    Console.WriteLine($"  Promotions  : {summary.Promotions.Count}");

    foreach (var promotion in summary.Promotions)
    {
        var target = promotion.ProductId is Guid id ? productNames[id] : "whole bill";
        var state = promotion.IsActive(today) ? "active" : promotion.EndDate < today ? "expired" : "upcoming";
        var audience = promotion.AppliesToMembersOnly ? "members only" : "everyone";
        Console.WriteLine(
            $"    - {promotion.Scope,-4} {promotion.DiscountPercentage,3:0}%  {target} / {audience}  "
            + $"{promotion.StartDate:yyyy-MM-dd} .. {promotion.EndDate:yyyy-MM-dd}  [{state}]");
    }

    Console.WriteLine($"  Bundles     : {summary.ConditionalPromotions.Count}");

    foreach (var promotion in summary.ConditionalPromotions)
    {
        var state = promotion.IsActive(today) ? "active" : promotion.EndDate < today ? "expired" : "upcoming";
        var audience = promotion.AppliesToMembersOnly ? "members only" : "everyone";
        Console.WriteLine(
            $"    - {promotion.Describe(productNames)} / {audience}  "
            + $"{promotion.StartDate:yyyy-MM-dd} .. {promotion.EndDate:yyyy-MM-dd}  [{state}]");
    }

    Console.WriteLine($"  Staff kept  : {string.Join(", ", summary.Staff.Select(s => $"{s.Username} ({s.Role})"))}");

    foreach (var warning in summary.Warnings)
    {
        Console.WriteLine($"  WARNING: {warning}");
    }
}
