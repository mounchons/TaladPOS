using TaladPOS.Domain.Promotions;

namespace TaladPOS.Application.Promotions;

public sealed record UpdatePromotionRequest(
    Guid PromotionId,
    PromotionScope Scope,
    decimal DiscountPercentage,
    Guid? ProductId,
    bool AppliesToMembersOnly,
    DateOnly StartDate,
    DateOnly EndDate);

/// <summary>contracts/promotions.md - PUT /api/v1/promotions/{id} (Manager only - FR-029)</summary>
public sealed class UpdatePromotionUseCase
{
    private readonly IPromotionRepository _promotions;

    public UpdatePromotionUseCase(IPromotionRepository promotions) => _promotions = promotions;

    public async Task<Promotion> ExecuteAsync(UpdatePromotionRequest request, CancellationToken ct = default)
    {
        var promotion = await _promotions.GetByIdAsync(request.PromotionId, ct)
            ?? throw new KeyNotFoundException($"Promotion {request.PromotionId} was not found.");

        promotion.Update(
            request.Scope, request.DiscountPercentage, request.ProductId, request.AppliesToMembersOnly,
            request.StartDate, request.EndDate);

        await _promotions.UpdateAsync(promotion, ct);
        return promotion;
    }
}
