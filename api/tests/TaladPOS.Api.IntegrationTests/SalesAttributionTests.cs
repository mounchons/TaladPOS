using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Infrastructure;
using TaladPOS.Api.Controllers;

namespace TaladPOS.Api.IntegrationTests;

/// <summary>
/// tasks.md T032 - US2: a sale's StaffId must come from the JWT claim of
/// whoever is logged in, never from the request body (FR-008). Proves this
/// by logging in as two different staff accounts and checking the
/// persisted, re-fetched Sale attributes to the one who actually made the
/// authenticated request - not to any id supplied by the client.
/// </summary>
public sealed class SalesAttributionTests : ApiTestBase
{
    public SalesAttributionTests(TaladPOSApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task Checkout_AttributesSaleToLoggedInStaff_NotToRequestBody()
    {
        var cashierLogin = await LoginAsync(CashierUsername, CashierPassword);
        var managerLogin = await LoginAsync(ManagerUsername, ManagerPassword);

        var cashierClient = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var mango = await GetProductByNameAsync(cashierClient, "มะม่วง");

        // Attempt to spoof attribution to the manager via an extraneous
        // staffId-shaped field in the body - the API only ever accepts
        // memberId/lineItems (contracts/sales.md), so this is inert, but it
        // proves the server isn't accidentally binding such a field.
        var createResponse = await cashierClient.PostAsJsonAsync("/api/v1/sales", new
        {
            memberId = (Guid?)null,
            staffId = managerLogin.StaffId,
            lineItems = new[] { new { productId = mango.Id, quantity = 1 } },
        });
        createResponse.EnsureSuccessStatusCode();
        var created = await createResponse.Content.ReadFromJsonAsync<SalesController.SaleDto>();

        created!.Staff!.Id.Should().Be(cashierLogin.StaffId);
        created.Staff.Id.Should().NotBe(managerLogin.StaffId);

        // Re-fetch through a different logged-in session (the manager) to
        // confirm attribution was actually persisted, not just echoed back.
        var managerClient = await CreateAuthenticatedClientAsync(ManagerUsername, ManagerPassword);
        var fetched = await managerClient.GetFromJsonAsync<SalesController.SaleDto>($"/api/v1/sales/{created.Id}");

        fetched!.Staff!.Id.Should().Be(cashierLogin.StaffId);
    }
}
