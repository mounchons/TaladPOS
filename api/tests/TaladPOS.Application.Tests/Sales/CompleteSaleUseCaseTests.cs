using FluentAssertions;
using TaladPOS.Application.Common;
using TaladPOS.Application.Members;
using TaladPOS.Application.Products;
using TaladPOS.Application.Promotions;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Members;
using TaladPOS.Domain.Products;
using TaladPOS.Domain.Promotions;
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
        var members = new FakeMemberRepository();
        var promotions = new FakePromotionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, members, promotions, unitOfWork);

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
    public async Task ExecuteAsync_WithMemberId_IncreasesMemberAccumulatedTotalByTotalAmount()
    {
        var mango = NewProduct("มะม่วง", 45m, stock: 10);
        var products = new FakeProductRepository(mango);
        var sales = new FakeSaleRepository();
        var member = new Member("สมชาย ใจดี", "0812345678");
        var members = new FakeMemberRepository(member);
        var promotions = new FakePromotionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, members, promotions, unitOfWork);

        var request = new CompleteSaleRequest(
            StaffId: Guid.NewGuid(),
            MemberId: member.Id,
            LineItems: [new CompleteSaleLineItemRequest(mango.Id, 2)]);

        var sale = await useCase.ExecuteAsync(request);

        members.AccumulatedTotal(member.Id).Should().Be(sale.TotalAmount);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveItemPromotion_AppliesItToThatLineOnly()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mango = NewProduct("มะม่วง", 45m, stock: 10);
        var apple = NewProduct("แอปเปิ้ล", 60m, stock: 10);
        var products = new FakeProductRepository(mango, apple);
        var sales = new FakeSaleRepository();
        var members = new FakeMemberRepository();
        var itemPromo = new Promotion(PromotionScope.Item, 10m, mango.Id, false, today, today);
        var promotions = new FakePromotionRepository(itemPromo);
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, members, promotions, unitOfWork);

        var request = new CompleteSaleRequest(
            StaffId: Guid.NewGuid(),
            MemberId: null,
            LineItems: [new CompleteSaleLineItemRequest(mango.Id, 2), new CompleteSaleLineItemRequest(apple.Id, 1)]);

        var sale = await useCase.ExecuteAsync(request);

        var mangoLine = sale.LineItems.Single(li => li.ProductId == mango.Id);
        var appleLine = sale.LineItems.Single(li => li.ProductId == apple.Id);
        mangoLine.DiscountAmount.Should().Be(9.00m, "10% of 90.00 (2 x 45.00)");
        appleLine.DiscountAmount.Should().Be(0m, "the promotion targets a different product");
        sale.DiscountAmount.Should().Be(9.00m);
        sale.TotalAmount.Should().Be(150.00m - 9.00m);
    }

    [Fact]
    public async Task ExecuteAsync_WithActiveBillPromotion_DistributesDiscountAcrossAllLines()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mango = NewProduct("มะม่วง", 45m, stock: 10);
        var apple = NewProduct("แอปเปิ้ล", 60m, stock: 10);
        var products = new FakeProductRepository(mango, apple);
        var sales = new FakeSaleRepository();
        var members = new FakeMemberRepository();
        var billPromo = new Promotion(PromotionScope.Bill, 10m, null, false, today, today);
        var promotions = new FakePromotionRepository(billPromo);
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, members, promotions, unitOfWork);

        var request = new CompleteSaleRequest(
            StaffId: Guid.NewGuid(),
            MemberId: null,
            LineItems: [new CompleteSaleLineItemRequest(mango.Id, 2), new CompleteSaleLineItemRequest(apple.Id, 1)]);

        var sale = await useCase.ExecuteAsync(request);

        sale.SubtotalAmount.Should().Be(150.00m);
        sale.DiscountAmount.Should().Be(15.00m, "10% of the 150.00 bill subtotal");
        sale.TotalAmount.Should().Be(135.00m);
        sale.LineItems.Sum(li => li.DiscountAmount).Should().Be(sale.DiscountAmount, "the bill discount must be fully distributed across lines, not lost or invented");
    }

    [Fact]
    public async Task ExecuteAsync_WithMembersOnlyPromotion_OnlyAppliesWhenSaleHasAMember()
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var mango = NewProduct("มะม่วง", 45m, stock: 10);
        var member = new Member("สมชาย ใจดี", "0812345678");
        var membersOnlyPromo = new Promotion(PromotionScope.Item, 20m, mango.Id, true, today, today);
        var promotions = new FakePromotionRepository(membersOnlyPromo);

        var withoutMemberUseCase = new CompleteSaleUseCase(
            new FakeProductRepository(mango), new FakeSaleRepository(), new FakeMemberRepository(),
            promotions, new FakeUnitOfWork());
        var saleWithoutMember = await withoutMemberUseCase.ExecuteAsync(new CompleteSaleRequest(
            Guid.NewGuid(), null, [new CompleteSaleLineItemRequest(mango.Id, 1)]));
        saleWithoutMember.DiscountAmount.Should().Be(0m);

        var mangoForMemberSale = NewProduct("มะม่วง", 45m, stock: 10);
        var withMemberUseCase = new CompleteSaleUseCase(
            new FakeProductRepository(mangoForMemberSale), new FakeSaleRepository(), new FakeMemberRepository(member),
            new FakePromotionRepository(new Promotion(PromotionScope.Item, 20m, mangoForMemberSale.Id, true, today, today)),
            new FakeUnitOfWork());
        var saleWithMember = await withMemberUseCase.ExecuteAsync(new CompleteSaleRequest(
            Guid.NewGuid(), member.Id, [new CompleteSaleLineItemRequest(mangoForMemberSale.Id, 1)]));
        saleWithMember.DiscountAmount.Should().Be(9.00m, "20% of 45.00");
    }

    [Fact]
    public async Task ExecuteAsync_WithUnknownMemberId_ThrowsAndNeverDecreasesStock()
    {
        var mango = NewProduct("มะม่วง", 45m, stock: 10);
        var products = new FakeProductRepository(mango);
        var sales = new FakeSaleRepository();
        var members = new FakeMemberRepository();
        var promotions = new FakePromotionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, members, promotions, unitOfWork);

        var request = new CompleteSaleRequest(
            StaffId: Guid.NewGuid(),
            MemberId: Guid.NewGuid(),
            LineItems: [new CompleteSaleLineItemRequest(mango.Id, 2)]);

        var act = async () => await useCase.ExecuteAsync(request);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        products.RemainingStock(mango.Id).Should().Be(10, "an unknown member must be rejected before any stock is touched");
        sales.Added.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_WhenStockInsufficient_ThrowsAndNeverAddsTheSale()
    {
        var mango = NewProduct("มะม่วง", 45m, stock: 1);
        var products = new FakeProductRepository(mango);
        var sales = new FakeSaleRepository();
        var members = new FakeMemberRepository();
        var promotions = new FakePromotionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, members, promotions, unitOfWork);

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
        var members = new FakeMemberRepository();
        var promotions = new FakePromotionRepository();
        var unitOfWork = new FakeUnitOfWork();
        var useCase = new CompleteSaleUseCase(products, sales, members, promotions, unitOfWork);

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

        public Task<PagedResult<Product>> SearchPagedAsync(
            string? search, string? barcode, bool lowStockOnly, PageRequest page,
            CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");

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

        public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeProductId, CancellationToken ct = default) =>
            Task.FromResult(_products.Values.Any(p => p.Barcode == barcode && p.Id != excludeProductId));

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            _products[product.Id] = product;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Product product, CancellationToken ct = default)
        {
            _products.Remove(product.Id);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeMemberRepository : IMemberRepository
    {
        private readonly Dictionary<Guid, Member> _members;
        private readonly Dictionary<Guid, decimal> _accumulated = new();

        public FakeMemberRepository(params Member[] members) => _members = members.ToDictionary(m => m.Id);

        public Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_members.GetValueOrDefault(id));

        public Task<IReadOnlyList<Member>> SearchAsync(string? search, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Member>>(_members.Values.ToList());

        public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken ct = default) =>
            Task.FromResult(_members.Values.Any(m => m.PhoneNumber == phoneNumber));

        public Task AddAsync(Member member, CancellationToken ct = default)
        {
            _members[member.Id] = member;
            return Task.CompletedTask;
        }

        public Task IncreaseAccumulatedPurchaseTotalAsync(Guid memberId, decimal amount, CancellationToken ct = default)
        {
            _accumulated[memberId] = _accumulated.GetValueOrDefault(memberId) + amount;
            return Task.CompletedTask;
        }

        public decimal AccumulatedTotal(Guid memberId) => _accumulated.GetValueOrDefault(memberId);
    }

    private sealed class FakePromotionRepository : IPromotionRepository
    {
        private readonly List<Promotion> _promotions;

        public FakePromotionRepository(params Promotion[] promotions) => _promotions = promotions.ToList();

        public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_promotions.SingleOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<Promotion>> ListAsync(bool activeOnly, DateOnly today, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Promotion>>(_promotions);

        public Task<IReadOnlyList<Promotion>> GetActiveOnAsync(DateOnly date, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Promotion>>(_promotions.Where(p => p.IsActive(date)).ToList());

        public Task AddAsync(Promotion promotion, CancellationToken ct = default)
        {
            _promotions.Add(promotion);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Promotion promotion, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Promotion promotion, CancellationToken ct = default)
        {
            _promotions.Remove(promotion);
            return Task.CompletedTask;
        }
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

        public Task<IReadOnlyList<Sale>> SearchAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");

        // These fakes back tests that aggregate over every matching bill, never
        // a page of them (research.md #6). Throwing rather than returning an
        // empty page keeps an accidental future call loud instead of silently
        // reporting zero takings.
        public Task<PagedResult<Sale>> SearchPagedAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, PageRequest page,
            CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");
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
