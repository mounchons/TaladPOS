using Microsoft.EntityFrameworkCore;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Promotions;
using TaladPOS.Infrastructure.Persistence;

namespace TaladPOS.Infrastructure.Repositories;

public class ConditionalPromotionRepository : IConditionalPromotionRepository
{
    private readonly TaladPOSDbContext _dbContext;

    public ConditionalPromotionRepository(TaladPOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    // A promotion without its condition lines cannot price anything, so every
    // read pulls them along - there is no caller that wants the bare row.
    private IQueryable<ConditionalPromotion> WithLines() =>
        _dbContext.ConditionalPromotions.Include(p => p.ConditionLines);

    public Task<ConditionalPromotion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        WithLines().FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task<IReadOnlyList<ConditionalPromotion>> ListAsync(
        bool activeOnly, DateOnly today, CancellationToken ct = default)
    {
        var query = WithLines();

        if (activeOnly)
        {
            query = query.Where(p => p.StartDate <= today && today <= p.EndDate);
        }

        return await query.OrderBy(p => p.StartDate).ThenBy(p => p.Id).ToListAsync(ct);
    }

    public async Task<IReadOnlyList<ConditionalPromotion>> GetActiveOnAsync(
        DateOnly date, CancellationToken ct = default) =>
        await WithLines()
            .Where(p => p.StartDate <= date && date <= p.EndDate)
            .ToListAsync(ct);

    public async Task AddAsync(ConditionalPromotion promotion, CancellationToken ct = default)
    {
        await _dbContext.ConditionalPromotions.AddAsync(promotion, ct);
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(ConditionalPromotion promotion, CancellationToken ct = default)
    {
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(ConditionalPromotion promotion, CancellationToken ct = default)
    {
        _dbContext.ConditionalPromotions.Remove(promotion);
        await _dbContext.SaveChangesAsync(ct);
    }
}
