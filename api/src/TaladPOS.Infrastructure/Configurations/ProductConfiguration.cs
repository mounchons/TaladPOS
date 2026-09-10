using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Products;

namespace TaladPOS.Infrastructure.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(200);
        builder.Property(p => p.ImageUrl).IsRequired();
        builder.Property(p => p.Price).IsRequired().HasColumnType("numeric(12,2)");
        builder.Property(p => p.Barcode).HasMaxLength(64);
        builder.Property(p => p.StockQuantity).IsRequired();
        builder.Property(p => p.LowStockThreshold).IsRequired();

        builder.Ignore(p => p.IsOutOfStock);
        builder.Ignore(p => p.IsLowStock);

        builder.HasIndex(p => p.Barcode).IsUnique().HasFilter("\"Barcode\" IS NOT NULL");
    }
}
