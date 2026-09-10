using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Members;

namespace TaladPOS.Infrastructure.Configurations;

public class MemberConfiguration : IEntityTypeConfiguration<Member>
{
    public void Configure(EntityTypeBuilder<Member> builder)
    {
        builder.ToTable("members");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Name).IsRequired().HasMaxLength(200);
        builder.Property(m => m.PhoneNumber).IsRequired().HasMaxLength(30);
        builder.Property(m => m.AccumulatedPurchaseTotal).IsRequired().HasColumnType("numeric(12,2)");

        builder.HasIndex(m => m.PhoneNumber).IsUnique();
    }
}
