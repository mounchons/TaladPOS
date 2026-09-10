using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Infrastructure.Configurations;

public class SaleLineItemConfiguration : IEntityTypeConfiguration<SaleLineItem>
{
    public void Configure(EntityTypeBuilder<SaleLineItem> builder)
    {
        builder.ToTable("sale_line_items");

        builder.HasKey(li => li.Id);

        builder.Property(li => li.SaleId).IsRequired();
        builder.Property(li => li.ProductId).IsRequired();
        builder.Property(li => li.ProductNameSnapshot).IsRequired().HasMaxLength(200);
        builder.Property(li => li.UnitPriceSnapshot).IsRequired().HasColumnType("numeric(12,2)");
        builder.Property(li => li.Quantity).IsRequired();
        builder.Property(li => li.DiscountAmount).IsRequired().HasColumnType("numeric(12,2)");

        builder.Ignore(li => li.LineTotal);
    }
}
