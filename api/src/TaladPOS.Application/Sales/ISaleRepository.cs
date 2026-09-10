using TaladPOS.Domain.Sales;

namespace TaladPOS.Application.Sales;

public interface ISaleRepository
{
    Task AddAsync(Sale sale, CancellationToken ct = default);

    Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default);
}
