namespace TaladPOS.Domain.Products;

/// <summary>Thrown when a product's barcode collides with another existing product's (FR-015).</summary>
public class DuplicateBarcodeException : Exception
{
    public string Barcode { get; }

    public DuplicateBarcodeException(string barcode)
        : base($"A product with barcode '{barcode}' already exists.")
    {
        Barcode = barcode;
    }
}
