using TaladPOS.Domain.Products;

namespace TaladPOS.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Product>> SearchAsync(
        string? search, string? barcode, bool lowStockOnly, CancellationToken ct = default);

    /// <summary>
    /// Atomic conditional decrement (research.md #2): issues
    /// <c>UPDATE products SET stock_quantity = stock_quantity - @qty WHERE
    /// id = @id AND stock_quantity &gt;= @qty</c> directly against the
    /// database. Returns false (no rows affected) when stock is
    /// insufficient - including when a concurrent sale from another
    /// register already consumed it (FR-016, the multi-register clarify
    /// answer).
    /// </summary>
    Task<bool> TryDecreaseStockAsync(Guid productId, int quantity, CancellationToken ct = default);
}
