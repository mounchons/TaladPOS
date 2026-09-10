using FluentAssertions;
using TaladPOS.Domain.Members;
using Xunit;

namespace TaladPOS.Domain.Tests.Members;

/// <summary>
/// tasks.md T047 - US4: AccumulatedPurchaseTotal increases by exactly a
/// bill's TotalAmount (FR-014).
/// </summary>
public class MemberAccumulationTests
{
    [Fact]
    public void IncreaseAccumulatedPurchaseTotal_AddsExactlyTheGivenAmount()
    {
        var member = new Member("สมชาย ใจดี", "0812345678");

        member.IncreaseAccumulatedPurchaseTotal(81.00m);

        member.AccumulatedPurchaseTotal.Should().Be(81.00m);
    }

    [Fact]
    public void IncreaseAccumulatedPurchaseTotal_AppliedTwice_AccumulatesAcrossBills()
    {
        var member = new Member("สมชาย ใจดี", "0812345678");

        member.IncreaseAccumulatedPurchaseTotal(81.00m);
        member.IncreaseAccumulatedPurchaseTotal(45.00m);

        member.AccumulatedPurchaseTotal.Should().Be(126.00m);
    }

    [Fact]
    public void IncreaseAccumulatedPurchaseTotal_WithNegativeAmount_Throws()
    {
        var member = new Member("สมชาย ใจดี", "0812345678");

        var act = () => member.IncreaseAccumulatedPurchaseTotal(-1m);

        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("amount");
        member.AccumulatedPurchaseTotal.Should().Be(0m);
    }
}
