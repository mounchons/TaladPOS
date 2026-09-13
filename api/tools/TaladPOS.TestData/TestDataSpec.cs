using TaladPOS.Domain.Promotions;

namespace TaladPOS.TestData;

/// <summary>
/// Store-side attributes products.json does not carry. <see cref="FinalStock"/>
/// is the stock left after the generated sales history; the product is created
/// with that plus everything the history sells. <see cref="Popularity"/> is a
/// relative weight for how often the product lands in a generated bill.
/// </summary>
internal sealed record ProductSpec(
    decimal Price, bool HasBarcode, int FinalStock, int Popularity, int? LowStockThreshold = null);

/// <summary>Start/end are day offsets from today (UTC, the same "today" CompleteSaleUseCase uses).</summary>
internal sealed record PromotionSpec(
    PromotionScope Scope, decimal DiscountPercentage, string? ProductSlug, bool AppliesToMembersOnly,
    int StartOffsetDays, int EndOffsetDays);

internal sealed record MemberSpec(string Name, string PhoneNumber);

/// <summary>
/// The hand-picked shape of the QA dataset. Values are chosen so that after
/// every reset each state the UI distinguishes has at least one example:
/// out of stock, low stock (default and custom threshold), products without a
/// barcode, active / expired / upcoming promotions of both scopes, a
/// members-only promotion that beats an everyone promotion on the same target
/// (FR-022), and a member with no purchases.
/// </summary>
internal static class TestDataSpec
{
    public const int RandomSeed = 20260913;

    /// <summary>Sales are generated for each of the last N days, plus today.</summary>
    public const int HistoryDays = 45;

    public const int BillsToday = 4;

    /// <summary>Asia/Bangkok has no DST, so a fixed offset is exact.</summary>
    public static readonly TimeSpan StoreUtcOffset = TimeSpan.FromHours(7);

    public static readonly TimeSpan StoreOpens = TimeSpan.FromHours(8);

    public static readonly TimeSpan StoreCloses = TimeSpan.FromHours(20);

    /// <summary>Used for a product added to products.json that has no entry in <see cref="Products"/>.</summary>
    public static readonly ProductSpec DefaultProduct = new(Price: 50m, HasBarcode: true, FinalStock: 20, Popularity: 2);

    public static readonly IReadOnlyDictionary<string, ProductSpec> Products = new Dictionary<string, ProductSpec>
    {
        ["apple"] = new(25m, HasBarcode: true, FinalStock: 48, Popularity: 6),
        ["banana"] = new(45m, HasBarcode: true, FinalStock: 35, Popularity: 9),
        ["orange"] = new(15m, HasBarcode: true, FinalStock: 60, Popularity: 8),
        ["mango"] = new(45m, HasBarcode: true, FinalStock: 30, Popularity: 10),
        ["watermelon"] = new(89m, HasBarcode: true, FinalStock: 8, Popularity: 5, LowStockThreshold: 10),
        ["pineapple"] = new(35m, HasBarcode: true, FinalStock: 22, Popularity: 4),
        ["grapes"] = new(120m, HasBarcode: true, FinalStock: 18, Popularity: 5),
        ["strawberry"] = new(150m, HasBarcode: true, FinalStock: 0, Popularity: 4),
        ["papaya"] = new(30m, HasBarcode: true, FinalStock: 26, Popularity: 3),
        ["guava"] = new(20m, HasBarcode: true, FinalStock: 40, Popularity: 4),
        ["dragon-fruit"] = new(40m, HasBarcode: true, FinalStock: 3, Popularity: 3),
        ["durian"] = new(350m, HasBarcode: false, FinalStock: 0, Popularity: 2),
        ["mangosteen"] = new(80m, HasBarcode: true, FinalStock: 4, Popularity: 3),
        ["rambutan"] = new(50m, HasBarcode: true, FinalStock: 5, Popularity: 3),
        ["longan"] = new(70m, HasBarcode: true, FinalStock: 25, Popularity: 3),
        ["lychee"] = new(90m, HasBarcode: true, FinalStock: 12, Popularity: 2),
        ["coconut"] = new(35m, HasBarcode: false, FinalStock: 30, Popularity: 4),
        ["pomelo"] = new(120m, HasBarcode: false, FinalStock: 15, Popularity: 2),
        ["pear"] = new(30m, HasBarcode: true, FinalStock: 20, Popularity: 2),
        ["kiwi"] = new(20m, HasBarcode: true, FinalStock: 1, Popularity: 3),
    };

    public static readonly IReadOnlyList<PromotionSpec> Promotions = new PromotionSpec[]
    {
        new(PromotionScope.Item, 10m, "mango", AppliesToMembersOnly: false, StartOffsetDays: -20, EndOffsetDays: 10),
        new(PromotionScope.Item, 25m, "mango", AppliesToMembersOnly: true, StartOffsetDays: -5, EndOffsetDays: 5),
        new(PromotionScope.Item, 15m, "watermelon", AppliesToMembersOnly: true, StartOffsetDays: -10, EndOffsetDays: 14),
        new(PromotionScope.Item, 20m, "durian", AppliesToMembersOnly: false, StartOffsetDays: -45, EndOffsetDays: -31),
        new(PromotionScope.Item, 30m, "pomelo", AppliesToMembersOnly: false, StartOffsetDays: 3, EndOffsetDays: 10),
        new(PromotionScope.Bill, 5m, null, AppliesToMembersOnly: false, StartOffsetDays: -40, EndOffsetDays: -21),
        new(PromotionScope.Bill, 10m, null, AppliesToMembersOnly: true, StartOffsetDays: -30, EndOffsetDays: 30),
        new(PromotionScope.Bill, 20m, null, AppliesToMembersOnly: false, StartOffsetDays: 7, EndOffsetDays: 21),
    };

    /// <summary>The last member never buys anything, leaving a zero accumulated total to test against.</summary>
    public static readonly IReadOnlyList<MemberSpec> Members = new MemberSpec[]
    {
        new("สมชาย ใจดี", "0812345678"),
        new("สมหญิง รักษ์ไทย", "0898765432"),
        new("วิชัย มั่นคง", "0861112222"),
        new("มาลี ศรีสุข", "0823334444"),
        new("ประเสริฐ ทองดี", "0845556666"),
    };

    public static int PurchasingMemberCount => Members.Count - 1;

    public static ProductSpec For(string slug) => Products.GetValueOrDefault(slug, DefaultProduct);
}
