using TaladPOS.Application.Common;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Auth;
using TaladPOS.Application.Members;
using TaladPOS.Application.Reports;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Members;
using TaladPOS.Domain.Promotions;
using TaladPOS.Domain.Sales;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Api.Controllers;

[ApiController]
[Route("api/v1/sales")]
public class SalesController : ControllerBase
{
    private readonly CompleteSaleUseCase _completeSaleUseCase;
    private readonly PreviewSaleUseCase _previewSaleUseCase;
    private readonly ISaleRepository _saleRepository;
    private readonly GetSalesHistoryQuery _getSalesHistoryQuery;
    private readonly IStaffRepository _staffRepository;
    private readonly IMemberRepository _memberRepository;

    public SalesController(
        CompleteSaleUseCase completeSaleUseCase,
        PreviewSaleUseCase previewSaleUseCase,
        ISaleRepository saleRepository,
        GetSalesHistoryQuery getSalesHistoryQuery,
        IStaffRepository staffRepository,
        IMemberRepository memberRepository)
    {
        _completeSaleUseCase = completeSaleUseCase;
        _previewSaleUseCase = previewSaleUseCase;
        _saleRepository = saleRepository;
        _getSalesHistoryQuery = getSalesHistoryQuery;
        _staffRepository = staffRepository;
        _memberRepository = memberRepository;
    }

    public record SaleLineItemRequestDto(Guid ProductId, int Quantity);

    public record CreateSaleRequestDto(Guid? MemberId, IReadOnlyList<SaleLineItemRequestDto> LineItems);

    public record SaleLineItemDto(
        Guid ProductId, string ProductNameSnapshot, decimal UnitPriceSnapshot, int Quantity,
        decimal DiscountAmount, decimal LineTotal, bool IsGift);

    /// <summary>003/FR-024 - which conditional promotions this bill used.</summary>
    public record AppliedPromotionDto(
        Guid PromotionId, string Description, int SetCount, decimal DiscountAmount);

    /// <summary>003/contracts/sales-preview.md - one line of a priced cart.</summary>
    public record PricedLineDto(
        Guid ProductId, string ProductName, decimal UnitPrice, int Quantity,
        decimal DiscountAmount, decimal LineTotal, bool IsGift);

    /// <summary>003/FR-015 - an earned gift the cashier has not put in the cart yet.</summary>
    public record UnclaimedGiftDto(
        Guid PromotionId, string Description, Guid GiftProductId, string GiftProductName, int MissingQuantity);

    public record PricedCartDto(
        IReadOnlyList<PricedLineDto> Lines,
        IReadOnlyList<AppliedPromotionDto> AppliedPromotions,
        IReadOnlyList<UnclaimedGiftDto> UnclaimedGifts,
        decimal SubtotalAmount,
        decimal DiscountAmount,
        decimal TotalAmount);

    public record StaffSummaryDto(Guid Id, string Name);

    public record MemberSummaryDto(Guid Id, string Name);

    public record SaleDto(
        Guid Id, DateTime CreatedAt, StaffSummaryDto? Staff, MemberSummaryDto? Member,
        IReadOnlyList<SaleLineItemDto> LineItems, decimal SubtotalAmount, decimal DiscountAmount,
        decimal TotalAmount, IReadOnlyList<AppliedPromotionDto> AppliedPromotions);

