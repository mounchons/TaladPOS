namespace TaladPOS.Application.Common;

/// <summary>
/// Explicit transaction boundary. Needed because
/// <c>IProductRepository.TryDecreaseStockAsync</c> issues its own atomic SQL
/// UPDATE immediately (bypassing the change tracker, research.md #2) and
/// does NOT automatically share a transaction with a later
/// <c>SaveChangesAsync</c> - use cases that must combine both (e.g.
/// CompleteSaleUseCase) wrap them in one transaction here so a stock
/// decrement never lands without its corresponding Sale, or vice versa.
/// </summary>
public interface IUnitOfWork
{
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default);
}

public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken ct = default);

    Task RollbackAsync(CancellationToken ct = default);
}
