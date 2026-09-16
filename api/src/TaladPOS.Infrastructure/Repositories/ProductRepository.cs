using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Common;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Products;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly TaladPOSDbContext _dbContext;

    public ProductRepository(TaladPOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Products.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Product>> GetByIdsAsync(
        IEnumerable<Guid> ids, CancellationToken ct = default)
    {
        var wanted = ids.Distinct().ToList();
        if (wanted.Count == 0)
        {
            return Array.Empty<Product>();
        }

        return await _dbContext.Products
            .Where(p => wanted.Contains(p.Id))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Product>> SearchAsync(
        string? search, string? barcode, bool lowStockOnly, CancellationToken ct = default)
    {
        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            query = query.Where(p => p.Barcode == barcode);
        }

        var products = await query.ToListAsync(ct);

        // IsLowStock is a computed property (not a mapped column), so this
        // filter must run in-memory after the database query.
        return lowStockOnly ? products.Where(p => p.IsLowStock).ToList() : products;
    }

    public async Task<PagedResult<Product>> SearchPagedAsync(
        string? search, string? barcode, bool lowStockOnly, PageRequest page, CancellationToken ct = default)
    {
        var query = BuildSearchQuery(search, barcode);

        if (lowStockOnly)
        {
            // The unpaged SearchAsync filters this in memory because IsLowStock
            // is a computed property. That is not an option here: filtering
            // after the slice would page the unfiltered set and report a
            // TotalCount for rows the caller never asked for. The predicate is
            // written out so PostgreSQL does the filter, the count and the
            // slice together - it must stay in step with Product.IsLowStock.
            query = query.Where(p => p.StockQuantity > 0 && p.StockQuantity <= p.LowStockThreshold);
        }

        var totalCount = await query.CountAsync(ct);

        // Ordered before paging: without a deterministic sort PostgreSQL may
        // return rows in any order, so the same product could appear on two
        // pages while another appears on none. Name, then Id to break ties.
        var items = await query
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(ct);

        return PagedResult<Product>.From(items, page, totalCount);
    }

    private IQueryable<Product> BuildSearchQuery(string? search, string? barcode)
    {
        var query = _dbContext.Products.AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(p => EF.Functions.ILike(p.Name, $"%{search}%"));
        }

        if (!string.IsNullOrWhiteSpace(barcode))
        {
            query = query.Where(p => p.Barcode == barcode);
        }

        return query;
    }

    public async Task<bool> TryDecreaseStockAsync(Guid productId, int quantity, CancellationToken ct = default)
    {
        var affectedRows = await _dbContext.Products
            .Where(p => p.Id == productId && p.StockQuantity >= quantity)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(p => p.StockQuantity, p => p.StockQuantity - quantity), ct);

        return affectedRows > 0;
    }

    public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeProductId, CancellationToken ct = default) =>
        _dbContext.Products.AnyAsync(
            p => p.Barcode == barcode && (excludeProductId == null || p.Id != excludeProductId), ct);

    public async Task AddAsync(Product product, CancellationToken ct = default)
    {
        await _dbContext.Products.AddAsync(product, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Product product, CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Product product, CancellationToken ct = default)
    {
        _dbContext.Products.Remove(product);
        await _dbContext.SaveChangesAsync(ct);
    }
}
