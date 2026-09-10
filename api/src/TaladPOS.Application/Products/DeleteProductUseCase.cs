namespace TaladPOS.Application.Products;

/// <summary>
/// contracts/products.md - DELETE /api/v1/products/{id} (FR-015). Deleting a
/// product never touches past sales: SaleLineItem stores its own
/// name/price snapshot and only a bare ProductId (no FK), by design.
/// </summary>
public sealed class DeleteProductUseCase
{
    private readonly IProductRepository _products;

    public DeleteProductUseCase(IProductRepository products) => _products = products;

    public async Task ExecuteAsync(Guid productId, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(productId, ct)
            ?? throw new KeyNotFoundException($"Product {productId} was not found.");

        await _products.DeleteAsync(product, ct);
    }
}
