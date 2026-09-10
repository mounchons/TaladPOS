using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaladPOS.Application.Reports;
using TaladPOS.Domain.Staff;

namespace TaladPOS.Api.Controllers;

/// <summary>contracts/reports.md - every endpoint here is Manager-only (FR-029).</summary>
[ApiController]
[Route("api/reports")]
[Authorize(Roles = nameof(StaffRole.Manager))]
public class ReportsController : ControllerBase
{
    private readonly GetDailyOrMonthlySalesReportQuery _salesReportQuery;
    private readonly GetBestSellingProductsReportQuery _bestSellingProductsQuery;
    private readonly GetSalesByStaffReportQuery _salesByStaffQuery;
    private readonly GetStockReportQuery _stockReportQuery;

    public ReportsController(
        GetDailyOrMonthlySalesReportQuery salesReportQuery,
        GetBestSellingProductsReportQuery bestSellingProductsQuery,
        GetSalesByStaffReportQuery salesByStaffQuery,
        GetStockReportQuery stockReportQuery)
    {
        _salesReportQuery = salesReportQuery;
        _bestSellingProductsQuery = bestSellingProductsQuery;
        _salesByStaffQuery = salesByStaffQuery;
        _stockReportQuery = stockReportQuery;
    }

    /// <summary>contracts/reports.md - GET /api/reports/sales (FR-025)</summary>
    [HttpGet("sales")]
    public async Task<ActionResult<SalesReportResult>> Sales(
        [FromQuery] string period, [FromQuery] DateOnly date, CancellationToken ct)
    {
        var result = await _salesReportQuery.ExecuteAsync(period, date, ct);
        return Ok(result);
    }

    /// <summary>contracts/reports.md - GET /api/reports/best-selling-products (FR-026)</summary>
    [HttpGet("best-selling-products")]
    public async Task<ActionResult<IReadOnlyList<BestSellingProductRow>>> BestSellingProducts(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, [FromQuery] int limit = 10, CancellationToken ct = default)
    {
        var result = await _bestSellingProductsQuery.ExecuteAsync(from, to, limit, ct);
        return Ok(result);
    }

    /// <summary>contracts/reports.md - GET /api/reports/sales-by-staff (FR-027)</summary>
    [HttpGet("sales-by-staff")]
    public async Task<ActionResult<IReadOnlyList<SalesByStaffRow>>> SalesByStaff(
        [FromQuery] DateOnly from, [FromQuery] DateOnly to, CancellationToken ct)
    {
        var result = await _salesByStaffQuery.ExecuteAsync(from, to, ct);
        return Ok(result);
    }

    /// <summary>contracts/reports.md - GET /api/reports/stock (FR-028)</summary>
    [HttpGet("stock")]
    public async Task<ActionResult<IReadOnlyList<StockReportRow>>> Stock(CancellationToken ct)
    {
        var result = await _stockReportQuery.ExecuteAsync(ct);
        return Ok(result);
    }
}
