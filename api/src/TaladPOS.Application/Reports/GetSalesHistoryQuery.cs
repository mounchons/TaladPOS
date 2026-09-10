using TaladPOS.Application.Sales;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Application.Reports;

/// <summary>contracts/sales.md - GET /api/v1/sales (FR-024)</summary>
public sealed class GetSalesHistoryQuery
{
    private readonly ISaleRepository _saleRepository;

    public GetSalesHistoryQuery(ISaleRepository saleRepository) => _saleRepository = saleRepository;

    public Task<IReadOnlyList<Sale>> ExecuteAsync(
        DateOnly? from, DateOnly? to, Guid? staffId, Guid? memberId, CancellationToken ct = default)
    {
        var fromUtc = from?.ToDateTime(TimeOnly.MinValue, DateTimeKind.Utc);
        var toUtc = to?.ToDateTime(TimeOnly.MaxValue, DateTimeKind.Utc);
        return _saleRepository.SearchAsync(fromUtc, toUtc, staffId, memberId, ct);
    }
}
