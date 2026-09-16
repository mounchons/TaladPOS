using FluentAssertions;
using TaladPOS.Domain.Promotions;
using Xunit;

namespace TaladPOS.Domain.Tests.Promotions;

/// <summary>
/// 003/tasks.md T004, T038, T041, T048, T049, T053, T057 - T060.
///
/// Every acceptance scenario in 003/spec.md with the numbers it states, plus
/// the edge cases. The numbers are the point: a test that only asserts "some
/// discount happened" would pass against most of the ways this can be wrong.
/// </summary>
public class CartPricerTests
{
    private static readonly DateOnly Today = new(2026, 9, 16);

    private static readonly Guid A = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");
    private static readonly Guid B = Guid.Parse("bbbbbbbb-0000-0000-0000-000000000002");
    private static readonly Guid C = Guid.Parse("cccccccc-0000-0000-0000-000000000003");
    private static readonly Guid D = Guid.Parse("dddddddd-0000-0000-0000-000000000004");

    private static readonly IReadOnlyDictionary<Guid, PricedProduct> Catalogue =
        new Dictionary<Guid, PricedProduct>
        {
            [A] = new(A, "สินค้า A", 100m),
            [B] = new(B, "สินค้า B", 200m),
            [C] = new(C, "สินค้า C", 50m),
            [D] = new(D, "สินค้า D", 50m),
        };

    private static PricedCart Price(
        (Guid Product, int Quantity)[] cart,
        ConditionalPromotion[]? conditional = null,
        Promotion[]? percentage = null,
        bool hasMember = false,
        IReadOnlyDictionary<Guid, PricedProduct>? catalogue = null) =>
        CartPricer.Price(
            cart.Select(item => new CartLine(item.Product, item.Quantity)).ToList(),
            catalogue ?? Catalogue,
            percentage ?? Array.Empty<Promotion>(),
            conditional ?? Array.Empty<ConditionalPromotion>(),
            hasMember,
            Today);

    private static ConditionalPromotion Bundle(
        (Guid Product, int Quantity)[] condition,
        Reward reward,
        bool membersOnly = false,
        DateOnly? start = null,
        DateOnly? end = null,
        string name = "โปรโมชั่นทดสอบ") =>
        new(
            name,
            condition.Select(c => new ConditionLine(c.Product, c.Quantity)),
            reward,
            membersOnly,
            start ?? Today.AddDays(-1),
            end ?? Today.AddDays(30));

    private static PricedLine PaidLine(PricedCart cart, Guid productId) =>
        cart.Lines.Single(line => line.ProductId == productId && !line.IsGift);

    private static PricedLine GiftLine(PricedCart cart, Guid productId) =>
        cart.Lines.Single(line => line.ProductId == productId && line.IsGift);

    // ---------------------------------------------------------------
    // T004 - set counting (FR-010) and determinism (FR-012)
    // ---------------------------------------------------------------

    [Fact]
    public void SetCount_IsTheSmallestWholeMultipleAcrossEveryConditionProduct()
    {
        var promotion = Bundle(
            new[] { (A, 2), (B, 3) },
            Reward.Percentage(10m));

        var cart = Price(new[] { (A, 7), (B, 6) }, new[] { promotion });

        // A allows floor(7/2) = 3 sets, B allows floor(6/3) = 2 - the cart gets 2.
        cart.AppliedPromotions.Single().SetCount.Should().Be(2);
    }

