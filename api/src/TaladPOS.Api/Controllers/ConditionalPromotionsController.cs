using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Promotions;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Api.Controllers;

/// <summary>
/// 003/contracts/conditional-promotions.md - Manager-only, same as the existing
/// promotions screen (001/FR-029). Deliberately a separate route from
/// /api/v1/promotions so the original endpoints keep their exact shape
/// (003/FR-026).
/// </summary>
[ApiController]
[Route("api/v1/conditional-promotions")]
[Authorize(Roles = nameof(StaffRole.Manager))]
public class ConditionalPromotionsController : ControllerBase
{
    private readonly ListConditionalPromotionsQuery _list;
    private readonly CreateConditionalPromotionUseCase _create;
    private readonly UpdateConditionalPromotionUseCase _update;
    private readonly DeleteConditionalPromotionUseCase _delete;

    public ConditionalPromotionsController(
        ListConditionalPromotionsQuery list,
        CreateConditionalPromotionUseCase create,
        UpdateConditionalPromotionUseCase update,
        DeleteConditionalPromotionUseCase delete)
    {
        _list = list;
        _create = create;
        _update = update;
        _delete = delete;
    }

    public record ConditionLineDto(Guid ProductId, string? ProductName, int MinimumQuantity);

    public record RewardDto(
        RewardKind Kind,
        Guid? GiftProductId,
        string? GiftProductName,
        int? GiftQuantity,
        decimal? DiscountPercentage);

    public record ConditionalPromotionDto(
        Guid Id,
        string Name,
        IReadOnlyList<ConditionLineDto> ConditionLines,
        RewardDto Reward,
        bool AppliesToMembersOnly,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsActive,
        string Description,
        bool IsUsable,
        string? UnusableReason);

    public record ConditionLineInputDto(Guid ProductId, int MinimumQuantity);

    public record RewardInputDto(
        RewardKind Kind, Guid? GiftProductId, int? GiftQuantity, decimal? DiscountPercentage);

    public record ConditionalPromotionInputDto(
        string Name,
        IReadOnlyList<ConditionLineInputDto> ConditionLines,
        RewardInputDto Reward,
        bool AppliesToMembersOnly,
        DateOnly StartDate,
        DateOnly EndDate);

    /// <summary>GET /api/v1/conditional-promotions</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<ConditionalPromotionDto>>> List(
        [FromQuery] bool activeOnly, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var views = await _list.ExecuteAsync(activeOnly, today, ct);
        return Ok(views.Select(ToDto).ToList());
    }

    /// <summary>POST /api/v1/conditional-promotions (003/FR-001 - FR-007)</summary>
    [HttpPost]
    public async Task<ActionResult<ConditionalPromotionDto>> Create(
        ConditionalPromotionInputDto request, CancellationToken ct)
    {
        try
        {
            var promotion = await _create.ExecuteAsync(ToRequest(request), ct);
            return CreatedAtAction(nameof(List), null, await DescribeAsync(promotion, ct));
        }
        catch (ConditionalPromotionValidationException ex)
        {
            return BadRequest(new { error = ex.ErrorCode });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_request", detail = ex.Message });
        }
    }

    /// <summary>PUT /api/v1/conditional-promotions/{id}</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ConditionalPromotionDto>> Update(
        Guid id, ConditionalPromotionInputDto request, CancellationToken ct)
    {
        try
        {
            var promotion = await _update.ExecuteAsync(id, ToRequest(request), ct);
            return Ok(await DescribeAsync(promotion, ct));
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
        catch (ConditionalPromotionValidationException ex)
        {
            return BadRequest(new { error = ex.ErrorCode });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { error = "invalid_request", detail = ex.Message });
        }
    }

    /// <summary>DELETE /api/v1/conditional-promotions/{id}</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        try
        {
            await _delete.ExecuteAsync(id, ct);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    private static ConditionalPromotionRequest ToRequest(ConditionalPromotionInputDto dto) =>
        new(
            dto.Name ?? string.Empty,
            (dto.ConditionLines ?? Array.Empty<ConditionLineInputDto>())
                .Select(line => new ConditionLineRequest(line.ProductId, line.MinimumQuantity))
                .ToList(),
            dto.Reward is null
                ? new RewardRequest(RewardKind.Gift, null, null, null)
                : new RewardRequest(
                    dto.Reward.Kind, dto.Reward.GiftProductId, dto.Reward.GiftQuantity,
                    dto.Reward.DiscountPercentage),
            dto.AppliesToMembersOnly,
            dto.StartDate,
            dto.EndDate);

    /// <summary>
    /// Re-reads the saved promotion through the list query so the response
    /// carries the same description and usability flags a later GET would -
    /// one place composes that text, never two (003/research.md #11).
    /// </summary>
    private async Task<ConditionalPromotionDto> DescribeAsync(
        ConditionalPromotion promotion, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var all = await _list.ExecuteAsync(false, today, ct);
        var view = all.FirstOrDefault(v => v.Id == promotion.Id);
        return view is null
            ? ToDto(ListConditionalPromotionsQuery.ToView(
                promotion, new Dictionary<Guid, string>(), today))
            : ToDto(view);
    }

    private static ConditionalPromotionDto ToDto(ConditionalPromotionView view) => new(
        view.Id,
        view.Name,
        view.ConditionLines
            .Select(line => new ConditionLineDto(line.ProductId, line.ProductName, line.MinimumQuantity))
            .ToList(),
        new RewardDto(
            view.Reward.Kind,
            view.Reward.GiftProductId,
            view.Reward.GiftProductName,
            view.Reward.GiftQuantity,
            view.Reward.DiscountPercentage),
        view.AppliesToMembersOnly,
        view.StartDate,
        view.EndDate,
        view.IsActive,
        view.Description,
        view.IsUsable,
        view.UnusableReason);
}
