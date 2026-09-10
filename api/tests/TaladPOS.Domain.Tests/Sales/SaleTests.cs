using FluentAssertions;
using TaladPOS.Domain.Sales;
using Xunit;

namespace TaladPOS.Domain.Tests.Sales;

public class SaleTests
{
    [Fact]
    public void Constructor_WithNoLineItems_ThrowsArgumentException()
    {
        var act = () => new Sale(Guid.NewGuid(), memberId: null, lineItems: Enumerable.Empty<SaleLineItem>());

        act.Should().Throw<ArgumentException>("the sale-screen edge case forbids checking out an empty cart");
    }

    [Fact]
    public void Constructor_WithLineItems_ComputesSubtotalDiscountAndTotal()
    {
        var lineItems = new[]
        {
            new SaleLineItem(Guid.NewGuid(), "มะม่วง", 45m, 2, discountAmount: 5m),
            new SaleLineItem(Guid.NewGuid(), "แอปเปิ้ล", 60m, 1),
        };

        var sale = new Sale(Guid.NewGuid(), memberId: null, lineItems);

        sale.SubtotalAmount.Should().Be((45m * 2) + (60m * 1));
        sale.DiscountAmount.Should().Be(5m);
        sale.TotalAmount.Should().Be(sale.SubtotalAmount - sale.DiscountAmount);
        sale.LineItems.Should().OnlyContain(item => item.SaleId == sale.Id);
    }
}
