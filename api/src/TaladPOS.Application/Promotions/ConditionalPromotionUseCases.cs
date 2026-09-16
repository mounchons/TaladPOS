using TaladPOS.Application.Products;
using TaladPOS.Domain.Promotions;

namespace TaladPOS.Application.Promotions;

/// <summary>One row of the buy condition as it arrives from the API.</summary>
public sealed record ConditionLineRequest(Guid ProductId, int MinimumQuantity);

/// <summary>
/// The reward, still in its wire shape: exactly one of the two sides is
/// filled in and the use case turns it into a validated <see cref="Reward"/>.
/// </summary>
public sealed record RewardRequest(
    RewardKind Kind, Guid? GiftProductId, int? GiftQuantity, decimal? DiscountPercentage);

public sealed record ConditionalPromotionRequest(
    string Name,
    IReadOnlyList<ConditionLineRequest> ConditionLines,
    RewardRequest Reward,
    bool AppliesToMembersOnly,
    DateOnly StartDate,
    DateOnly EndDate);

/// <summary>
/// Raised when a request breaks one of the rules in the Validation table of
/// 003/contracts/conditional-promotions.md. Carries the contract's error code
/// so the controller does not have to pattern-match on messages.
/// </summary>
public sealed class ConditionalPromotionValidationException : Exception
{
    public ConditionalPromotionValidationException(string errorCode, string? message = null)
        : base(message ?? errorCode)
    {
        ErrorCode = errorCode;
    }

    public string ErrorCode { get; }
}

/// <summary>
/// Turns a wire request into the aggregate, rejecting anything the contract
/// forbids before a single row is written (003/FR-007).
/// </summary>
public static class ConditionalPromotionFactory
{
    public static Reward BuildReward(RewardRequest request)
    {
        if (request is null)
        {
            throw new ConditionalPromotionValidationException("reward_required");
        }

        if (request.Kind == RewardKind.Gift)
        {
            if (request.DiscountPercentage is not null)
            {
                throw new ConditionalPromotionValidationException("reward_fields_mismatch");
            }

            if (request.GiftProductId is not Guid giftProductId || giftProductId == Guid.Empty
                || request.GiftQuantity is not int giftQuantity)
            {
                throw new ConditionalPromotionValidationException("gift_reward_incomplete");
            }

            if (giftQuantity < 1)
            {
                throw new ConditionalPromotionValidationException("gift_reward_incomplete");
            }

            return Reward.Gift(giftProductId, giftQuantity);
        }

        if (request.GiftProductId is not null || request.GiftQuantity is not null)
        {
            throw new ConditionalPromotionValidationException("reward_fields_mismatch");
        }

        if (request.DiscountPercentage is not decimal percentage || percentage <= 0 || percentage > 100)
        {
            throw new ConditionalPromotionValidationException("invalid_discount_percentage");
        }

        return Reward.Percentage(percentage);
    }

    public static IReadOnlyList<ConditionLine> BuildConditionLines(
        IReadOnlyList<ConditionLineRequest>? requests)
    {
        if (requests is null || requests.Count == 0)
        {
            throw new ConditionalPromotionValidationException("condition_lines_required");
        }

        if (requests.Select(line => line.ProductId).Distinct().Count() != requests.Count)
        {
            throw new ConditionalPromotionValidationException("duplicate_condition_product");
        }

        if (requests.Any(line => line.MinimumQuantity < 1))
        {
            throw new ConditionalPromotionValidationException("invalid_minimum_quantity");
        }

        return requests.Select(line => new ConditionLine(line.ProductId, line.MinimumQuantity)).ToList();
    }

    public static void ValidateHeader(ConditionalPromotionRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ConditionalPromotionValidationException("name_required");
        }

        if (request.Name.Length > ConditionalPromotion.MaxNameLength)
        {
            throw new ConditionalPromotionValidationException("name_too_long");
        }

        if (request.EndDate < request.StartDate)
        {
            throw new ConditionalPromotionValidationException("invalid_date_range");
        }
    }
}

