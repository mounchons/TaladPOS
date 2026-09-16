using FluentAssertions;
using TaladPOS.Domain.Promotions;
using Xunit;

namespace TaladPOS.Domain.Tests.Promotions;

/// <summary>
/// 003/tasks.md T003 - every rule in the Validation table of
/// 003/contracts/conditional-promotions.md (FR-002 - FR-007).
/// </summary>
public class ConditionalPromotionValidationTests
{
    private static readonly DateOnly Today = new(2026, 9, 16);
    private static readonly Guid ProductA = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductB = Guid.Parse("22222222-2222-2222-2222-222222222222");
    private static readonly Guid ProductC = Guid.Parse("33333333-3333-3333-3333-333333333333");

    private static ConditionalPromotion Build(
        string name = "ซื้อคู่สุดคุ้ม",
        IEnumerable<ConditionLine>? lines = null,
        Reward? reward = null,
        bool membersOnly = false,
        DateOnly? start = null,
        DateOnly? end = null) =>
        new(
            name,
            lines ?? new[] { new ConditionLine(ProductA, 1) },
            reward ?? Reward.Gift(ProductC, 1),
            membersOnly,
            start ?? Today,
            end ?? Today.AddDays(30));

    // FR-002: name is required and capped at 100 characters.

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankName_Throws(string name)
    {
        var act = () => Build(name: name);

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNameLongerThan100Characters_Throws()
    {
        var act = () => Build(name: new string('ก', 101));

        act.Should().Throw<ArgumentException>().WithParameterName("name");
    }

    [Fact]
    public void Constructor_WithNameOfExactly100Characters_IsAccepted()
    {
        var promotion = Build(name: new string('ก', 100));

        promotion.Name.Should().HaveLength(100);
    }

    // FR-003: at least one condition line, quantities >= 1, no duplicate product.

    [Fact]
    public void Constructor_WithNoConditionLines_Throws()
    {
        var act = () => Build(lines: Array.Empty<ConditionLine>());

        act.Should().Throw<ArgumentException>().WithParameterName("conditionLines");
    }

    [Fact]
    public void Constructor_WithTheSameProductTwiceInTheCondition_Throws()
    {
        var act = () => Build(lines: new[]
        {
            new ConditionLine(ProductA, 1),
            new ConditionLine(ProductA, 2),
        });

        act.Should().Throw<ArgumentException>().WithParameterName("conditionLines");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void ConditionLine_WithMinimumQuantityBelowOne_Throws(int quantity)
    {
        var act = () => new ConditionLine(ProductA, quantity);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("minimumQuantity");
    }

    // FR-004: a gift reward needs a product and a quantity of at least 1.

    [Fact]
    public void GiftReward_WithoutAProduct_Throws()
    {
        var act = () => Reward.Gift(Guid.Empty, 1);

        act.Should().Throw<ArgumentException>().WithParameterName("giftProductId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void GiftReward_WithQuantityBelowOne_Throws(int quantity)
    {
        var act = () => Reward.Gift(ProductC, quantity);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("giftQuantity");
    }

    [Fact]
    public void GiftReward_LeavesThePercentageFieldNull()
    {
        var reward = Reward.Gift(ProductC, 2);

        reward.Kind.Should().Be(RewardKind.Gift);
        reward.GiftProductId.Should().Be(ProductC);
        reward.GiftQuantity.Should().Be(2);
        reward.DiscountPercentage.Should().BeNull();
    }

    // FR-005: a percentage reward is in the range (0, 100].

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(100.01)]
    [InlineData(150)]
    public void PercentageReward_OutsideTheAllowedRange_Throws(decimal percentage)
    {
        var act = () => Reward.Percentage(percentage);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("discountPercentage");
    }

    [Theory]
    [InlineData(0.01)]
    [InlineData(15)]
    [InlineData(100)]
    public void PercentageReward_InsideTheAllowedRange_IsAccepted(decimal percentage)
    {
        var reward = Reward.Percentage(percentage);

        reward.Kind.Should().Be(RewardKind.Percentage);
        reward.DiscountPercentage.Should().Be(percentage);
        reward.GiftProductId.Should().BeNull();
        reward.GiftQuantity.Should().BeNull();
    }

    // FR-006: date range and the members-only gate.

    [Fact]
    public void Constructor_WithEndDateBeforeStartDate_Throws()
    {
        var act = () => Build(start: Today, end: Today.AddDays(-1));

        act.Should().Throw<ArgumentException>().WithParameterName("endDate");
    }

    [Fact]
    public void IsActive_IsInclusiveOfBothEnds()
    {
        var promotion = Build(start: Today, end: Today.AddDays(2));

        promotion.IsActive(Today.AddDays(-1)).Should().BeFalse();
        promotion.IsActive(Today).Should().BeTrue();
        promotion.IsActive(Today.AddDays(2)).Should().BeTrue();
        promotion.IsActive(Today.AddDays(3)).Should().BeFalse();
    }

    // FR-007: a rejected edit must not leave the aggregate half-written.

    [Fact]
    public void Update_WhenRejected_LeavesTheExistingValuesUntouched()
    {
        var promotion = Build(name: "เดิม");

        var act = () => promotion.Update(
            "ใหม่",
            Array.Empty<ConditionLine>(),
            Reward.Gift(ProductC, 1),
            false,
            Today,
            Today.AddDays(1));

        act.Should().Throw<ArgumentException>();
        promotion.Name.Should().Be("เดิม");
        promotion.ConditionLines.Should().ContainSingle();
    }

    // FR-023 support: every product the promotion points at.

    [Fact]
    public void ReferencedProductIds_CoversConditionProductsAndTheGift()
    {
        var promotion = Build(
            lines: new[] { new ConditionLine(ProductA, 1), new ConditionLine(ProductB, 1) },
            reward: Reward.Gift(ProductC, 1));

        promotion.ReferencedProductIds.Should().BeEquivalentTo(new[] { ProductA, ProductB, ProductC });
    }

    [Fact]
    public void ReferencedProductIds_DoesNotRepeatAGiftThatIsAlsoAConditionProduct()
    {
        var promotion = Build(
            lines: new[] { new ConditionLine(ProductA, 2) },
            reward: Reward.Gift(ProductA, 1));

        promotion.ReferencedProductIds.Should().BeEquivalentTo(new[] { ProductA });
    }

    // FR-008: the one-line description the promotions screen and receipt show.

    [Fact]
    public void Describe_RendersAGiftBundle()
    {
        var promotion = Build(
            lines: new[] { new ConditionLine(ProductA, 1), new ConditionLine(ProductB, 1) },
            reward: Reward.Gift(ProductC, 1));

        promotion.Describe(new Dictionary<Guid, string>
        {
            [ProductA] = "บะหมี่",
            [ProductB] = "น้ำอัดลม",
            [ProductC] = "ขนม",
        }).Should().Be("ซื้อ บะหมี่ 1 + น้ำอัดลม 1 แถม ขนม 1");
    }

    [Fact]
    public void Describe_RendersABuyManyGetOneFree()
    {
        var promotion = Build(
            lines: new[] { new ConditionLine(ProductA, 2) },
            reward: Reward.Gift(ProductA, 1));

        promotion.Describe(new Dictionary<Guid, string> { [ProductA] = "น้ำส้ม" })
            .Should().Be("ซื้อ น้ำส้ม 2 แถม น้ำส้ม 1");
    }

    [Fact]
    public void Describe_RendersAPercentageBundle()
    {
        var promotion = Build(
            lines: new[] { new ConditionLine(ProductA, 1), new ConditionLine(ProductB, 1) },
            reward: Reward.Percentage(15m));

        promotion.Describe(new Dictionary<Guid, string>
        {
            [ProductA] = "แชมพู",
            [ProductB] = "ครีมนวด",
        }).Should().Be("ซื้อ แชมพู 1 + ครีมนวด 1 ลด 15%");
    }
}
