using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Promotions;

namespace TaladPOS.Infrastructure.Configurations;

public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.ToTable("promotions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Scope).IsRequired().HasConversion<string>().HasMaxLength(10);
        builder.Property(p => p.DiscountPercentage).IsRequired().HasColumnType("numeric(5,2)");
        builder.Property(p => p.AppliesToMembersOnly).IsRequired();
        builder.Property(p => p.StartDate).IsRequired().HasColumnType("date");
        builder.Property(p => p.EndDate).IsRequired().HasColumnType("date");
    }
}
