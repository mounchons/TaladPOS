using TaladPOS.Application.Auth;
using TaladPOS.Application.Sales;

namespace TaladPOS.Application.Reports;

public sealed record SalesByStaffRow(Guid StaffId, string StaffName, int BillCount, decimal TotalSalesAmount);

/// <summary>contracts/reports.md - GET /api/reports/sales-by-staff (FR-027)</summary>
public sealed class GetSalesByStaffReportQuery
{
    private readonly ISaleRepository _saleRepository;
    private readonly IStaffRepository _staffRepository;

    public GetSalesByStaffReportQuery(ISaleRepository saleRepository, IStaffRepository staffRepository)
    {
        _saleRepository = saleRepository;
        _staffRepository = staffRepository;
    }

    public async Task<IReadOnlyList<SalesByStaffRow>> ExecuteAsync(DateOnly from, DateOnly to, CancellationToken ct = default)
    {
        var fromUtc = from.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        var sales = await _saleRepository.SearchAsync(fromUtc, toUtc, staffId: null, memberId: null, ct);
        var staffNames = (await _staffRepository.GetAllAsync(ct)).ToDictionary(s => s.Id, s => s.Name);

        return sales
            .GroupBy(s => s.StaffId)
            .Select(g => new SalesByStaffRow(
                g.Key, staffNames.GetValueOrDefault(g.Key, "ไม่ทราบชื่อ"), g.Count(), g.Sum(s => s.TotalAmount)))
            .OrderByDescending(r => r.TotalSalesAmount)
            .ToList();
    }
}