    /// <summary>contracts/sales.md - POST /api/v1/sales (checkout, FR-005/FR-006/FR-008/FR-016/FR-023)</summary>
    [HttpPost]
    public async Task<ActionResult<SaleDto>> Create(CreateSaleRequestDto request, CancellationToken ct)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            return BadRequest(new { error = "empty_cart" });
        }

        var staffId = GetStaffIdFromClaims();

        var useCaseRequest = new CompleteSaleRequest(
            staffId,
            request.MemberId,
            request.LineItems.Select(li => new CompleteSaleLineItemRequest(li.ProductId, li.Quantity)).ToList());

        var sale = await _completeSaleUseCase.ExecuteAsync(useCaseRequest, ct);
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, await ToDtoAsync(sale, ct));
    }

    /// <summary>
    /// 003/contracts/sales-preview.md - POST /api/v1/sales/preview.
    ///
    /// No role attribute, so the fallback policy in Program.cs lets any signed-in
    /// staff member call it. That is deliberate: the register is operated by
    /// cashiers, and /api/v1/conditional-promotions is Manager-only, so this is
    /// the only way the sales screen can learn what a cart is worth without the
    /// web app computing discounts itself (constitution Principle I).
    /// </summary>
    [HttpPost("preview")]
    public async Task<ActionResult<PricedCartDto>> Preview(CreateSaleRequestDto request, CancellationToken ct)
    {
        if (request.LineItems is null || request.LineItems.Count == 0)
        {
            return BadRequest(new { error = "empty_cart" });
        }

        if (request.LineItems.Any(li => li.Quantity <= 0))
        {
            return BadRequest(new { error = "invalid_quantity" });
        }

        try
        {
            var cart = await _previewSaleUseCase.ExecuteAsync(
                new PreviewSaleRequest(
                    request.MemberId,
                    request.LineItems
                        .Select(li => new CompleteSaleLineItemRequest(li.ProductId, li.Quantity))
                        .ToList()),
                ct);

            return Ok(ToDto(cart));
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { error = "not_found", detail = ex.Message });
        }
    }

    /// <summary>contracts/sales.md - GET /api/v1/sales (ประวัติการขาย, FR-024)</summary>
    [HttpGet]
    public async Task<ActionResult<PagedResult<SaleDto>>> List(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? staffId, [FromQuery] Guid? memberId,
        [FromQuery] int? page = null, [FromQuery] int? pageSize = null,
        CancellationToken ct = default)
    {
        if (!PageRequest.TryCreate(page, pageSize, out var pageRequest))
        {
            return BadRequest(new { error = "invalid_pagination" });
        }

        var sales = await _getSalesHistoryQuery.ExecuteAsync(from, to, staffId, memberId, pageRequest, ct);

        // ToDtosAsync resolves staff/member names, so it has to run on the page
        // rather than inside Map - the envelope is rebuilt around its result.
        var dtos = await ToDtosAsync(sales.Items, ct);
        return Ok(new PagedResult<SaleDto>(dtos, sales.Page, sales.PageSize, sales.TotalCount));
    }

    /// <summary>contracts/sales.md - GET /api/v1/sales/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> GetById(Guid id, CancellationToken ct)
    {
        var sale = await _saleRepository.GetByIdAsync(id, ct);
        return sale is null ? NotFound() : Ok(await ToDtoAsync(sale, ct));
    }

    /// <summary>contracts/sales.md - GET /api/v1/sales/{id}/receipt (FR-030, research.md #5)</summary>
    [HttpGet("{id:guid}/receipt")]
    public async Task<ActionResult<SaleDto>> GetReceipt(Guid id, CancellationToken ct)
    {
        var sale = await _saleRepository.GetByIdAsync(id, ct);
        return sale is null ? NotFound() : Ok(await ToDtoAsync(sale, ct));
    }

    private Guid GetStaffIdFromClaims()
    {
        // FR-008: the seller comes from the authenticated JWT, never from the
        // request body, so a sale's attribution can't be spoofed by the client.
        var claim = User.FindFirst("staffId")?.Value;
        return Guid.TryParse(claim, out var staffId)
            ? staffId
            : throw new InvalidOperationException("Authenticated request is missing a valid staffId claim.");
    }

    private async Task<SaleDto> ToDtoAsync(Sale sale, CancellationToken ct)
    {
        var staff = await _staffRepository.GetByIdAsync(sale.StaffId, ct);
        var member = sale.MemberId is Guid memberId ? await _memberRepository.GetByIdAsync(memberId, ct) : null;
        return ToDto(sale, staff, member);
    }

    private async Task<IReadOnlyList<SaleDto>> ToDtosAsync(IReadOnlyList<Sale> sales, CancellationToken ct)
    {
        var staffById = (await _staffRepository.GetAllAsync(ct)).ToDictionary(s => s.Id);
        var memberCache = new Dictionary<Guid, Member?>();

        var dtos = new List<SaleDto>(sales.Count);
        foreach (var sale in sales)
        {
            staffById.TryGetValue(sale.StaffId, out var staff);

            Member? member = null;
            if (sale.MemberId is Guid memberId)
            {
                if (!memberCache.TryGetValue(memberId, out member))
                {
                    member = await _memberRepository.GetByIdAsync(memberId, ct);
                    memberCache[memberId] = member;
                }
            }

            dtos.Add(ToDto(sale, staff, member));
        }

        return dtos;
    }

    private static SaleDto ToDto(Sale sale, Staff? staff, Member? member) => new(
        sale.Id,
        sale.CreatedAtUtc,
        staff is null ? null : new StaffSummaryDto(staff.Id, staff.Name),
        member is null ? null : new MemberSummaryDto(member.Id, member.Name),
        sale.LineItems
            .Select(li => new SaleLineItemDto(
                li.ProductId, li.ProductNameSnapshot, li.UnitPriceSnapshot, li.Quantity, li.DiscountAmount,
                li.LineTotal, li.IsGift))
            .ToList(),
        sale.SubtotalAmount,
        sale.DiscountAmount,
        sale.TotalAmount,
        sale.AppliedPromotions
            .Select(p => new AppliedPromotionDto(
                p.PromotionId, p.DescriptionSnapshot, p.SetCount, p.DiscountAmount))
            .ToList());

    /// <summary>003/contracts/sales-preview.md - POST /api/v1/sales/preview</summary>
    internal static PricedCartDto ToDto(PricedCart cart) => new(
        cart.Lines
            .Select(line => new PricedLineDto(
                line.ProductId, line.ProductName, line.UnitPrice, line.Quantity,
                line.DiscountAmount, line.LineTotal, line.IsGift))
            .ToList(),
        cart.AppliedPromotions
            .Select(p => new AppliedPromotionDto(p.PromotionId, p.Description, p.SetCount, p.DiscountAmount))
            .ToList(),
        cart.UnclaimedGifts
            .Select(g => new UnclaimedGiftDto(
                g.PromotionId, g.Description, g.GiftProductId, g.GiftProductName, g.MissingQuantity))
            .ToList(),
        cart.SubtotalAmount,
        cart.DiscountAmount,
        cart.TotalAmount);
}
