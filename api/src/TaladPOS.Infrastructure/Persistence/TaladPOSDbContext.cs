using Microsoft.EntityFrameworkCore;
using TaladPOS.Domain.Members;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Promotions;
using TaladPOS.Domain.Sales;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Infrastructure.Persistence;

public class TaladPOSDbContext : DbContext
{
    public TaladPOSDbContext(DbContextOptions<TaladPOSDbContext> options)
        : base(options)
    {
    }

    public DbSet<Staff> Staff => Set<Staff>();

    public DbSet<Product> Products => Set<Product>();

    public DbSet<Sale> Sales => Set<Sale>();

    public DbSet<SaleLineItem> SaleLineItems => Set<SaleLineItem>();

    public DbSet<Member> Members => Set<Member>();

    public DbSet<Promotion> Promotions => Set<Promotion>();

    public DbSet<ConditionalPromotion> ConditionalPromotions => Set<ConditionalPromotion>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaladPOSDbContext).Assembly);
    }
}
