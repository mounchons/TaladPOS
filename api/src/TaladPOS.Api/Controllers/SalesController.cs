using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Api.Controllers;

[ApiController]
[Route("api/sales")]
public class SalesController : ControllerBase
{
    private readonly CompleteSaleUseCase _completeSaleUseCase;
    private readonly ISaleRepository _saleRepository;

    public SalesController(CompleteSaleUseCase completeSaleUseCase, ISaleRepository saleRepository)
    {
        _completeSaleUseCase = completeSaleUseCase;
        _saleRepository = saleRepository;
    }

    public record SaleLineItemRequestDto(Guid ProductId, int Quantity);

    public record CreateSaleRequestDto(Guid? MemberId, IReadOnlyList<SaleLineItemRequestDto> LineItems);

    public record SaleLineItemDto(
        Guid ProductId, string ProductNameSnapshot, decimal UnitPriceSnapshot, int Quantity,
        decimal DiscountAmount, decimal LineTotal);

    public record SaleDto(
        Guid Id, DateTime CreatedAt, Guid StaffId, Guid? MemberId,
        IReadOnlyList<SaleLineItemDto> LineItems, decimal SubtotalAmount, decimal DiscountAmount,
        decimal TotalAmount);

    /// <summary>contracts/sales.md - POST /api/sales (checkout, FR-005/FR-006/FR-008/FR-016/FR-023)</summary>
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
        return CreatedAtAction(nameof(GetById), new { id = sale.Id }, ToDto(sale));
    }

    /// <summary>contracts/sales.md - GET /api/sales/{id}</summary>
    [HttpGet("{id:guid}")]
    public async Task<ActionResult<SaleDto>> GetById(Guid id, CancellationToken ct)
    {
        var sale = await _saleRepository.GetByIdAsync(id, ct);
        return sale is null ? NotFound() : Ok(ToDto(sale));
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

    private static SaleDto ToDto(Sale sale) => new(
        sale.Id,
        sale.CreatedAtUtc,
        sale.StaffId,
        sale.MemberId,
        sale.LineItems
            .Select(li => new SaleLineItemDto(
                li.ProductId, li.ProductNameSnapshot, li.UnitPriceSnapshot, li.Quantity, li.DiscountAmount,
                li.LineTotal))
            .ToList(),
        sale.SubtotalAmount,
        sale.DiscountAmount,
        sale.TotalAmount);
}
