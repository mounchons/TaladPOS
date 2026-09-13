using System.Text.Json;

namespace TaladPOS.TestData;

internal sealed record CatalogImage(string Path, int Width, int Height);

internal sealed record CatalogImages(CatalogImage Large, CatalogImage Small);

internal sealed record CatalogProduct(int Id, string Slug, string NameTh, string NameEn, CatalogImages Images);

internal sealed record CatalogFile(int SchemaVersion, IReadOnlyList<CatalogProduct> Fruits);

/// <summary>Reads docs/image/product/products.json and publishes its images to the web app.</summary>
internal static class ProductCatalog
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public static IReadOnlyList<CatalogProduct> Load(string path)
    {
        if (!File.Exists(path))
        {
            throw new FileNotFoundException($"Product catalog not found: {path}");
        }

        using var stream = File.OpenRead(path);
        var file = JsonSerializer.Deserialize<CatalogFile>(stream, JsonOptions);

        if (file?.Fruits is not { Count: > 0 } products)
        {
            throw new InvalidDataException($"{path} has no entries under \"fruits\".");
        }

        var invalid = products
            .Where(p => string.IsNullOrWhiteSpace(p.Slug) || string.IsNullOrWhiteSpace(p.NameTh)
                || p.Images?.Small?.Path is null || p.Images.Large?.Path is null)
            .Select(p => p.Id)
            .ToList();
        if (invalid.Count > 0)
        {
            throw new InvalidDataException(
                $"{path}: entries {string.Join(", ", invalid)} need slug, nameTh, images.small.path and images.large.path.");
        }

        var duplicates = products.GroupBy(p => p.Slug).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
        {
            throw new InvalidDataException($"{path}: duplicate slugs {string.Join(", ", duplicates)}.");
        }

        return products;
    }

    /// <summary>
    /// Copies both sizes of every product image into <paramref name="outputDir"/>
    /// side by side, so the small image's URL is all a product needs to store:
    /// the large one is the same name with -512 swapped for -1024.
    /// Returns slug -> small image URL.
    /// </summary>
    public static IReadOnlyDictionary<string, string> PublishImages(
        IReadOnlyList<CatalogProduct> products, string repoRoot, string outputDir, string imageBaseUrl)
    {
        Directory.CreateDirectory(outputDir);

        var missing = new List<string>();
        var urls = new Dictionary<string, string>();

        foreach (var product in products)
        {
            foreach (var image in new[] { product.Images.Small, product.Images.Large })
            {
                var source = Path.GetFullPath(Path.Combine(repoRoot, image.Path));
                if (!File.Exists(source))
                {
                    missing.Add(image.Path);
                    continue;
                }

                var target = Path.Combine(outputDir, Path.GetFileName(source));
                if (!IsUpToDate(source, target))
                {
                    File.Copy(source, target, overwrite: true);
                }
            }

            urls[product.Slug] = $"{imageBaseUrl}/images/products/{Path.GetFileName(product.Images.Small.Path)}";
        }

        if (missing.Count > 0)
        {
            throw new FileNotFoundException($"Missing product images: {string.Join(", ", missing)}");
        }

        return urls;
    }

    private static bool IsUpToDate(string source, string target)
    {
        var targetInfo = new FileInfo(target);
        if (!targetInfo.Exists)
        {
            return false;
        }

        var sourceInfo = new FileInfo(source);
        return targetInfo.Length == sourceInfo.Length && targetInfo.LastWriteTimeUtc >= sourceInfo.LastWriteTimeUtc;
    }
}
