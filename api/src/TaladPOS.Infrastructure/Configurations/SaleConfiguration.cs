using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Infrastructure.Configurations;

public class SaleConfiguration : IEntityTypeConfiguration<Sale>
{
    public void Configure(EntityTypeBuilder<Sale> builder)
    {
        builder.ToTable("sales");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.StaffId).IsRequired();
        builder.Property(s => s.MemberId);
        builder.Property(s => s.CreatedAtUtc).IsRequired();

        // Computed from LineItems - not persisted columns.
        builder.Ignore(s => s.SubtotalAmount);
        builder.Ignore(s => s.DiscountAmount);
        builder.Ignore(s => s.TotalAmount);

        builder.HasMany(s => s.LineItems)
            .WithOne()
            .HasForeignKey(li => li.SaleId)
            .OnDelete(DeleteBehavior.Cascade);

        // LineItems exposes a read-only wrapper over the private `_lineItems`
        // field - point EF Core at the backing field directly so it can
        // materialize/track the collection without needing a public setter.
        builder.Navigation(s => s.LineItems)
            .HasField("_lineItems")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
