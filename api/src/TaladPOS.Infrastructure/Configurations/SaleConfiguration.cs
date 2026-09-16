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

        // 003/FR-024: which conditional promotions fired, as a snapshot. No
        // foreign key back to the promotion - it may be edited or deleted and
        // the bill still has to explain itself.
        builder.OwnsMany(s => s.AppliedPromotions, promotion =>
        {
            promotion.ToTable("sale_applied_promotions");

            promotion.WithOwner().HasForeignKey(p => p.SaleId);

            promotion.HasKey(p => p.Id);

            promotion.Property(p => p.SaleId).IsRequired();
            promotion.Property(p => p.PromotionId).IsRequired();
            promotion.Property(p => p.DescriptionSnapshot).IsRequired().HasMaxLength(400);
            promotion.Property(p => p.SetCount).IsRequired();
            promotion.Property(p => p.DiscountAmount).IsRequired().HasColumnType("numeric(12,2)");
        });

        builder.Navigation(s => s.AppliedPromotions)
            .HasField("_appliedPromotions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
