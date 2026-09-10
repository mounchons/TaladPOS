using TaladPOS.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Api.Controllers;

[ApiController]
[Route("api/v1/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productRepository;
    private readonly CreateProductUseCase _createProductUseCase;
    private readonly UpdateProductUseCase _updateProductUseCase;
    private readonly DeleteProductUseCase _deleteProductUseCase;

    public ProductsController(
        IProductRepository productRepository,
        CreateProductUseCase createProductUseCase,
        UpdateProductUseCase updateProductUseCase,
        DeleteProductUseCase deleteProductUseCase)
    {
        _productRepository = productRepository;
        _createProductUseCase = createProductUseCase;
        _updateProductUseCase = updateProductUseCase;
        _deleteProductUseCase = deleteProductUseCase;
    }

    public record ProductRequestDto(
        string Name, string ImageUrl, decimal Price, string? Barcode, int StockQuantity, int? LowStockThreshold);

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

    /// <summary>contracts/products.md - GET /api/v1/products (FR-001, FR-002, FR-017)</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetProducts(
        [FromQuery] string? search,
        [FromQuery] string? barcode,
        [FromQuery] bool lowStockOnly = false,
        [FromQuery] int? page = null,
        [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        // Rejected rather than clamped (contracts/products.md): a caller that
        // asked for 500 rows and silently got 100 would treat them as all of them.
        if (!PageRequest.TryCreate(page, pageSize, out var pageRequest))
        {
            return BadRequest(new { error = "invalid_pagination" });
        }

        var products = await _productRepository.SearchPagedAsync(search, barcode, lowStockOnly, pageRequest, ct);
        return Ok(products.Map(ToDto));
    }

    /// <summary>contracts/products.md - GET /api/v1/products/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ProductDto>> GetById(Guid id, CancellationToken ct)
    {
        var product = await _productRepository.GetByIdAsync(id, ct);
        return product is null ? NotFound() : Ok(ToDto(product));
    }

    /// <summary>contracts/products.md - POST /api/v1/products (FR-015, FR-029: Manager only)</summary>
    [HttpPost]
    [Authorize(Roles = nameof(StaffRole.Manager))]
    public async Task<ActionResult<ProductDto>> Create(ProductRequestDto request, CancellationToken ct)
    {
        var product = await _createProductUseCase.ExecuteAsync(
            new CreateProductRequest(
                request.Name, request.ImageUrl, request.Price, request.Barcode, request.StockQuantity,
                request.LowStockThreshold),
            ct);

        return CreatedAtAction(nameof(GetById), new { id = product.Id }, ToDto(product));
    }

    /// <summary>contracts/products.md - PUT /api/v1/products/{id} (FR-015, FR-018, FR-029: Manager only)</summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = nameof(StaffRole.Manager))]
    public async Task<ActionResult<ProductDto>> Update(Guid id, ProductRequestDto request, CancellationToken ct)
    {
        var product = await _updateProductUseCase.ExecuteAsync(
            new UpdateProductRequest(
                id, request.Name, request.ImageUrl, request.Price, request.Barcode, request.StockQuantity,
                request.LowStockThreshold),
            ct);

        return Ok(ToDto(product));
    }

    /// <summary>contracts/products.md - DELETE /api/v1/products/{id} (FR-015, FR-029: Manager only)</summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = nameof(StaffRole.Manager))]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _deleteProductUseCase.ExecuteAsync(id, ct);
        return NoContent();
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
