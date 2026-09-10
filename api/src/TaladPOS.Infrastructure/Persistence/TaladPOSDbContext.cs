using Microsoft.EntityFrameworkCore;
using TaladPOS.Domain.Products;
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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TaladPOSDbContext).Assembly);
    }
}
