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
}
