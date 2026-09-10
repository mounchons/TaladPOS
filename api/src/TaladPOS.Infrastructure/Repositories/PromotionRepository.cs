using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Promotions;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Infrastructure.Repositories;

public class PromotionRepository : IPromotionRepository
{
    private readonly TaladPOSDbContext _dbContext;

    public PromotionRepository(TaladPOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _dbContext.Promotions.FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<Promotion>> ListAsync(bool activeOnly, DateOnly today, CancellationToken ct = default)
    {
        var query = _dbContext.Promotions.AsQueryable();

        if (activeOnly)
        {
            query = query.Where(p => p.StartDate <= today && today <= p.EndDate);
        }

        return await query.ToListAsync(ct);
    }

    public async Task<IReadOnlyList<Promotion>> GetActiveOnAsync(DateOnly date, CancellationToken ct = default) =>
        await _dbContext.Promotions
            .Where(p => p.StartDate <= date && date <= p.EndDate)
            .ToListAsync(ct);

    public async Task AddAsync(Promotion promotion, CancellationToken ct = default)
    {
        await _dbContext.Promotions.AddAsync(promotion, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(Promotion promotion, CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(Promotion promotion, CancellationToken ct = default)
    {
        _dbContext.Promotions.Remove(promotion);
        await _dbContext.SaveChangesAsync(ct);
    }
}
