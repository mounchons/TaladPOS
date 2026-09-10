using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Products;

namespace TaladPOS.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;

    public ProductsController(IProductRepository productRepository)
    {
        _productRepository = productRepository;
    }

    public record ProductDto(
        Guid Id,
        string Name,
        string ImageUrl,
        decimal Price,
        string? Barcode,
        int StockQuantity,
        int LowStockThreshold,
        bool IsLowStock,
        bool IsOutOfStock);

    /// <summary>contracts/products.md - GET /api/products (FR-001, FR-002, FR-017)</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? barcode,
        [FromQuery] bool lowStockOnly = false,
        CancellationToken ct = default)
    {
        var products = await _productRepository.SearchAsync(search, barcode, lowStockOnly, ct);
        return Ok(products.Select(ToDto).ToList());
    }

    /// <summary>contracts/products.md - GET /api/products/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(ToDto(product));
    }

    private static ProductDto ToDto(Product product) => new(
        product.Id,
        product.Name,
        product.ImageUrl,
        product.Price,
        product.Barcode,
        product.StockQuantity,
        product.LowStockThreshold,
        product.IsLowStock,
        product.IsOutOfStock);
}
