using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Auth;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Staff;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Infrastructure.Seed;

/// <summary>
/// Development-only seed data (quickstart.md step 3): one Manager, one
/// Cashier, and two sample products. Never runs against a non-empty
/// database, so it is safe to call unconditionally at startup.
/// </summary>
public static class DevelopmentSeeder
{
    public static async Task SeedAsync(TaladPOSDbContext dbContext, StaffAuthenticator authenticator)
    {
        if (!await dbContext.Staff.AnyAsync())
        {
            dbContext.Staff.AddRange(
                new Staff("ผู้จัดการร้าน", "manager", authenticator.HashPassword("Manager123!"), StaffRole.Manager),
                new Staff("แคชเชียร์", "cashier", authenticator.HashPassword("Cashier123!"), StaffRole.Cashier));
        }

        if (!await dbContext.Products.AnyAsync())
        {
            dbContext.Products.AddRange(
                new Product("มะม่วง", "https://placehold.co/200x200?text=Mango", 45m, "8850000000012", 10),
                new Product("แอปเปิ้ล", "https://placehold.co/200x200?text=Apple", 60m, null, 3));
        }

        await dbContext.SaveChangesAsync();
    }
}