/// <summary>contracts/conditional-promotions.md - POST (003/FR-001 - FR-007, Manager only).</summary>
public sealed class CreateConditionalPromotionUseCase
{
    private readonly IConditionalPromotionRepository _promotions;
    private readonly IProductRepository _products;

    public CreateConditionalPromotionUseCase(
        IConditionalPromotionRepository promotions, IProductRepository products)
    {
        _promotions = promotions;
        _products = products;
    }

    public async Task<ConditionalPromotion> ExecuteAsync(
        ConditionalPromotionRequest request, CancellationToken ct = default)
    {
        var promotion = await ConditionalPromotionBuilder.BuildAsync(request, _products, ct);
        await _promotions.AddAsync(promotion, ct);
        return promotion;
    }
}

/// <summary>contracts/conditional-promotions.md - PUT. Condition lines are replaced wholesale.</summary>
public sealed class UpdateConditionalPromotionUseCase
{
    private readonly IConditionalPromotionRepository _promotions;
    private readonly IProductRepository _products;

    public UpdateConditionalPromotionUseCase(
        IConditionalPromotionRepository promotions, IProductRepository products)
    {
        _promotions = promotions;
        _products = products;
    }

    public async Task<ConditionalPromotion> ExecuteAsync(
        Guid id, ConditionalPromotionRequest request, CancellationToken ct = default)
    {
        var promotion = await _promotions.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Conditional promotion {id} was not found.");

        ConditionalPromotionFactory.ValidateHeader(request);
        var lines = ConditionalPromotionFactory.BuildConditionLines(request.ConditionLines);
        var reward = ConditionalPromotionFactory.BuildReward(request.Reward);
        await ConditionalPromotionBuilder.EnsureProductsExistAsync(lines, reward, _products, ct);

        promotion.Update(
            request.Name, lines, reward, request.AppliesToMembersOnly, request.StartDate, request.EndDate);

        await _promotions.UpdateAsync(promotion, ct);
        return promotion;
    }
}

/// <summary>contracts/conditional-promotions.md - DELETE.</summary>
public sealed class DeleteConditionalPromotionUseCase
{
    private readonly IConditionalPromotionRepository _promotions;

    public DeleteConditionalPromotionUseCase(IConditionalPromotionRepository promotions) =>
        _promotions = promotions;

    public async Task ExecuteAsync(Guid id, CancellationToken ct = default)
    {
        var promotion = await _promotions.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Conditional promotion {id} was not found.");

        await _promotions.DeleteAsync(promotion, ct);
    }
}

internal static class ConditionalPromotionBuilder
{
    internal static async Task<ConditionalPromotion> BuildAsync(
        ConditionalPromotionRequest request, IProductRepository products, CancellationToken ct)
    {
        ConditionalPromotionFactory.ValidateHeader(request);
        var lines = ConditionalPromotionFactory.BuildConditionLines(request.ConditionLines);
        var reward = ConditionalPromotionFactory.BuildReward(request.Reward);
        await EnsureProductsExistAsync(lines, reward, products, ct);

        return new ConditionalPromotion(
            request.Name, lines, reward, request.AppliesToMembersOnly, request.StartDate, request.EndDate);
    }

    /// <summary>
    /// 003/FR-007: a promotion may not be created pointing at a product that
    /// does not exist. There is no foreign key doing this for us, on purpose
    /// (003/research.md #7).
    /// </summary>
    internal static async Task EnsureProductsExistAsync(
        IReadOnlyList<ConditionLine> lines,
        Reward reward,
        IProductRepository products,
        CancellationToken ct)
    {
        var referenced = lines.Select(line => line.ProductId).ToList();
        if (reward.Kind == RewardKind.Gift)
        {
            referenced.Add(reward.GiftProductId!.Value);
        }

        foreach (var productId in referenced.Distinct())
        {
            if (await products.GetByIdAsync(productId, ct) is null)
            {
                throw new ConditionalPromotionValidationException(
                    "product_not_found", $"Product {productId} was not found.");
            }
        }
    }
}
