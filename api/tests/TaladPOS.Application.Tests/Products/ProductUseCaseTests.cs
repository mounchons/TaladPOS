using TaladPOS.Application.Common;
using FluentAssertions;
using TaladPOS.Application.Products;
using TaladPOS.Domain.Products;
using Xunit;

namespace TaladPOS.Application.Tests.Products;

/// <summary>tasks.md T040 - US3: create/update reject a Barcode already used by another product.</summary>
public class ProductUseCaseTests
{
    private static Product NewProduct(string name, string? barcode, decimal price = 45m, int stock = 10) =>
        new(name, "https://example.com/img.jpg", price, barcode, stock);

    [Fact]
    public async Task Create_WithBarcodeAlreadyUsedByAnotherProduct_ThrowsDuplicateBarcodeException()
    {
        var existing = NewProduct("มะม่วง", "8850000000012");
        var products = new FakeProductRepository(existing);
        var useCase = new CreateProductUseCase(products);

        var request = new CreateProductRequest("แอปเปิ้ล", "https://example.com/apple.jpg", 60m, "8850000000012", 5, null);

        var act = async () => await useCase.ExecuteAsync(request);

        await act.Should().ThrowAsync<DuplicateBarcodeException>();
        products.Added.Should().BeEmpty("a rejected create must not persist the product");
    }

    [Fact]
    public async Task Create_WithNullBarcode_Succeeds()
    {
        var products = new FakeProductRepository();
        var useCase = new CreateProductUseCase(products);

        var request = new CreateProductRequest("แอปเปิ้ล", "https://example.com/apple.jpg", 60m, null, 5, null);

        var product = await useCase.ExecuteAsync(request);

        products.Added.Should().ContainSingle().Which.Should().BeSameAs(product);
    }

    [Fact]
    public async Task Update_WithBarcodeUsedByAnotherProduct_ThrowsDuplicateBarcodeException()
    {
        var mango = NewProduct("มะม่วง", "8850000000012");
        var apple = NewProduct("แอปเปิ้ล", "8850000000099");
        var products = new FakeProductRepository(mango, apple);
        var useCase = new UpdateProductUseCase(products);

        var request = new UpdateProductRequest(
            apple.Id, "แอปเปิ้ล", "https://example.com/apple.jpg", 60m, "8850000000012", 5, null);

        var act = async () => await useCase.ExecuteAsync(request);

        await act.Should().ThrowAsync<DuplicateBarcodeException>();
        apple.Barcode.Should().Be("8850000000099", "a rejected update must not partially mutate the entity");
    }

    [Fact]
    public async Task Update_KeepingItsOwnUnchangedBarcode_Succeeds()
    {
        var mango = NewProduct("มะม่วง", "8850000000012");
        var products = new FakeProductRepository(mango);
        var useCase = new UpdateProductUseCase(products);

        var request = new UpdateProductRequest(
            mango.Id, "มะม่วง", "https://example.com/mango.jpg", 50m, "8850000000012", 8, null);

        var product = await useCase.ExecuteAsync(request);

        product.Price.Should().Be(50m);
        product.Barcode.Should().Be("8850000000012");
    }

    private sealed class FakeProductRepository : IProductRepository
    {
        private readonly Dictionary<Guid, Product> _products;

        public FakeProductRepository(params Product[] products) => _products = products.ToDictionary(p => p.Id);

        public List<Product> Added { get; } = [];

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
            Task.FromResult(_products.GetValueOrDefault(id));

        public Task<IReadOnlyList<Product>> SearchAsync(
            string? search, string? barcode, bool lowStockOnly, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<Product>>(_products.Values.ToList());

        public Task<PagedResult<Product>> SearchPagedAsync(
            string? search, string? barcode, bool lowStockOnly, PageRequest page,
            CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");

        public Task<bool> TryDecreaseStockAsync(Guid productId, int quantity, CancellationToken ct = default) =>
            throw new NotSupportedException("Not exercised by these tests.");

        public Task<bool> BarcodeExistsAsync(string barcode, Guid? excludeProductId, CancellationToken ct = default) =>
            Task.FromResult(_products.Values.Any(p => p.Barcode == barcode && p.Id != excludeProductId));

        public Task AddAsync(Product product, CancellationToken ct = default)
        {
            _products[product.Id] = product;
            Added.Add(product);
            return Task.CompletedTask;
        }

        public Task UpdateAsync(Product product, CancellationToken ct = default) => Task.CompletedTask;

        public Task DeleteAsync(Product product, CancellationToken ct = default)
        {
            _products.Remove(product.Id);
            return Task.CompletedTask;
        }
    }
}
