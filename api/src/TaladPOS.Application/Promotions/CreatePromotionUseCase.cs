using TaladPOS.Domain.Promotions;

namespace TaladPOS.Application.Promotions;

public sealed record CreatePromotionRequest(
    PromotionScope Scope,
    decimal DiscountPercentage,
    Guid? ProductId,
    bool AppliesToMembersOnly,
    DateOnly StartDate,
    DateOnly EndDate);

/// <summary>contracts/promotions.md - POST /api/promotions (FR-019-FR-021, Manager only - FR-029)</summary>
public sealed class CreatePromotionUseCase
{
    private readonly IPromotionRepository _promotions;

    public CreatePromotionUseCase(IPromotionRepository promotions) => _promotions = promotions;

    public async Task<Promotion> ExecuteAsync(CreatePromotionRequest request, CancellationToken ct = default)
    {
        var promotion = new Promotion(
            request.Scope, request.DiscountPercentage, request.ProductId, request.AppliesToMembersOnly,
            request.StartDate, request.EndDate);

        await _promotions.AddAsync(promotion, ct);
        return promotion;
    }
}
