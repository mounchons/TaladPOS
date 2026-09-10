using TaladPOS.Domain.Sales;

namespace TaladPOS.Application.Sales;

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken ct = default);

    Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>contracts/sales.md - GET /api/sales (FR-024); also the raw data source for reports (research.md #6 - aggregation happens in-memory, not via SQL GroupBy).</summary>
    Task<IReadOnlyList<Sale>> SearchAsync(
        DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default);
}
