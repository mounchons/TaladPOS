using Microsoft.EntityFrameworkCore.Storage;
using TaladPOS.Application.Common;

namespace TaladPOS.Infrastructure.Persistence;

public class EfUnitOfWork : IUnitOfWork
{
    private readonly TaladPOSDbContext _dbContext;

    public EfUnitOfWork(TaladPOSDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var transaction = await _dbContext.Database.BeginTransactionAsync(ct);
        return new EfUnitOfWorkTransaction(transaction);
    }

    private sealed class EfUnitOfWorkTransaction : IUnitOfWorkTransaction
    {
        private readonly IDbContextTransaction _transaction;

        public EfUnitOfWorkTransaction(IDbContextTransaction transaction)
        {
            _transaction = transaction;
        }

        public Task CommitAsync(CancellationToken ct = default) => _transaction.CommitAsync(ct);

        public Task RollbackAsync(CancellationToken ct = default) => _transaction.RollbackAsync(ct);

        public ValueTask DisposeAsync() => _transaction.DisposeAsync();
    }
}
