using TaladPOS.Domain.Products;

namespace TaladPOS.Application.Products;

public sealed record CreateProductRequest(
    string Name, string ImageUrl, decimal Price, string? Barcode, int StockQuantity, int? LowStockThreshold);

/// <summary>contracts/products.md - POST /api/v1/products (FR-015)</summary>
public sealed class CreateProductUseCase
{
    private readonly IProductRepository _products;

    public CreateProductUseCase(IProductRepository products) => _products = products;

    public async Task<Product> ExecuteAsync(CreateProductRequest request, CancellationToken ct = default)
    {
        if (!string.IsNullOrWhiteSpace(request.Barcode)
            && await _products.BarcodeExistsAsync(request.Barcode, excludeProductId: null, ct))
        {
            throw new DuplicateBarcodeException(request.Barcode);
        }

        var product = new Product(
            request.Name, request.ImageUrl, request.Price, request.Barcode, request.StockQuantity,
            request.LowStockThreshold);

        await _products.AddAsync(product, ct);
        return product;
    }
}
