using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Infrastructure;

namespace TaladPOS.Api.IntegrationTests;

/// <summary>
/// 003/tasks.md T037, T061, T062 - the parts that only a real HTTP round trip
/// against real PostgreSQL can show: who is allowed to call what, that the
/// preview and the checkout agree to the satang, and that bills written before
/// this feature still read back.
/// </summary>
public sealed class ConditionalPromotionTests : ApiTestBase
{
    public ConditionalPromotionTests(TaladPOSApiFactory factory) : base(factory)
    {
    }

    private sealed record ConditionalPromotionDto(
        Guid Id,
        string Name,
        IReadOnlyList<ConditionLineDto> ConditionLines,
        RewardDto Reward,
        bool AppliesToMembersOnly,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsActive,
        string Description,
        bool IsUsable,
        string? UnusableReason);

    private sealed record ConditionLineDto(Guid ProductId, string? ProductName, int MinimumQuantity);

    private sealed record RewardDto(
        string Kind, Guid? GiftProductId, string? GiftProductName, int? GiftQuantity,
        decimal? DiscountPercentage);

    private sealed record PricedLineDto(
        Guid ProductId, string ProductName, decimal UnitPrice, int Quantity,
        decimal DiscountAmount, decimal LineTotal, bool IsGift);

    private sealed record AppliedPromotionDto(
        Guid PromotionId, string Description, int SetCount, decimal DiscountAmount);

    private sealed record UnclaimedGiftDto(
        Guid PromotionId, string Description, Guid GiftProductId, string GiftProductName,
        int MissingQuantity);

    private sealed record PricedCartDto(
        IReadOnlyList<PricedLineDto> Lines,
        IReadOnlyList<AppliedPromotionDto> AppliedPromotions,
        IReadOnlyList<UnclaimedGiftDto> UnclaimedGifts,
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal TotalAmount);

    private sealed record SaleLineItemDto(
        Guid ProductId, string ProductNameSnapshot, decimal UnitPriceSnapshot, int Quantity,
        decimal DiscountAmount, decimal LineTotal, bool IsGift);

    private sealed record SaleDto(
        Guid Id,
        IReadOnlyList<SaleLineItemDto> LineItems,
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal TotalAmount,
        IReadOnlyList<AppliedPromotionDto> AppliedPromotions);

    private static object BuyTwoGetOneFree(Guid productId, string name = "ซื้อสองแถมหนึ่ง") => new
    {
        name,
        conditionLines = new[] { new { productId, minimumQuantity = 2 } },
        reward = new
        {
            kind = "Gift",
            giftProductId = productId,
            giftQuantity = 1,
            discountPercentage = (decimal?)null,
        },
        appliesToMembersOnly = false,
        startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
        endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
    };

    // ---------------------------------------------------------------
    // T037 - CRUD and who is allowed to reach it
    // ---------------------------------------------------------------

    [Fact]
    public async Task Manager_CanCreateReadUpdateAndDeleteAConditionalPromotion()
    {
        var manager = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);
        var mango = await GetProductByNameAsync(manager, "มะม่วง");

        var created = await manager.PostAsJsonAsync(
            "/api/v1/conditional-promotions", BuyTwoGetOneFree(mango.Id));
        created.StatusCode.Should().Be(HttpStatusCode.Created);

        var dto = await created.Content.ReadFromJsonAsync<ConditionalPromotionDto>();
        dto!.Description.Should().Be("ซื้อ มะม่วง 2 แถม มะม่วง 1");
        dto.IsUsable.Should().BeTrue();
        dto.Reward.Kind.Should().Be("Gift");

        var listed = await manager.GetFromJsonAsync<List<ConditionalPromotionDto>>(
            "/api/v1/conditional-promotions");
        listed!.Should().ContainSingle(p => p.Id == dto.Id);

        var apple = await GetProductByNameAsync(manager, "แอปเปิ้ล");
        var updated = await manager.PutAsJsonAsync(
            $"/api/v1/conditional-promotions/{dto.Id}",
            new
            {
                name = "ซื้อคู่แถมแอปเปิ้ล",
                conditionLines = new[]
                {
                    // Keeps มะม่วง from the original condition on purpose: replacing
                    // the list while retaining a product is the case a natural key
                    // on (promotion, product) would have broken (003/data-model.md §2).
                    new { productId = mango.Id, minimumQuantity = 1 },
                    new { productId = apple.Id, minimumQuantity = 1 },
                },
                reward = new
                {
                    kind = "Gift",
                    giftProductId = apple.Id,
                    giftQuantity = 1,
                    discountPercentage = (decimal?)null,
                },
                appliesToMembersOnly = false,
                startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
                endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
            });

        updated.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedDto = await updated.Content.ReadFromJsonAsync<ConditionalPromotionDto>();
        updatedDto!.ConditionLines.Should().HaveCount(2);
        updatedDto.Description.Should().Be("ซื้อ มะม่วง 1 + แอปเปิ้ล 1 แถม แอปเปิ้ล 1");

