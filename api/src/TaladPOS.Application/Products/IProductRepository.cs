using TaladPOS.Application.Common;
using TaladPOS.Domain.Products;

namespace TaladPOS.Application.Products;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Product>> SearchAsync(
        string? search, string? barcode, bool lowStockOnly, CancellationToken ct = default);

    /// <summary>
    /// contracts/products.md - the paged form of <see cref="SearchAsync"/>.
    ///
    /// Kept separate rather than replacing it: the stock report
    /// (GetStockReportQuery) needs every row, and handing it a page would
    /// silently truncate the report. TotalCount counts the filtered set, so
    /// filtering, counting and slicing all happen in one place.
    /// </summary>
    Task<PagedResult<Product>> SearchPagedAsync(
        string? search, string? barcode, bool lowStockOnly, PageRequest page, CancellationToken ct = default);

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

    /// <summary>
    /// True if another product already has this barcode. Pass
    /// <paramref name="excludeProductId"/> when checking during an update so
    /// a product doesn't collide with its own unchanged barcode.
    /// </summary>
    Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeProductId, CancellationToken ct = default);

    Task AddAsync(Product product, CancellationToken ct = default);

    Task UpdateAsync(Product product, CancellationToken ct = default);

    Task DeleteAsync(Product product, CancellationToken ct = default);
}
