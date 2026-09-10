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
        var response = await Client.PostAsJsonAsync("/api/auth/login", new { username, password });
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
        var products = await authenticatedClient.GetFromJsonAsync<List<ProductDto>>($"/api/products?search={Uri.EscapeDataString(name)}");
        var match = products!.Single(p => p.Name == name);
        return new ProductSummary(match.Id, match.Name, match.StockQuantity);
    }

    private sealed record LoginResponseDto(string Token, DateTime ExpiresAt, StaffDto Staff);

    private sealed record StaffDto(Guid Id, string Name, string Role);

    private sealed record ProductDto(Guid Id, string Name, int StockQuantity);

    protected sealed record LoginResult(string Token, Guid StaffId, string StaffName, string Role);

    protected sealed record ProductSummary(Guid Id, string Name, int StockQuantity);
}
