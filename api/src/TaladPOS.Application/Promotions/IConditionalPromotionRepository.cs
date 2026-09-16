using TaladPOS.Domain.Promotions;

namespace TaladPOS.Application.Promotions;

public interface IConditionalPromotionRepository
{
    Task<ConditionalPromotion?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<IReadOnlyList<ConditionalPromotion>> ListAsync(
        bool activeOnly, DateOnly today, CancellationToken ct = default);

    /// <summary>Promotions whose [StartDate, EndDate] covers <paramref name="date"/> - used when pricing a cart.</summary>
    Task<IReadOnlyList<ConditionalPromotion>> GetActiveOnAsync(DateOnly date, CancellationToken ct = default);

    Task AddAsync(ConditionalPromotion promotion, CancellationToken ct = default);

    Task UpdateAsync(ConditionalPromotion promotion, CancellationToken ct = default);

    Task DeleteAsync(ConditionalPromotion promotion, CancellationToken ct = default);
}
