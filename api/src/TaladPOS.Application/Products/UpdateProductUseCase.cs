using TaladPOS.Domain.Products;

namespace TaladPOS.Application.Products;

public sealed record UpdateProductRequest(
    Guid ProductId, string Name, string ImageUrl, decimal Price, string? Barcode, int StockQuantity,
    int? LowStockThreshold);

/// <summary>contracts/products.md - PUT /api/v1/products/{id} (FR-015, FR-018)</summary>
public sealed class UpdateProductUseCase
{
    private readonly IProductRepository _products;

    public UpdateProductUseCase(IProductRepository products) => _products = products;

    public async Task<Product> ExecuteAsync(UpdateProductRequest request, CancellationToken ct = default)
    {
        var product = await _products.GetByIdAsync(request.ProductId, ct)
            ?? throw new KeyNotFoundException($"Product {request.ProductId} was not found.");

        if (!string.IsNullOrWhiteSpace(request.Barcode)
            && await _products.BarcodeExistsAsync(request.Barcode, request.ProductId, ct))
        {
            throw new DuplicateBarcodeException(request.Barcode);
        }

        product.Rename(request.Name);
        product.SetImageUrl(request.ImageUrl);
        product.SetPrice(request.Price);
        product.SetBarcode(request.Barcode);
        product.SetStockQuantity(request.StockQuantity);
        product.SetLowStockThreshold(request.LowStockThreshold ?? Product.DefaultLowStockThreshold);

        await _products.UpdateAsync(product, ct);
        return product;
    }
}
