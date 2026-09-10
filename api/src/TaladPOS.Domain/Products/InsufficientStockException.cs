namespace TaladPOS.Domain.Products;

/// <summary>
/// Thrown when a requested quantity exceeds a product's current stock (FR-005).
/// </summary>
public class InsufficientStockException : Exception
{
    public Guid ProductId { get; }

    public InsufficientStockException(Guid productId, int requestedQuantity, int availableQuantity)
        : base(
            $"Cannot decrease stock for product {productId} by {requestedQuantity}: "
            + $"only {availableQuantity} available.")
    {
        ProductId = productId;
    }
}
