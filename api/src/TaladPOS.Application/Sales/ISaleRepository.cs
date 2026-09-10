using TaladPOS.Application.Common;
using TaladPOS.Domain.Sales;

namespace TaladPOS.Application.Sales;

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken ct = default);

    Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>contracts/sales.md - GET /api/v1/sales (FR-024); also the raw data source for reports (research.md #6 - aggregation happens in-memory, not via SQL GroupBy).</summary>
    Task<IReadOnlyList<Sale>> SearchAsync(
        DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default);

    /// <summary>
    /// contracts/sales.md - the paged form of <see cref="SearchAsync"/>, for the
    /// sales-history screen only.
    ///
    /// The reports keep using the unpaged overload: they aggregate over the
    /// whole matching set (research.md #6), and paging them would quietly turn
    /// a day's takings into the first twenty bills of it.
    /// </summary>
    Task<PagedResult<Sale>> SearchPagedAsync(
        DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, PageRequest page,
        CancellationToken ct = default);
}
