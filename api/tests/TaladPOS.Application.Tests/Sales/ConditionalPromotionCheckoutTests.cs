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
/// 003/tasks.md T039, T040 - the parts of the conditional-promotion flow that
/// only exist at the application layer: which promotions are allowed to reach
/// the pricer, that gifts come off the shelf, and that they do not inflate a
/// member's accumulated total.
/// </summary>
public class ConditionalPromotionCheckoutTests
{
    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private static Product NewProduct(string name, decimal price, int stock) =>
        new(name, "https://example.com/img.jpg", price, barcode: null, stockQuantity: stock);

    private static ConditionalPromotion Bundle(
        (Guid Product, int Quantity)[] condition, Reward reward, string name = "โปรทดสอบ") =>
        new(
            name,
            condition.Select(c => new ConditionLine(c.Product, c.Quantity)),
            reward,
            false,
            Today.AddDays(-1),
            Today.AddDays(30));

    private static (CompleteSaleUseCase UseCase, FakeProductRepository Products, FakeMemberRepository Members)
        BuildCheckout(Product[] products, ConditionalPromotion[] conditional, Member? member = null)
    {
        var productRepo = new FakeProductRepository(products);
        var memberRepo = member is null ? new FakeMemberRepository() : new FakeMemberRepository(member);
        var pricing = new CartPricingService(
            productRepo,
            new FakePromotionRepository(),
            new FakeConditionalPromotions(conditional));

        var useCase = new CompleteSaleUseCase(
            productRepo, new FakeSaleRepository(), memberRepo, pricing, new FakeUnitOfWork());

        return (useCase, productRepo, memberRepo);
    }

    // ---------------------------------------------------------------
    // T039 - a promotion pointing at a deleted product never reaches the pricer
    // ---------------------------------------------------------------

    [Fact]
    public async Task ADeletedGiftProduct_StopsThePromotionAndProducesNoHint()
    {
        var noodles = NewProduct("บะหมี่", 20m, stock: 10);
        var deletedSnack = Guid.NewGuid();
        var promotion = Bundle(new[] { (noodles.Id, 1) }, Reward.Gift(deletedSnack, 1));

        var pricing = new CartPricingService(
            new FakeProductRepository(noodles),
            new FakePromotionRepository(),
            new FakeConditionalPromotions(new[] { promotion }));

        var priced = await pricing.PriceAsync(
            new[] { new CartLine(noodles.Id, 3) }, hasMember: false, Today);

        priced.Cart.AppliedPromotions.Should().BeEmpty();
        priced.Cart.UnclaimedGifts.Should().BeEmpty(
            "the cashier cannot fetch a product that no longer exists");
        priced.Cart.TotalAmount.Should().Be(60m);
    }

    [Fact]
    public async Task PricingLoadsProductsThePromotionNeeds_EvenWhenTheyAreNotInTheCart()
    {
        var noodles = NewProduct("บะหมี่", 20m, stock: 10);
        var snack = NewProduct("ขนม", 15m, stock: 10);
        var promotion = Bundle(new[] { (noodles.Id, 1) }, Reward.Gift(snack.Id, 1));

        var pricing = new CartPricingService(
            new FakeProductRepository(noodles, snack),
            new FakePromotionRepository(),
            new FakeConditionalPromotions(new[] { promotion }));

        // The snack is not in the cart, so the hint can only be produced if the
        // service looked it up on the promotion's behalf (003/research.md #7).
        var priced = await pricing.PriceAsync(
            new[] { new CartLine(noodles.Id, 1) }, hasMember: false, Today);

        var hint = priced.Cart.UnclaimedGifts.Should().ContainSingle().Subject;
        hint.GiftProductName.Should().Be("ขนม");
        hint.MissingQuantity.Should().Be(1);
    }

    // ---------------------------------------------------------------
    // T040 - stock (FR-017) and member accumulation (FR-022)
    // ---------------------------------------------------------------

