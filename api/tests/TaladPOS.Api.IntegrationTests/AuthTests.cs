using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using TaladPOS.Api.IntegrationTests.Infrastructure;

namespace TaladPOS.Api.IntegrationTests;

/// <summary>tasks.md T031 - US2: an unauthenticated checkout request must be rejected.</summary>
public sealed class AuthTests : ApiTestBase
{
    public AuthTests(TaladPOSApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task PostSales_WithoutAuthorizationHeader_Returns401()
    {
        var mango = await GetProductByNameAsync(await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword), "มะม่วง");

        var response = await Client.PostAsJsonAsync("/api/v1/sales", new
        {
            memberId = (Guid?)null,
            lineItems = new[] { new { productId = mango.Id, quantity = 1 } },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostSales_WithValidToken_Returns201()
    {
        var authClient = await CreateAuthenticatedClientAsync(CashierUsername, CashierPassword);
        var mango = await GetProductByNameAsync(authClient, "มะม่วง");

        var response = await authClient.PostAsJsonAsync("/api/v1/sales", new
        {
            memberId = (Guid?)null,
            lineItems = new[] { new { productId = mango.Id, quantity = 1 } },
        });

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }
}
