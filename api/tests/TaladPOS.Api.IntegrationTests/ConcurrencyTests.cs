using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaladPOS.Api.Controllers;
using TaladPOS.Api.IntegrationTests.Infrastructure;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Api.IntegrationTests;

/// <summary>
/// tasks.md T075 - the concurrency guarantee from quickstart.md section 5:
/// two registers racing for the same last unit must not both succeed, and
/// stock must never go negative.
///
/// These tests are deliberately run against real PostgreSQL rather than an
/// in-memory provider (see TaladPOSApiFactory): the whole mechanism under
/// test is the atomic conditional UPDATE from research.md #2
/// (<c>WHERE id = @id AND stock_quantity >= @qty</c>, translated by EF Core's
/// ExecuteUpdateAsync). An in-memory provider would satisfy the assertions
/// while proving nothing about the SQL that actually ships.
/// </summary>
public sealed class ConcurrencyTests : ApiTestBase
{
    public ConcurrencyTests(TaladPOSApiFactory factory) : base(factory)
    {
    }

    /// <summary>
    /// quickstart.md section 5 (Concurrency) / FR-016: two simultaneous
    /// POST /api/v1/sales for the last unit in stock.
    ///
    /// The assertions describe the *invariant*, not a particular
    /// interleaving: PostgreSQL serialises the two row updates, so whichever
    /// one reaches the row first wins and the other re-evaluates the
    /// predicate against the committed value and matches 0 rows. Forcing a
    /// precise interleaving with barriers would add flake without adding
    /// signal - "exactly one 201 and exactly one 409" is the property that
    /// must hold either way.
    /// </summary>
    [Fact]
    public async Task TwoSimultaneousCheckouts_ForTheLastUnit_OnlyOneSucceeds()
    {
        var cashierClient = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var mango = await GetProductByNameAsync(cashierClient, "มะม่วง");
        await SetStockQuantityAsync(mango.Id, 1);

        // Two independent clients so neither request can be queued behind the
        // other by HttpClient itself - both hit the in-process server at once.
        var registerOne = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var registerTwo = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);

        var body = new { memberId = (Guid?)null, lineItems = new[] { new { productId = mango.Id, quantity = 1 } } };

        var firstRequest = registerOne.PostAsJsonAsync("/api/v1/sales", body);
        var secondRequest = registerTwo.PostAsJsonAsync("/api/v1/sales", body);
        var responses = await Task.WhenAll(firstRequest, secondRequest);

        var created = responses.Where(r => r.StatusCode == HttpStatusCode.Created).ToList();
        var conflicted = responses.Where(r => r.StatusCode == HttpStatusCode.Conflict).ToList();

        created.Should().HaveCount(1, "exactly one register may claim the last unit");
        conflicted.Should().HaveCount(1, "the losing register must be rejected, not silently oversold");

        var error = await conflicted[0].Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Be("insufficient_stock");
        error.Details!.ProductId.Should().Be(mango.Id);

        // Stock must land on exactly 0 - never -1 (both decrements applied)
        // and never 1 (the winning decrement lost to a rollback).
        var afterSale = await GetProductByNameAsync(cashierClient, "มะม่วง");
        afterSale.StockQuantity.Should().Be(0);

        // FR-016: the rejected checkout must not have left a Sale behind.
        var sales = await cashierClient.GetFromJsonAsync<List<SalesController.SaleDto>>("/api/v1/sales");
        sales.Should().ContainSingle()
            .Which.LineItems.Should().ContainSingle(li => li.ProductId == mango.Id && li.Quantity == 1);
    }

    /// <summary>
    /// FR-016 from the other direction: a checkout that fails partway through
    /// must not leave the earlier lines' stock decremented. This is what the
    /// explicit transaction in CompleteSaleUseCase buys us - ExecuteUpdateAsync
    /// writes immediately and bypasses the change tracker (see IUnitOfWork),
    /// so without that transaction the first line would stay deducted after
    /// the second line throws.
    /// </summary>
    [Fact]
    public async Task Checkout_WhenALaterLineIsOutOfStock_RollsBackEarlierDeductions()
    {
        var cashierClient = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var mango = await GetProductByNameAsync(cashierClient, "มะม่วง");
        var apple = await GetProductByNameAsync(cashierClient, "แอปเปิ้ล");

        var response = await cashierClient.PostAsJsonAsync("/api/v1/sales", new
        {
            memberId = (Guid?)null,
            lineItems = new[]
            {
                new { productId = mango.Id, quantity = 1 },                        // in stock
                new { productId = apple.Id, quantity = apple.StockQuantity + 1 },  // one too many
            },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
        var error = await response.Content.ReadFromJsonAsync<ErrorResponse>();
        error!.Error.Should().Be("insufficient_stock");
        error.Details!.ProductId.Should().Be(apple.Id);

        var mangoAfter = await GetProductByNameAsync(cashierClient, "มะม่วง");
        var appleAfter = await GetProductByNameAsync(cashierClient, "แอปเปิ้ล");
        mangoAfter.StockQuantity.Should().Be(mango.StockQuantity, "the first line's deduction must be rolled back");
        appleAfter.StockQuantity.Should().Be(apple.StockQuantity);

        var sales = await cashierClient.GetFromJsonAsync<List<SalesController.SaleDto>>("/api/v1/sales");
        sales!.Should().BeEmpty("a rejected checkout is never persisted");
    }

    /// <summary>
    /// Drives stock to an exact value straight through the DbContext. Going
    /// through PUT /api/v1/products would work too, but it couples this test to
    /// the product-update contract; the race being tested is about the row's
    /// value, so setting the row directly keeps the arrangement unambiguous.
    /// </summary>
    private async Task SetStockQuantityAsync(Guid productId, int stockQuantity)
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<TaladPOSDbContext>();
        var affected = await db.Products
            .Where(p => p.Id == productId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.StockQuantity, stockQuantity));

        affected.Should().Be(1, "the test arrangement must actually have set the stock it relies on");
    }

    // Shape written by ErrorHandlingMiddleware: {"error":"...","details":{"ProductId":"..."}}.
    private sealed record ErrorResponse(string Error, ErrorDetails? Details);

    private sealed record ErrorDetails(Guid ProductId);
}