    [Fact]
    public async Task Checkout_DecrementsStockForGiftUnitsToo()
    {
        var noodles = NewProduct("บะหมี่", 20m, stock: 10);
        var snack = NewProduct("ขนม", 15m, stock: 10);
        var promotion = Bundle(new[] { (noodles.Id, 1) }, Reward.Gift(snack.Id, 1));

        var (useCase, products, _) = BuildCheckout(new[] { noodles, snack }, new[] { promotion });

        var sale = await useCase.ExecuteAsync(new CompleteSaleRequest(
            Guid.NewGuid(),
            null,
            new[]
            {
                new CompleteSaleLineItemRequest(noodles.Id, 2),
                new CompleteSaleLineItemRequest(snack.Id, 2),
            }));

        products.Get(noodles.Id).StockQuantity.Should().Be(8);
        products.Get(snack.Id).StockQuantity.Should().Be(8, "both snacks left the shelf, free or not");

        sale.LineItems.Where(li => li.IsGift).Sum(li => li.Quantity).Should().Be(2);
        sale.TotalAmount.Should().Be(40m, "only the noodles are charged for");
    }

    [Fact]
    public async Task Checkout_RecordsWhichPromotionFiredAsASnapshot()
    {
        var noodles = NewProduct("บะหมี่", 20m, stock: 10);
        var snack = NewProduct("ขนม", 15m, stock: 10);
        var promotion = Bundle(new[] { (noodles.Id, 1) }, Reward.Gift(snack.Id, 1));

        var (useCase, _, _) = BuildCheckout(new[] { noodles, snack }, new[] { promotion });

        var sale = await useCase.ExecuteAsync(new CompleteSaleRequest(
            Guid.NewGuid(),
            null,
            new[]
            {
                new CompleteSaleLineItemRequest(noodles.Id, 1),
                new CompleteSaleLineItemRequest(snack.Id, 1),
            }));

        var recorded = sale.AppliedPromotions.Should().ContainSingle().Subject;
        recorded.PromotionId.Should().Be(promotion.Id);
        recorded.SetCount.Should().Be(1);
        recorded.DiscountAmount.Should().Be(15m);
        recorded.DescriptionSnapshot.Should().Be("ซื้อ บะหมี่ 1 แถม ขนม 1");
    }

    [Fact]
    public async Task Checkout_DoesNotAddTheGiftValueToAMembersAccumulatedTotal()
    {
        var noodles = NewProduct("บะหมี่", 20m, stock: 10);
        var snack = NewProduct("ขนม", 15m, stock: 10);
        var promotion = Bundle(new[] { (noodles.Id, 1) }, Reward.Gift(snack.Id, 1));
        var member = new Member("สมชาย ใจดี", "0812345678");

        var (useCase, _, members) = BuildCheckout(new[] { noodles, snack }, new[] { promotion }, member);

        var sale = await useCase.ExecuteAsync(new CompleteSaleRequest(
            Guid.NewGuid(),
            member.Id,
            new[]
            {
                new CompleteSaleLineItemRequest(noodles.Id, 1),
                new CompleteSaleLineItemRequest(snack.Id, 1),
            }));

        sale.TotalAmount.Should().Be(20m);
        members.Accumulated(member.Id).Should().Be(20m, "the free snack is worth nothing to the member total");
    }

    [Fact]
    public async Task Checkout_FailsWhenStockCannotCoverTheGiftUnits()
    {
        var noodles = NewProduct("บะหมี่", 20m, stock: 10);
        var snack = NewProduct("ขนม", 15m, stock: 1);
        var promotion = Bundle(new[] { (noodles.Id, 1) }, Reward.Gift(snack.Id, 1));

        var (useCase, _, _) = BuildCheckout(new[] { noodles, snack }, new[] { promotion });

        var act = () => useCase.ExecuteAsync(new CompleteSaleRequest(
            Guid.NewGuid(),
            null,
            new[]
            {
                new CompleteSaleLineItemRequest(noodles.Id, 2),
                new CompleteSaleLineItemRequest(snack.Id, 2),
            }));

        await act.Should().ThrowAsync<InsufficientStockException>();
    }

    private sealed class FakeConditionalPromotions : IConditionalPromotionRepository
    {
        private readonly List<ConditionalPromotion> _promotions;

        public FakeConditionalPromotions(IEnumerable<ConditionalPromotion> promotions) =>
            _promotions = promotions.ToList();

