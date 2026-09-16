using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Promotions;

namespace TaladPOS.Infrastructure.Configurations;

/// <summary>
/// 003/data-model.md sections 1-3. Its own tables, untouched by and untouching
/// the existing `promotions` table (003/FR-026).
/// </summary>
public class ConditionalPromotionConfiguration : IEntityTypeConfiguration<ConditionalPromotion>
{
    public void Configure(EntityTypeBuilder<ConditionalPromotion> builder)
    {
        builder.ToTable("conditional_promotions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name).IsRequired().HasMaxLength(ConditionalPromotion.MaxNameLength);
        builder.Property(p => p.AppliesToMembersOnly).IsRequired();
        builder.Property(p => p.StartDate).IsRequired().HasColumnType("date");
        builder.Property(p => p.EndDate).IsRequired().HasColumnType("date");

        // Derived from the condition lines and the reward - never a column.
        builder.Ignore(p => p.ReferencedProductIds);

        // The reward flattens into this table: one row always carries exactly
        // one of the two shapes, so a second table would buy nothing.
        builder.OwnsOne(p => p.Reward, reward =>
        {
            reward.Property(r => r.Kind).IsRequired().HasConversion<string>().HasMaxLength(10);
            reward.Property(r => r.GiftProductId);
            reward.Property(r => r.GiftQuantity);
            reward.Property(r => r.DiscountPercentage).HasColumnType("numeric(5,2)");
        });

        builder.Navigation(p => p.Reward).IsRequired();

        builder.OwnsMany(p => p.ConditionLines, line =>
        {
            line.ToTable("conditional_promotion_lines");

            line.WithOwner().HasForeignKey("ConditionalPromotionId");

            line.HasKey(l => l.Id);

            line.Property(l => l.Id).ValueGeneratedNever();
            line.Property(l => l.ProductId).IsRequired();
            line.Property(l => l.MinimumQuantity).IsRequired();
            line.Property(l => l.SortOrder).IsRequired();

            // 003/FR-003 enforced by the database as well as by the aggregate.
            // A unique index rather than a composite primary key: an edit
            // replaces the whole condition list, and a natural key would make
            // EF Core treat a retained product as a delete plus an add sharing
            // one key, which it refuses to track.
            line.HasIndex("ConditionalPromotionId", nameof(ConditionLine.ProductId)).IsUnique();
        });

        builder.Navigation(p => p.ConditionLines)
            .HasField("_conditionLines")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
