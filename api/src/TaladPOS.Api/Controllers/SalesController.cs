using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Auth;
using TaladPOS.Application.Members;
using TaladPOS.Application.Reports;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Members;
using TaladPOS.Domain.Sales;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Api.Controllers;

[ApiController]
[Route("api/v1/sales")]
public class SalesController : ControllerBase
{
    private readonly CompleteSaleUseCase _completeSaleUseCase;
    private readonly ISaleRepository _saleRepository;
    private readonly GetSalesHistoryQuery _getSalesHistoryQuery;
    private readonly IStaffRepository _staffRepository;
    private readonly IMemberRepository _memberRepository;

    public SalesController(
        CompleteSaleUseCase completeSaleUseCase,
        ISaleRepository saleRepository,
        GetSalesHistoryQuery getSalesHistoryQuery,
        IStaffRepository staffRepository,
        IMemberRepository memberRepository)
    {
        _completeSaleUseCase = completeSaleUseCase;
        _saleRepository = saleRepository;
        _getSalesHistoryQuery = getSalesHistoryQuery;
        _staffRepository = staffRepository;
        _memberRepository = memberRepository;
    }

    public record SaleLineItemRequestDto(Guid ProductId, int Quantity);

    public record CreateSaleRequestDto(Guid? MemberId, IReadOnlyList<SaleLineItemRequestDto> LineItems);

    public record SaleLineItemDto(
        Guid ProductId, string ProductNameSnapshot, decimal UnitPriceSnapshot, int Quantity,
        decimal DiscountAmount, decimal LineTotal);

    public record StaffSummaryDto(Guid Id, string Name);

    public record MemberSummaryDto(Guid Id, string Name);

    public record SaleDto(
        Guid Id, DateTime CreatedAt, StaffSummaryDto? Staff, MemberSummaryDto? Member,
        IReadOnlyList<SaleLineItemDto> LineItems, decimal SubtotalAmount, decimal DiscountAmount,
        decimal TotalAmount);

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

    /// <summary>contracts/sales.md - GET /api/v1/sales (ประวัติการขาย, FR-024)</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SaleDto>>> List(
        [FromQuery] DateOnly? from, [FromQuery] DateOnly? to, [FromQuery] Guid? staffId, [FromQuery] Guid? memberId,
        CancellationToken ct)
    {
        var sales = await _getSalesHistoryQuery.ExecuteAsync(from, to, staffId, memberId, ct);
        return Ok(await ToDtosAsync(sales, ct));
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
                li.LineTotal))
            .ToList(),
        sale.SubtotalAmount,
        sale.DiscountAmount,
        sale.TotalAmount);
}
