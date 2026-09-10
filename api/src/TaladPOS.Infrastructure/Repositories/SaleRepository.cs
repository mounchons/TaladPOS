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
}
