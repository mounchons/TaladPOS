using FluentAssertions;
using TaladPOS.Application.Common;
using TaladPOS.Application.Products;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Sales;
using Xunit;

namespace TaladPOS.Application.Tests.Sales;

/// <summary>
/// Exercises CompleteSaleUseCase against in-memory fakes. This does NOT
/// verify the atomic SQL UPDATE that makes stock decrement concurrency-safe
/// across registers (research.md #2) - that guarantee only exists in the
/// real PostgreSQL implementation and is covered by the (integration-level,
/// Postgres-dependent) concurrency test in the Polish phase, not here.
/// </summary>
public class CompleteSaleUseCaseTests
{
    private static Product NewProduct(string name, decimal price, int stock) =>
        new(name, "https://example.com/img.jpg", price, barcode: null, stockQuantity: stock);

    [Fact]
    public async Task ExecuteAsync_WithSufficientStock_CreatesSaleAndCommitsTransaction()
    {
        var mango = NewProduct("มะม่วง", 45m, stock: 10);
        var products = new FakeProductRepository(mango);
        var sales = new FakeSaleRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, unitOfWork);

        var request = new CompleteSaleRequest(
            StaffId: Guid.NewGuid(),
            MemberId: null,
            LineItems: [new CompleteSaleLineItemRequest(mango.Id, 2)]);

        var sale = await useCase.ExecuteAsync(request);

        sale.TotalAmount.Should().Be(90m);
        sales.Added.Should().ContainSingle().Which.Should().BeSameAs(sale);
        products.RemainingStock(mango.Id).Should().Be(8);
        unitOfWork.LastTransaction!.Committed.Should().BeTrue();
        unitOfWork.LastTransaction.RolledBack.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStockInsufficient_ThrowsAndNeverAddsTheSale()
    {
        var mango = NewProduct("มะม่วง", 45m, stock: 1);
        var products = new FakeProductRepository(mango);
        var sales = new FakeSaleRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, unitOfWork);

        var request = new CompleteSaleRequest(
            StaffId: Guid.NewGuid(),
            MemberId: null,
            LineItems: [new CompleteSaleLineItemRequest(mango.Id, 5)]);

        var act = async () => await useCase.ExecuteAsync(request);

        await act.Should().ThrowAsync<InsufficientStockException>();
        sales.Added.Should().BeEmpty("a failed stock decrement must not leave a partially-created bill");
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyLineItems_ThrowsWithoutStartingATransaction()
    {
        var products = new FakeProductRepository();
        var sales = new FakeSaleRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, unitOfWork);

        var request = new CompleteSaleRequest(Guid.NewGuid(), null, LineItems: []);

        var act = async () => await useCase.ExecuteAsync(request);

        await act.Should().ThrowAsync<ArgumentException>();
        unitOfWork.LastTransaction.Should().BeNull("checking out an empty cart must be rejected before any transaction work");
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products;

        public FakeProductRepository(params Product[] products) =>
            _products = products.ToDictionary(p => p.Id);

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_products.GetValueOrDefault(id));

        public Task<IReadOnlyList<Product>> SearchAsync(
            string? search, string? barcode, bool lowStockOnly, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(_products.Values.ToList());

        public Task<bool> TryDecreaseStockAsync(Guid productId, int quantity, CancellationToken ct = default)
        {
            if (!_products.TryGetValue(productId, out var product) || product.StockQuantity < quantity)
            {
                return Task.FromResult(false);
            }

            product.DecreaseStock(quantity);
            return Task.FromResult(true);
        }

        public int RemainingStock(Guid productId) => _products[productId].StockQuantity;
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        public List<Sale> Added { get; } = [];

        public Task AddAsync(Sale sale, CancellationToken ct = default)
        {
            Added.Add(sale);
            return Task.CompletedTask;
        }

        public Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(Added.SingleOrDefault(s => s.Id == id));
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public FakeTransaction? LastTransaction { get; private set; }

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default)
        {
            LastTransaction = new FakeTransaction();
            return Task.FromResult<IUnitOfWorkTransaction>(LastTransaction);
        }
    }

    private sealed class FakeTransaction : IUnitOfWorkTransaction
    {
        public bool Committed { get; private set; }

        public bool RolledBack { get; private set; }

        public Task CommitAsync(CancellationToken ct = default)
        {
            Committed = true;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken ct = default)
        {
            RolledBack = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
