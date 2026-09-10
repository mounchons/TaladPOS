using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Infrastructure.Configurations;

public class StaffConfiguration : IEntityTypeConfiguration<Staff>
{
    public void Configure(EntityTypeBuilder<Staff> builder)
    {
        builder.ToTable("staff");

        builder.HasKey(s => s.Id);

        builder.Property(s => s.Name).IsRequired().HasMaxLength(200);
        builder.Property(s => s.Username).IsRequired().HasMaxLength(100);
        builder.Property(s => s.PasswordHash).IsRequired();
        builder.Property(s => s.Role).IsRequired().HasConversion<string>().HasMaxLength(20);

        builder.HasIndex(s => s.Username).IsUnique();
    }
}
