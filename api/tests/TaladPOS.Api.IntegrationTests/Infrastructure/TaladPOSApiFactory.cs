using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaladPOS.Application.Auth;
using TaladPOS.Infrastructure.Persistence;
using TaladPOS.Infrastructure.Seed;

namespace TaladPOS.Api.IntegrationTests.Infrastructure;

/// <summary>
/// Boots the real API pipeline (quickstart.md's own stack: EF Core against
/// real PostgreSQL, real JWT auth, real controllers) against a dedicated
/// `taladpos_test` database instead of an in-memory provider, because
/// ExecuteUpdateAsync/raw-SQL translation differs between providers
/// (research.md #2) and these tests exist specifically to catch that class
/// of bug.
/// </summary>
public sealed class TaladPOSApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("TEST_DB_CONNECTION")
        ?? "Host=localhost;Port=5432;Database=taladpos_test;Username=taladpos;Password=taladpos_dev_password";

    // Program.cs uses top-level statements and reads Jwt:SigningKey /
    // ConnectionStrings:TaladPOSDb eagerly, BEFORE builder.Build() is
    // called. WebApplicationFactory's ConfigureWebHost/ConfigureAppConfiguration
    // hooks only apply to the deferred host builder captured *at* Build(),
    // so they run too late to affect that eager read - real environment
    // variables (picked up by WebApplicationBuilder.CreateBuilder's default
    // AddEnvironmentVariables()) are the only override point that works
    // here. See Program.cs line ~35.
    static TaladPOSApiFactory()
    {
        Environment.SetEnvironmentVariable("ConnectionStrings__TaladPOSDb", ConnectionString);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TaladPOS");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TaladPOS");
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-only-signing-key-not-a-real-secret-32chars+");
        Environment.SetEnvironmentVariable("Jwt__ExpiryMinutes", "480");
        Environment.SetEnvironmentVariable("Cors__WebAppOrigin", "http://localhost:3000");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
    }

    public async Task InitializeAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaladPOSDbContext>();
        await db.Database.MigrateAsync();
        await SeedAsync(db, scope.ServiceProvider.GetRequiredService<StaffAuthenticator>());
    }

    /// <summary>
    /// Called before every test (constructor-per-test xunit semantics via
    /// IClassFixture/ICollectionFixture) to give each test a clean, fully
    /// stocked dataset without re-running migrations. Staff accounts are
    /// left alone (seeded once) so login credentials stay stable for the
    /// whole run; Products/Sales are wiped and re-seeded so stock-sensitive
    /// tests (checkout, concurrency) never inherit another test's state.
    /// </summary>
    public async Task ResetTransactionalDataAsync()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaladPOSDbContext>();
        await db.Database.ExecuteSqlRawAsync(
            // 002: the conditional promotion tables go too. They reference products
            // by id with no foreign key (003/research.md #7), so a promotion left
            // behind would point at a product this very reset just replaced, and
            // the next test would inherit a promotion flagged unusable.
            "TRUNCATE TABLE sale_applied_promotions, sale_line_items, sales, "
            + "conditional_promotion_lines, conditional_promotions, products CASCADE;");
        await SeedAsync(db, scope.ServiceProvider.GetRequiredService<StaffAuthenticator>());
    }

    private static Task SeedAsync(TaladPOSDbContext db, StaffAuthenticator authenticator) =>
        DevelopmentSeeder.SeedAsync(db, authenticator);

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync().AsTask();
    }
}

[CollectionDefinition(Name)]
public sealed class ApiCollection : ICollectionFixture<TaladPOSApiFactory>
{
    public const string Name = "Api";
}
