using TaladPOS.Domain.Promotions;

namespace TaladPOS.Application.Promotions;

public interface IPromotionRepository
{
    Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<Promotion>> ListAsync(bool activeOnly, DateOnly today, CancellationToken ct = default);

    /// <summary>Promotions whose [StartDate, EndDate] covers <paramref name="date"/> - used by checkout (FR-021/FR-022).</summary>
    Task<IReadOnlyList<Promotion>> GetActiveOnAsync(DateOnly date, CancellationToken ct = default);

    Task AddAsync(Promotion promotion, CancellationToken ct = default);

    Task UpdateAsync(Promotion promotion, CancellationToken ct = default);

    Task DeleteAsync(Promotion promotion, CancellationToken ct = default);
}
