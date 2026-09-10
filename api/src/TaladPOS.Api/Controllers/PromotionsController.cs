using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Promotions;
using TaladPOS.Domain.Promotions;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Api.Controllers;

/// <summary>contracts/promotions.md - every endpoint here is Manager-only (FR-029).</summary>
[ApiController]
[Route("api/v1/promotions")]
[Authorize(Roles = nameof(StaffRole.Manager))]
public class PromotionsController : ControllerBase
{
    private readonly IPromotionRepository _promotionRepository;
    private readonly CreatePromotionUseCase _createPromotionUseCase;
    private readonly UpdatePromotionUseCase _updatePromotionUseCase;
    private readonly DeletePromotionUseCase _deletePromotionUseCase;

    public PromotionsController(
        IPromotionRepository promotionRepository,
        CreatePromotionUseCase createPromotionUseCase,
        UpdatePromotionUseCase updatePromotionUseCase,
        DeletePromotionUseCase deletePromotionUseCase)
    {
        _promotionRepository = promotionRepository;
        _createPromotionUseCase = createPromotionUseCase;
        _updatePromotionUseCase = updatePromotionUseCase;
        _deletePromotionUseCase = deletePromotionUseCase;
    }

    public record PromotionRequestDto(
        PromotionScope Scope,
        decimal DiscountPercentage,
        Guid? ProductId,
        bool AppliesToMembersOnly,
        DateOnly StartDate,
        DateOnly EndDate);

    public record PromotionDto(
        Guid Id,
        PromotionScope Scope,
        decimal DiscountPercentage,
        Guid? ProductId,
        bool AppliesToMembersOnly,
        DateOnly StartDate,
        DateOnly EndDate,
        bool IsActive);

    /// <summary>contracts/promotions.md - GET /api/v1/promotions</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<PromotionDto>>> List([FromQuery] bool activeOnly, CancellationToken ct)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var promotions = await _promotionRepository.ListAsync(activeOnly, today, ct);
        return Ok(promotions.Select(p => ToDto(p, today)).ToList());
    }

    /// <summary>contracts/promotions.md - POST /api/v1/promotions (FR-019-FR-021)</summary>
    [HttpPost]
    public async Task<ActionResult<PromotionDto>> Create(PromotionRequestDto request, CancellationToken ct)
    {
        var promotion = await _createPromotionUseCase.ExecuteAsync(
            new CreatePromotionRequest(
                request.Scope, request.DiscountPercentage, request.ProductId, request.AppliesToMembersOnly,
                request.StartDate, request.EndDate),
            ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return CreatedAtAction(nameof(List), null, ToDto(promotion, today));
    }

    /// <summary>contracts/promotions.md - PUT /api/v1/promotions/{id}</summary>
    [HttpPut("{id:guid}")]
    public async Task<ActionResult<PromotionDto>> Update(Guid id, PromotionRequestDto request, CancellationToken ct)
    {
        var promotion = await _updatePromotionUseCase.ExecuteAsync(
            new UpdatePromotionRequest(
                id, request.Scope, request.DiscountPercentage, request.ProductId, request.AppliesToMembersOnly,
                request.StartDate, request.EndDate),
            ct);

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return Ok(ToDto(promotion, today));
    }

    /// <summary>contracts/promotions.md - DELETE /api/v1/promotions/{id}</summary>
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _deletePromotionUseCase.ExecuteAsync(id, ct);
        return NoContent();
    }

    private static PromotionDto ToDto(Promotion promotion, DateOnly today) => new(
        promotion.Id,
        promotion.Scope,
        promotion.DiscountPercentage,
        promotion.ProductId,
        promotion.AppliesToMembersOnly,
        promotion.StartDate,
        promotion.EndDate,
        promotion.IsActive(today));
}
