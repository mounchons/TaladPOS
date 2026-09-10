namespace TaladPOS.Application.Promotions;

/// <summary>contracts/promotions.md - DELETE /api/v1/promotions/{id} (Manager only - FR-029)</summary>
public sealed class DeletePromotionUseCase
{
    private readonly IPromotionRepository _promotions;

    public DeletePromotionUseCase(IPromotionRepository promotions) => _promotions = promotions;

    public async Task ExecuteAsync(Guid promotionId, CancellationToken ct = default)
    {
        var promotion = await _promotions.GetByIdAsync(promotionId, ct)
            ?? throw new KeyNotFoundException($"Promotion {promotionId} was not found.");

        await _promotions.DeleteAsync(promotion, ct);
    }
}