        var deleted = await manager.DeleteAsync($"/api/v1/conditional-promotions/{dto.Id}");
        deleted.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var afterDelete = await manager.GetFromJsonAsync<List<ConditionalPromotionDto>>(
            "/api/v1/conditional-promotions");
        afterDelete!.Should().NotContain(p => p.Id == dto.Id);
    }

    [Fact]
    public async Task Create_RejectsADuplicateProductInTheCondition_WithTheContractsErrorCode()
    {
        var manager = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);
        var mango = await GetProductByNameAsync(manager, "มะม่วง");

        var response = await manager.PostAsJsonAsync("/api/v1/conditional-promotions", new
        {
            name = "เงื่อนไขซ้ำ",
            conditionLines = new[]
            {
                new { productId = mango.Id, minimumQuantity = 1 },
                new { productId = mango.Id, minimumQuantity = 2 },
            },
            reward = new
            {
                kind = "Gift",
                giftProductId = mango.Id,
                giftQuantity = 1,
                discountPercentage = (decimal?)null,
            },
            appliesToMembersOnly = false,
            startDate = DateTime.UtcNow.ToString("yyyy-MM-dd"),
            endDate = DateTime.UtcNow.AddDays(1).ToString("yyyy-MM-dd"),
        });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadFromJsonAsync<Dictionary<string, string>>();
        body!["error"].Should().Be("duplicate_condition_product");
    }

    [Fact]
    public async Task Cashier_IsForbiddenFromTheConditionalPromotionEndpoints()
    {
        var cashier = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);

        var response = await cashier.GetAsync("/api/v1/conditional-promotions");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Cashier_CanReachThePreviewEndpoint()
    {
        // The whole reason preview lives under /api/v1/sales: the register is
        // run by cashiers, and they cannot read the promotion endpoints at all
        // (003/research.md #3).
        var cashier = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var mango = await GetProductByNameAsync(cashier, "มะม่วง");

        var response = await cashier.PostAsJsonAsync("/api/v1/sales/preview", new
        {
            memberId = (Guid?)null,
            lineItems = new[] { new { productId = mango.Id, quantity = 1 } },
        });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------------
    // T061 - the preview and the charge must agree
    // ---------------------------------------------------------------

    [Fact]
    public async Task PreviewAndCheckout_ProduceTheSameTotalForTheSameCart()
    {
        var manager = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);
        var mango = await GetProductByNameAsync(manager, "มะม่วง");
        (await manager.PostAsJsonAsync("/api/v1/conditional-promotions", BuyTwoGetOneFree(mango.Id)))
            .EnsureSuccessStatusCode();

        var cashier = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var cart = new
        {
            memberId = (Guid?)null,
            lineItems = new[] { new { productId = mango.Id, quantity = 5 } },
        };

        var preview = await (await cashier.PostAsJsonAsync("/api/v1/sales/preview", cart))
            .Content.ReadFromJsonAsync<PricedCartDto>();
        var sale = await (await cashier.PostAsJsonAsync("/api/v1/sales", cart))
            .Content.ReadFromJsonAsync<SaleDto>();

        sale!.TotalAmount.Should().Be(preview!.TotalAmount);
        sale.DiscountAmount.Should().Be(preview.DiscountAmount);
        sale.SubtotalAmount.Should().Be(preview.SubtotalAmount);

        // 5 units, buy 2 get 1: one set of three, two units at full price.
        sale.LineItems.Where(li => li.IsGift).Sum(li => li.Quantity).Should().Be(1);
        sale.LineItems.Sum(li => li.Quantity).Should().Be(5);
        sale.AppliedPromotions.Should().ContainSingle()
            .Which.Description.Should().Be("ซื้อ มะม่วง 2 แถม มะม่วง 1");
    }

    [Fact]
    public async Task ScanOrder_DoesNotChangeTheTotal()
    {
        var manager = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);
        var mango = await GetProductByNameAsync(manager, "มะม่วง");
        var apple = await GetProductByNameAsync(manager, "แอปเปิ้ล");

        (await manager.PostAsJsonAsync("/api/v1/conditional-promotions", new
        {
            name = "ซื้อคู่แถมแอปเปิ้ล",
            conditionLines = new[]
            {
                new { productId = mango.Id, minimumQuantity = 1 },
                new { productId = apple.Id, minimumQuantity = 1 },
            },
            reward = new
            {
                kind = "Gift",
                giftProductId = apple.Id,
                giftQuantity = 1,
                discountPercentage = (decimal?)null,
            },
            appliesToMembersOnly = false,
            startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
            endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
        })).EnsureSuccessStatusCode();

        var cashier = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);

        var forwards = await (await cashier.PostAsJsonAsync("/api/v1/sales/preview", new
        {
            memberId = (Guid?)null,
            lineItems = new[]
            {
                new { productId = mango.Id, quantity = 2 },
                new { productId = apple.Id, quantity = 3 },
            },
        })).Content.ReadFromJsonAsync<PricedCartDto>();

        var backwards = await (await cashier.PostAsJsonAsync("/api/v1/sales/preview", new
        {
            memberId = (Guid?)null,
            lineItems = new[]
            {
                new { productId = apple.Id, quantity = 3 },
                new { productId = mango.Id, quantity = 2 },
            },
        })).Content.ReadFromJsonAsync<PricedCartDto>();

        backwards!.TotalAmount.Should().Be(forwards!.TotalAmount);
        backwards.DiscountAmount.Should().Be(forwards.DiscountAmount);
    }

    [Fact]
    public async Task Preview_ReportsAGiftTheCashierHasNotScannedYet()
    {
        var manager = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);
        var mango = await GetProductByNameAsync(manager, "มะม่วง");
        var apple = await GetProductByNameAsync(manager, "แอปเปิ้ล");

        (await manager.PostAsJsonAsync("/api/v1/conditional-promotions", new
        {
            name = "ซื้อมะม่วงแถมแอปเปิ้ล",
            conditionLines = new[] { new { productId = mango.Id, minimumQuantity = 1 } },
            reward = new
            {
                kind = "Gift",
                giftProductId = apple.Id,
                giftQuantity = 1,
                discountPercentage = (decimal?)null,
            },
            appliesToMembersOnly = false,
            startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
            endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
        })).EnsureSuccessStatusCode();

        var cashier = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var preview = await (await cashier.PostAsJsonAsync("/api/v1/sales/preview", new
        {
            memberId = (Guid?)null,
            lineItems = new[] { new { productId = mango.Id, quantity = 1 } },
        })).Content.ReadFromJsonAsync<PricedCartDto>();

        var hint = preview!.UnclaimedGifts.Should().ContainSingle().Subject;
        hint.GiftProductName.Should().Be("แอปเปิ้ล");
        hint.MissingQuantity.Should().Be(1);

        // The hint is advisory: nothing was added to the cart and no money moved.
        preview.Lines.Should().OnlyContain(line => line.ProductId == mango.Id);
        preview.DiscountAmount.Should().Be(0m);
    }

    // ---------------------------------------------------------------
    // T062 - bills written before this feature still read back
    // ---------------------------------------------------------------

    [Fact]
    public async Task ABillWithNoConditionalPromotion_ReadsBackWithTheNewFieldsAtTheirDefaults()
    {
        var cashier = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var mango = await GetProductByNameAsync(cashier, "มะม่วง");

        var created = await (await cashier.PostAsJsonAsync("/api/v1/sales", new
        {
            memberId = (Guid?)null,
            lineItems = new[] { new { productId = mango.Id, quantity = 2 } },
        })).Content.ReadFromJsonAsync<SaleDto>();

        foreach (var url in new[]
        {
            $"/api/v1/sales/{created!.Id}",
            $"/api/v1/sales/{created.Id}/receipt",
        })
        {
            var fetched = await cashier.GetFromJsonAsync<SaleDto>(url);
            fetched!.LineItems.Should().OnlyContain(li => !li.IsGift);
            fetched.AppliedPromotions.Should().BeEmpty();
            fetched.TotalAmount.Should().Be(created.TotalAmount);
        }
    }

    [Fact]
    public async Task ADeletedProduct_MarksThePromotionUnusableInsteadOfHidingIt()
    {
        var manager = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);
        var mango = await GetProductByNameAsync(manager, "มะม่วง");
        var apple = await GetProductByNameAsync(manager, "แอปเปิ้ล");

        (await manager.PostAsJsonAsync("/api/v1/conditional-promotions", new
        {
            name = "ซื้อมะม่วงแถมแอปเปิ้ล",
            conditionLines = new[] { new { productId = mango.Id, minimumQuantity = 1 } },
            reward = new
            {
                kind = "Gift",
                giftProductId = apple.Id,
                giftQuantity = 1,
                discountPercentage = (decimal?)null,
            },
            appliesToMembersOnly = false,
            startDate = DateTime.UtcNow.AddDays(-1).ToString("yyyy-MM-dd"),
            endDate = DateTime.UtcNow.AddDays(30).ToString("yyyy-MM-dd"),
        })).EnsureSuccessStatusCode();

        (await manager.DeleteAsync($"/api/v1/products/{apple.Id}")).EnsureSuccessStatusCode();

        var listed = await manager.GetFromJsonAsync<List<ConditionalPromotionDto>>(
            "/api/v1/conditional-promotions");
        var promotion = listed!.Single();
        promotion.IsUsable.Should().BeFalse();
        promotion.UnusableReason.Should().NotBeNullOrWhiteSpace();
        promotion.Reward.GiftProductName.Should().BeNull();

        // And it stops firing at the register, silently, with no hint pointing at
        // a product the cashier cannot fetch.
        var cashier = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var preview = await (await cashier.PostAsJsonAsync("/api/v1/sales/preview", new
        {
            memberId = (Guid?)null,
            lineItems = new[] { new { productId = mango.Id, quantity = 1 } },
        })).Content.ReadFromJsonAsync<PricedCartDto>();

        preview!.AppliedPromotions.Should().BeEmpty();
        preview.UnclaimedGifts.Should().BeEmpty();
    }
}