    [Fact]
    public void SetCount_IsZero_WhenAnyConditionProductIsMissing()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Percentage(10m));

        var cart = Price(new[] { (A, 5) }, new[] { promotion });

        cart.AppliedPromotions.Should().BeEmpty();
        cart.DiscountAmount.Should().Be(0m);
    }

    [Fact]
    public void ScanOrder_DoesNotChangeAnyFieldOfTheResult()
    {
        var promotions = new[]
        {
            Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1), name: "ชุดที่หนึ่ง"),
            Bundle(new[] { (B, 2) }, Reward.Percentage(20m), name: "ชุดที่สอง"),
        };

        var forwards = Price(new[] { (A, 3), (B, 4), (C, 2) }, promotions);
        var backwards = Price(new[] { (C, 2), (B, 4), (A, 3) }, promotions);

        backwards.Should().BeEquivalentTo(forwards);
    }

    [Fact]
    public void DuplicateRowsForTheSameProduct_AreFoldedTogetherBeforePricing()
    {
        var promotion = Bundle(new[] { (A, 2) }, Reward.Gift(A, 1));

        var asOneRow = Price(new[] { (A, 6) }, new[] { promotion });
        var asTwoRows = Price(new[] { (A, 3), (A, 3) }, new[] { promotion });

        asTwoRows.Should().BeEquivalentTo(asOneRow);
        asTwoRows.AppliedPromotions.Single().SetCount.Should().Be(2);
    }

    // ---------------------------------------------------------------
    // T038 - User Story 1, all seven acceptance scenarios
    // ---------------------------------------------------------------

    [Fact]
    public void US1_Scenario1_GiftLineNetsToZeroAndTheRestIsFullPrice()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1));

        var cart = Price(new[] { (A, 1), (B, 1), (C, 1) }, new[] { promotion });

        PaidLine(cart, A).LineTotal.Should().Be(100m);
        PaidLine(cart, B).LineTotal.Should().Be(200m);

        var gift = GiftLine(cart, C);
        gift.UnitPrice.Should().Be(50m, "the receipt shows the real price, not 0");
        gift.DiscountAmount.Should().Be(50m);
        gift.LineTotal.Should().Be(0m);

        cart.TotalAmount.Should().Be(300m);
    }

    [Fact]
    public void US1_Scenario2_NoConditionMet_MeansNoGiftAndNoDiscount()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1));

        var cart = Price(new[] { (A, 1) }, new[] { promotion });

        cart.Lines.Should().ContainSingle().Which.IsGift.Should().BeFalse();
        cart.DiscountAmount.Should().Be(0m);
        cart.TotalAmount.Should().Be(100m);
    }

    [Fact]
    public void US1_Scenario3_TwoSetsGiveTwoFreeUnitsAndTheThirdIsPaidFor()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1));

        var cart = Price(new[] { (A, 3), (B, 2), (C, 3) }, new[] { promotion });

        cart.AppliedPromotions.Single().SetCount.Should().Be(2);
        GiftLine(cart, C).Quantity.Should().Be(2);
        PaidLine(cart, C).Quantity.Should().Be(1);
        cart.TotalAmount.Should().Be(100m * 3 + 200m * 2 + 50m);
    }

    [Fact]
    public void US1_Scenario4_MembersOnlyPromotionDoesNotFireWithoutAMember()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1), membersOnly: true);

        var cart = Price(new[] { (A, 1), (B, 1), (C, 1) }, new[] { promotion });

        cart.AppliedPromotions.Should().BeEmpty();
        cart.DiscountAmount.Should().Be(0m);
    }

    [Fact]
    public void US1_Scenario4b_MembersOnlyPromotionFiresWhenAMemberIsAttached()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1), membersOnly: true);

        var cart = Price(new[] { (A, 1), (B, 1), (C, 1) }, new[] { promotion }, hasMember: true);

        GiftLine(cart, C).LineTotal.Should().Be(0m);
    }

    [Fact]
    public void US1_Scenario5_AnExpiredPromotionDoesNotFire()
    {
        var promotion = Bundle(
            new[] { (A, 1), (B, 1) },
            Reward.Gift(C, 1),
            start: Today.AddDays(-10),
            end: Today.AddDays(-1));

        var cart = Price(new[] { (A, 1), (B, 1), (C, 1) }, new[] { promotion });

        cart.AppliedPromotions.Should().BeEmpty();
        cart.DiscountAmount.Should().Be(0m);
    }

    [Fact]
    public void US1_Scenario6_GiftOutOfStockIsReportedAsUnclaimedAndDoesNotBlockTheSale()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1));

        var cart = Price(new[] { (A, 1), (B, 1) }, new[] { promotion });

        cart.AppliedPromotions.Single().SetCount.Should().Be(1);
        cart.UnclaimedGifts.Single().MissingQuantity.Should().Be(1);
        cart.UnclaimedGifts.Single().GiftProductId.Should().Be(C);
        cart.TotalAmount.Should().Be(300m, "the two bought products are still charged in full");
    }

    [Fact]
    public void US1_Scenario7_TheGiftIsNeverAddedToTheCartBySystem()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1));

        var withoutGift = Price(new[] { (A, 1), (B, 1) }, new[] { promotion });
        withoutGift.Lines.Should().NotContain(line => line.ProductId == C);
        withoutGift.UnclaimedGifts.Single().MissingQuantity.Should().Be(1);

        var withGift = Price(new[] { (A, 1), (B, 1), (C, 1) }, new[] { promotion });
        withGift.UnclaimedGifts.Should().BeEmpty();
        GiftLine(withGift, C).LineTotal.Should().Be(0m);
    }

    /// <summary>
    /// FR-013: the promotion applies from the cart contents alone. There is no
    /// confirm flag anywhere in the call - if one were ever added, this test is
    /// the thing that would have to be changed to accommodate it.
    /// </summary>
    [Fact]
    public void US1_EntitlementAppliesImmediately_WithNoConfirmationStep()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 1));

        var cart = Price(new[] { (A, 1), (B, 1), (C, 1) }, new[] { promotion });

        cart.AppliedPromotions.Should().ContainSingle();
    }

    // ---------------------------------------------------------------
    // T041 - FR-014: the pricer never invents cart contents
    // ---------------------------------------------------------------

    [Fact]
    public void ThePricerNeverAddsUnitsThatWereNotScanned()
    {
        var promotions = new[]
        {
            Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(C, 2), name: "แถม C สองชิ้น"),
            Bundle(new[] { (A, 2) }, Reward.Gift(A, 1), name: "ซื้อ A สองแถมหนึ่ง"),
        };

        var submitted = new Dictionary<Guid, int> { [A] = 5, [B] = 2, [C] = 1 };
        var cart = Price(new[] { (A, 5), (B, 2), (C, 1) }, promotions);

        foreach (var group in cart.Lines.GroupBy(line => line.ProductId))
        {
            group.Sum(line => line.Quantity).Should().Be(
                submitted[group.Key],
                "the priced result may split a product across lines but never change how many there are");
        }

        cart.Lines.Select(line => line.ProductId).Should().OnlyContain(id => submitted.ContainsKey(id));
    }

    [Fact]
    public void AProductThatQualifiesAsAGiftButIsNotInTheCart_ProducesAHintNotALine()
    {
        var promotion = Bundle(new[] { (A, 1) }, Reward.Gift(D, 1));

        var cart = Price(new[] { (A, 1) }, new[] { promotion });

        cart.Lines.Should().NotContain(line => line.ProductId == D);
        cart.UnclaimedGifts.Single().GiftProductId.Should().Be(D);
    }

    // ---------------------------------------------------------------
    // T048, T049 - User Story 2, buy y get x of the same product
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(3, 1, 200)] // one set, nothing left over
    [InlineData(5, 1, 400)] // one set (5 / 3), two units at full price
    [InlineData(6, 2, 400)] // two sets exactly
    [InlineData(2, 0, 200)] // not enough for a set
    public void US2_BuyTwoGetOne_ChargesOnlyForTheUnitsThatAreNotFree(
        int scanned, int expectedGifts, decimal expectedTotal)
    {
        var promotion = Bundle(new[] { (A, 2) }, Reward.Gift(A, 1));

        var cart = Price(new[] { (A, scanned) }, new[] { promotion });

        var gifted = cart.Lines.Where(line => line.IsGift).Sum(line => line.Quantity);
        gifted.Should().Be(expectedGifts);
        cart.TotalAmount.Should().Be(expectedTotal);
        cart.Lines.Sum(line => line.Quantity).Should().Be(scanned);
    }

    [Fact]
    public void US2_Scenario5_LeftoverUnitsStillGetTheOldPerItemDiscount()
    {
        var bundle = Bundle(new[] { (A, 2) }, Reward.Gift(A, 1));
        var tenPercentOffA = new Promotion(
            PromotionScope.Item, 10m, A, false, Today.AddDays(-1), Today.AddDays(1));

        var cart = Price(new[] { (A, 5) }, new[] { bundle }, new[] { tenPercentOffA });

        // 3 units went into the set: 2 paid in full, 1 free. The other 2 get 10% off.
        cart.TotalAmount.Should().Be(380m);
        PaidLine(cart, A).Quantity.Should().Be(4);
        PaidLine(cart, A).DiscountAmount.Should().Be(20m);
        GiftLine(cart, A).Quantity.Should().Be(1);
    }

    [Fact]
    public void US2_TheGiftIsAlwaysItsOwnLine_EvenForTheSameProduct()
    {
        var promotion = Bundle(new[] { (A, 2) }, Reward.Gift(A, 1));

        var cart = Price(new[] { (A, 3) }, new[] { promotion });

        cart.Lines.Where(line => line.ProductId == A).Should().HaveCount(2);
        PaidLine(cart, A).Quantity.Should().Be(2);
        PaidLine(cart, A).LineTotal.Should().Be(200m);
        GiftLine(cart, A).UnitPrice.Should().Be(100m);
        GiftLine(cart, A).LineTotal.Should().Be(0m);
    }

    [Fact]
    public void US2_ASameProductGiftIsNeverReportedAsUnclaimed()
    {
        var promotion = Bundle(new[] { (A, 2) }, Reward.Gift(A, 1));

        var cart = Price(new[] { (A, 3) }, new[] { promotion });

        cart.UnclaimedGifts.Should().BeEmpty();
    }

    // ---------------------------------------------------------------
    // T053 - User Story 3, percentage off a bundle
    // ---------------------------------------------------------------

    [Fact]
    public void US3_Scenario1_DiscountsOnlyTheBundledProducts()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Percentage(15m));

        var cart = Price(new[] { (A, 1), (B, 1), (D, 1) }, new[] { promotion });

        cart.DiscountAmount.Should().Be(45m, "15% of 300, and D is not part of the bundle");
        PaidLine(cart, D).LineTotal.Should().Be(50m);
        cart.TotalAmount.Should().Be(305m);
    }

    [Fact]
    public void US3_Scenario2_ALeftoverUnitOutsideTheSetIsFullPrice()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Percentage(15m));

        var cart = Price(new[] { (A, 2), (B, 1) }, new[] { promotion });

        cart.AppliedPromotions.Single().SetCount.Should().Be(1);
        cart.DiscountAmount.Should().Be(45m);
        cart.TotalAmount.Should().Be(400m - 45m);
    }

    [Fact]
    public void US3_Scenario3_TwoSetsDoubleTheDiscount()
    {
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Percentage(15m));

        var cart = Price(new[] { (A, 2), (B, 2) }, new[] { promotion });

        cart.AppliedPromotions.Single().SetCount.Should().Be(2);
        cart.DiscountAmount.Should().Be(90m);
    }

    [Fact]
    public void US3_Scenario4_ABillDiscountStacksOnTopBecauseItIsADifferentTarget()
    {
        var bundle = Bundle(new[] { (A, 1), (B, 1) }, Reward.Percentage(15m));
        var fivePercentOffTheBill = new Promotion(
            PromotionScope.Bill, 5m, null, false, Today.AddDays(-1), Today.AddDays(1));

        var cart = Price(new[] { (A, 1), (B, 1) }, new[] { bundle }, new[] { fivePercentOffTheBill });

        // 45 from the bundle plus 15 (5% of the undiscounted 300) = 240 to pay.
        cart.DiscountAmount.Should().Be(60m);
        cart.TotalAmount.Should().Be(240m);
    }

    // ---------------------------------------------------------------
    // T057 - edge cases from spec.md
    // ---------------------------------------------------------------

    [Fact]
    public void EdgeCase_GiftIsAlsoAConditionProduct_InAMultiProductBundle()
    {
        // "ซื้อ A 1 + B 1 แถม A 1": A costs 1 + 1 = 2 per set, B costs 1.
        var promotion = Bundle(new[] { (A, 1), (B, 1) }, Reward.Gift(A, 1));

        var cart = Price(new[] { (A, 3), (B, 1) }, new[] { promotion });

        cart.AppliedPromotions.Single().SetCount.Should().Be(1);
        PaidLine(cart, A).Quantity.Should().Be(2);
        GiftLine(cart, A).Quantity.Should().Be(1);
        PaidLine(cart, B).Quantity.Should().Be(1);
        cart.TotalAmount.Should().Be(100m * 2 + 200m);
    }

    [Fact]
    public void EdgeCase_AUnitIsNeverCountedByTwoConditionalPromotions()
    {
        var first = Bundle(new[] { (A, 2) }, Reward.Percentage(50m), name: "ลดครึ่งราคา");
        var second = Bundle(new[] { (A, 2) }, Reward.Percentage(10m), name: "ลดสิบ");

        var cart = Price(new[] { (A, 2) }, new[] { first, second });

        cart.AppliedPromotions.Should().ContainSingle()
            .Which.Description.Should().Contain("50%", "the richer promotion wins the units");
        cart.DiscountAmount.Should().Be(100m);
    }

    [Fact]
    public void EdgeCase_ADeletedProductStopsThePromotionAndSuppressesItsHint()
    {
        var promotion = Bundle(new[] { (A, 1) }, Reward.Gift(C, 1));
        var catalogueWithoutC = Catalogue.Where(pair => pair.Key != C)
            .ToDictionary(pair => pair.Key, pair => pair.Value);

        var cart = Price(new[] { (A, 1) }, new[] { promotion }, catalogue: catalogueWithoutC);

        cart.AppliedPromotions.Should().BeEmpty();
        cart.UnclaimedGifts.Should().BeEmpty("the cashier cannot add a product that no longer exists");
        cart.TotalAmount.Should().Be(100m);
    }

    [Fact]
    public void EdgeCase_AHundredPercentBundleNetsToZeroButIsNotAGift()
    {
        var promotion = Bundle(new[] { (A, 1) }, Reward.Percentage(100m));

        var cart = Price(new[] { (A, 1) }, new[] { promotion });

        var line = PaidLine(cart, A);
        line.LineTotal.Should().Be(0m);
        line.IsGift.Should().BeFalse("a 100% discount is not the same thing as a giveaway");
        cart.Lines.Should().NotContain(l => l.IsGift);
    }

    [Fact]
    public void EdgeCase_AGiftAlreadyInTheCartIsFullPriceUntilTheConditionIsMet()
    {
        var promotion = Bundle(new[] { (A, 2) }, Reward.Gift(C, 1));

        var cart = Price(new[] { (A, 1), (C, 1) }, new[] { promotion });

        cart.AppliedPromotions.Should().BeEmpty();
        PaidLine(cart, C).LineTotal.Should().Be(50m);
    }

    // ---------------------------------------------------------------
    // T058 - allocation order (FR-019) is a total order
    // ---------------------------------------------------------------

    [Fact]
    public void Allocation_PrefersTheHigherValuePerSet()
    {
        var cheap = Bundle(new[] { (A, 2) }, Reward.Percentage(10m), name: "ลดสิบ");
        var rich = Bundle(new[] { (A, 2) }, Reward.Percentage(40m), name: "ลดสี่สิบ");

        var cart = Price(new[] { (A, 2) }, new[] { cheap, rich });

        cart.AppliedPromotions.Single().Description.Should().Contain("40%");
    }

    [Fact]
    public void Allocation_BreaksAValueTieByTheOlderStartDate()
    {
        var older = Bundle(
            new[] { (A, 2) }, Reward.Percentage(10m),
            start: Today.AddDays(-10), name: "เก่ากว่า");
        var newer = Bundle(
            new[] { (A, 2) }, Reward.Percentage(10m),
            start: Today.AddDays(-1), name: "ใหม่กว่า");

        var cart = Price(new[] { (A, 2) }, new[] { newer, older });

        cart.AppliedPromotions.Single().Description.Should().Be(older.Describe(NameLookup()));
    }

    [Fact]
    public void Allocation_IsStillDeterministic_WhenValueAndStartDateBothTie()
    {
        var first = Bundle(new[] { (A, 2) }, Reward.Percentage(10m), name: "หนึ่ง");
        var second = Bundle(new[] { (A, 2) }, Reward.Percentage(10m), name: "สอง");
        var expected = first.Id < second.Id ? first : second;

        var forwards = Price(new[] { (A, 2) }, new[] { first, second });
        var backwards = Price(new[] { (A, 2) }, new[] { second, first });

        forwards.AppliedPromotions.Single().PromotionId.Should().Be(expected.Id);
        backwards.AppliedPromotions.Single().PromotionId.Should().Be(expected.Id);
    }

    private static Dictionary<Guid, string> NameLookup() =>
        Catalogue.ToDictionary(pair => pair.Key, pair => pair.Value.Name);
}