        public Task<ConditionalPromotion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_promotions.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<ConditionalPromotion>> ListAsync(
            bool activeOnly, DateOnly today, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConditionalPromotion>>(
                _promotions.Where(p => !activeOnly || p.IsActive(today)).ToList());

        public Task<IReadOnlyList<ConditionalPromotion>> GetActiveOnAsync(
            DateOnly date, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<ConditionalPromotion>>(
                _promotions.Where(p => p.IsActive(date)).ToList());

        public Task AddAsync(ConditionalPromotion promotion, CancellationToken ct = default)
        {
            _promotions.Add(promotion);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(ConditionalPromotion promotion, CancellationToken ct = default) =>
            Task.CompletedTask;

        public Task DeleteAsync(ConditionalPromotion promotion, CancellationToken ct = default)
        {
            _promotions.Remove(promotion);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products;

        public FakeProductRepository(params Product[] products) =>
            _products = products.ToDictionary(p => p.Id);

        public Product Get(Guid id) => _products[id];

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_products.GetValueOrDefault(id));

        public Task<IReadOnlyList<Product>> GetByIdsAsync(
            IEnumerable<Guid> ids, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(
                ids.Distinct()
                    .Select(id => _products.GetValueOrDefault(id))
                    .Where(product => product is not null)
                    .Select(product => product!)
                    .ToList());

        public Task<IReadOnlyList<Product>> SearchAsync(
            string? search, string? barcode, bool lowStockOnly, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(_products.Values.ToList());

        public Task<PagedResult<Product>> SearchPagedAsync(
            string? search, string? barcode, bool lowStockOnly, PageRequest page,
            CancellationToken ct = default) =>
            Task.FromResult(new PagedResult<Product>(
                _products.Values.ToList(), page.Page, page.PageSize, _products.Count));

        public Task<bool> TryDecreaseStockAsync(Guid productId, int quantity, CancellationToken ct = default)
        {
            var product = _products[productId];
            if (product.StockQuantity < quantity)
            {
                return Task.FromResult(false);
            }

            product.DecreaseStock(quantity);
            return Task.FromResult(true);
        }

        public Task<bool> BarcodeExistsAsync(
            string barcode, Guid? excludeProductId, CancellationToken ct = default) =>
            Task.FromResult(false);

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

        public FakeMemberRepository(params Member[] members) =>
            _members = members.ToDictionary(m => m.Id);

        public decimal Accumulated(Guid memberId) => _accumulated.GetValueOrDefault(memberId);

        public Task<Member?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_members.GetValueOrDefault(id));

        public Task<bool> PhoneNumberExistsAsync(string phoneNumber, CancellationToken ct = default) =>
            Task.FromResult(_members.Values.Any(m => m.PhoneNumber == phoneNumber));

        public Task<IReadOnlyList<Member>> SearchAsync(string? search, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Member>>(_members.Values.ToList());

        public Task AddAsync(Member member, CancellationToken ct = default)
        {
            _members[member.Id] = member;
            return Task.CompletedTask;
        }

        public Task IncreaseAccumulatedPurchaseTotalAsync(
            Guid memberId, decimal amount, CancellationToken ct = default)
        {
            _accumulated[memberId] = _accumulated.GetValueOrDefault(memberId) + amount;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePromotionRepository : IPromotionRepository
    {
        private readonly List<Promotion> _promotions;

        public FakePromotionRepository(params Promotion[] promotions) => _promotions = promotions.ToList();

        public Task<Promotion?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_promotions.FirstOrDefault(p => p.Id == id));

        public Task<IReadOnlyList<Promotion>> ListAsync(
            bool activeOnly, DateOnly today, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Promotion>>(_promotions);

        public Task<IReadOnlyList<Promotion>> GetActiveOnAsync(
            DateOnly date, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Promotion>>(
                _promotions.Where(p => p.IsActive(date)).ToList());

        public Task AddAsync(Promotion promotion, CancellationToken ct = default) => Task.CompletedTask;

        public Task UpdateAsync(Promotion promotion, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Promotion promotion, CancellationToken ct = default) => Task.CompletedTask;
    }

    private sealed class FakeSaleRepository : ISaleRepository
    {
        private readonly List<Sale> _sales = new();

        public Task AddAsync(Sale sale, CancellationToken ct = default)
        {
            _sales.Add(sale);
            return Task.CompletedTask;
        }

        public Task<Sale?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_sales.FirstOrDefault(s => s.Id == id));

        public Task<IReadOnlyList<Sale>> SearchAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");

        public Task<PagedResult<Sale>> SearchPagedAsync(
            DateTime? from, DateTime? to, Guid? staffId, Guid? memberId, PageRequest page,
            CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken ct = default) =>
            Task.FromResult<IUnitOfWorkTransaction>(new FakeTransaction());

        private sealed class FakeTransaction : IUnitOfWorkTransaction
        {
            public Task CommitAsync(CancellationToken ct = default) => Task.CompletedTask;

            public Task RollbackAsync(CancellationToken ct = default) => Task.CompletedTask;

            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }
}
