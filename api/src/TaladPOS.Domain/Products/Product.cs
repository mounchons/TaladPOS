namespace TaladPOS.Domain.Products;

/// <summary>
/// A sellable product (FR-001-FR-006, FR-015-FR-018). Sold by whole units
/// only - no weight-based pricing in this version (clarify session #1).
/// </summary>
public class Product
{
    public const int DefaultLowStockThreshold = 5;

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string ImageUrl { get; private set; }
    public decimal Price { get; private set; }
    public string? Barcode { get; private set; }
    public int StockQuantity { get; private set; }
    public int LowStockThreshold { get; private set; }

    public bool IsOutOfStock => StockQuantity == 0;

    public bool IsLowStock => StockQuantity > 0 && StockQuantity <= LowStockThreshold;

    // EF Core materialization constructor.
    private Product()
    {
        Name = string.Empty;
        ImageUrl = string.Empty;
    }

    public Product(
        string name,
        string imageUrl,
        decimal price,
        string? barcode,
        int stockQuantity,
        int? lowStockThreshold = null)
    {
        Id = Guid.NewGuid();
        Name = name;
        ImageUrl = imageUrl;
        Barcode = barcode;

        SetPrice(price);
        SetStockQuantity(stockQuantity);
        SetLowStockThreshold(lowStockThreshold ?? DefaultLowStockThreshold);

        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("ImageUrl is required.", nameof(imageUrl));
        }
    }

    public void SetPrice(decimal price)
    {
        if (price <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(price), price, "Price must be greater than 0.");
        }

        Price = price;
    }

    public void SetStockQuantity(int stockQuantity)
    {
        if (stockQuantity < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(stockQuantity), stockQuantity, "StockQuantity cannot be negative.");
        }

        StockQuantity = stockQuantity;
    }

    public void SetLowStockThreshold(int lowStockThreshold)
    {
        if (lowStockThreshold < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lowStockThreshold), lowStockThreshold, "LowStockThreshold cannot be negative.");
        }

        LowStockThreshold = lowStockThreshold;
    }

    public void Rename(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        Name = name;
    }

    public void SetImageUrl(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            throw new ArgumentException("ImageUrl is required.", nameof(imageUrl));
        }

        ImageUrl = imageUrl;
    }

    public void SetBarcode(string? barcode) => Barcode = barcode;

    /// <summary>
    /// In-memory guard used by domain unit tests. The authoritative,
    /// concurrency-safe enforcement for multi-register sales happens as an
    /// atomic conditional SQL UPDATE in the infrastructure layer
    /// (research.md #2) - this method is not what protects against races.
    /// </summary>
    public void DecreaseStock(int quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(quantity), quantity, "Quantity must be greater than 0.");
        }

        if (quantity > StockQuantity)
        {
            throw new InsufficientStockException(Id, quantity, StockQuantity);
        }

        StockQuantity -= quantity;
    }
}
