using TaladPOS.Application.Common;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TaladPOS.Api.IntegrationTests.Infrastructure;

[Collection(ApiCollection.Name)]
public abstract class ApiTestBase : IAsyncLifetime
{
    protected readonly TaladPOSApiFactory Factory;
    protected readonly HttpClient Client;

    protected ApiTestBase(TaladPOSApiFactory factory)
    {
        Factory = factory;
        Client = factory.CreateClient();
    }

    // quickstart.md step 3 seed data (DevelopmentSeeder): known dev-only test credentials.
    protected const string ManagerUsername = "manager";
    protected const string ManagerPassword = "Manager123!";
    protected const string CashierUsername = "cashier";
    protected const string CashierPassword = "Cashier123!";

    public Task InitializeAsync() => Factory.ResetTransactionalDataAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    protected async Task<LoginResult> LoginAsync(string username, string password)
    {
        var response = await Client.PostAsJsonAsync("/api/v1/auth/login", new { username, password });
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
        return new LoginResult(body!.Token, body.Staff.Id, body.Staff.Name, body.Staff.Role);
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string username, string password)
    {
        var login = await LoginAsync(username, password);
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.Token);
        return client;
    }

    protected async Task<ProductSummary> GetProductByNameAsync(HttpClient authenticatedClient, string name)
    {
        // GET /api/v1/products returns a paged envelope, not a bare array
        // (contracts/products.md). pageSize is explicit rather than left to the
        // default of 20: the dev/test database accumulates products across runs,
        // and a lookup that silently searched only the first page would start
        // failing on a row it can see perfectly well.
        var page = await authenticatedClient.GetFromJsonAsync<PagedResponse<ProductDto>>(
            $"/api/v1/products?search={Uri.EscapeDataString(name)}&pageSize={PageRequest.MaxPageSize}");
        var match = page!.Items.Single(p => p.Name == name);
        return new ProductSummary(match.Id, match.Name, match.StockQuantity);
    }

    /// <summary>Mirrors PagedResult&lt;T&gt; on the wire (contracts/*.md).</summary>
    protected sealed record PagedResponse<T>(
        IReadOnlyList<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

    private sealed record LoginResponseDto(string Token, DateTime ExpiresAt, StaffDto Staff);

    private sealed record StaffDto(Guid Id, string Name, string Role);

    private sealed record ProductDto(Guid Id, string Name, int StockQuantity);

    protected sealed record LoginResult(string Token, Guid StaffId, string StaffName, string Role);

    protected sealed record ProductSummary(Guid Id, string Name, int StockQuantity);
}
