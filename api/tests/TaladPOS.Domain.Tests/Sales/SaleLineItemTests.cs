using FluentAssertions;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Sales;
using Xunit;

namespace TaladPOS.Domain.Tests.Sales;

public class SaleLineItemTests
{
    [Fact]
    public void Snapshot_StaysUnchanged_AfterProductIsLaterEditedOrDeleted()
    {
        var product = new Product("มะม่วง", "https://example.com/mango.jpg", 45m, barcode: null, stockQuantity: 10);
        var lineItem = new SaleLineItem(product.Id, product.Name, product.Price, quantity: 2);

        // Simulate the product being edited (or, conceptually, deleted) after the sale.
        product.Rename("มะม่วงพันธุ์ใหม่");
        product.SetPrice(99m);

        lineItem.ProductNameSnapshot.Should().Be("มะม่วง", "the bill must show the name at the time of sale");
        lineItem.UnitPriceSnapshot.Should().Be(45m, "the bill must show the price at the time of sale");
    }

    [Fact]
    public void LineTotal_SubtractsDiscountFromUnitPriceTimesQuantity()
    {
        var lineItem = new SaleLineItem(Guid.NewGuid(), "มะม่วง", 45m, quantity: 3, discountAmount: 10m);

        lineItem.LineTotal.Should().Be((45m * 3) - 10m);
    }
}
