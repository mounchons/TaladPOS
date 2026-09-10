using TaladPOS.Application.Common;
using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Sales;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly TaladPOSDbContext _dbContext;

    public SaleRepository(TaladPOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddAsync(Sale sale, CancellationToken ct = default)
    {
        await _dbContext.Sales.AddAsync(sale, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Sales.Include(s => s.LineItems).FirstOrDefaultAsync(s => s.Id == id, ct);

    public async Task<IReadOnlyList<Sale>> SearchAsync(
        DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default)
    {
        var query = _dbContext.Sales.Include(s => s.LineItems).AsQueryable();

        if (from is DateTime fromUtc)
        {
            query = query.Where(s => s.CreatedAtUtc >= fromUtc);
        }

        if (to is DateTime toUtc)
        {
            query = query.Where(s => s.CreatedAtUtc <= toUtc);
        }

        if (staffId is Guid sid)
        {
            query = query.Where(s => s.StaffId == sid);
        }

        if (memberId is Guid mid)
        {
            query = query.Where(s => s.MemberId == mid);
        }

        return await query.OrderByDescending(s => s.CreatedAtUtc).ToListAsync(ct);
    }

    public async Task<PagedResult<Sale>> SearchPagedAsync(
        DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, PageRequest page,
        CancellationToken ct = default)
    {
        var query = BuildSearchQuery(from, to, staffId, memberId);

        // Counted on the filtered query before Include: counting rows does not
        // need the line items, and joining them first would make PostgreSQL
        // count join rows rather than bills.
        var totalCount = await query.CountAsync(ct);

        // CreatedAtUtc alone is not a stable sort - two bills rung up in the
        // same millisecond could swap places between requests, showing one bill
        // twice and hiding another. Id breaks the tie deterministically
        // (contracts/sales.md).
        var items = await query
            .Include(s => s.LineItems)
            .OrderByDescending(s => s.CreatedAtUtc)
            .ThenBy(s => s.Id)
            .Skip(page.Skip)
            .Take(page.Take)
            .ToListAsync(ct);

        return PagedResult<Sale>.From(items, page, totalCount);
    }

    private IQueryable<Sale> BuildSearchQuery(DateTime? from, DateTime? to, Guid? staffId, Guid? memberId)
    {
        var query = _dbContext.Sales.AsQueryable();

        if (from is DateTime fromUtc)
        {
            query = query.Where(s => s.CreatedAtUtc >= fromUtc);
        }

        if (to is DateTime toUtc)
        {
            query = query.Where(s => s.CreatedAtUtc <= toUtc);
        }

        if (staffId is Guid sid)
        {
            query = query.Where(s => s.StaffId == sid);
        }

        if (memberId is Guid mid)
        {
            query = query.Where(s => s.MemberId == mid);
        }

        return query;
    }
}
