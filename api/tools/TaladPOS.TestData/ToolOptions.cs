namespace TaladPOS.TestData;

internal sealed class ToolOptions
{
    /// <summary>The only database the tool resets unless --allow-any-database is passed.</summary>
    public const string DevDatabaseName = "taladpos";

    // Same default as appsettings.Development.json; the same environment
    // variable the API reads overrides it.
    private const string DefaultConnectionString =
        "Host=localhost;Port=5432;Database=taladpos;Username=taladpos;Password=taladpos_dev_password";

    public const string Usage = """
        Usage: dotnet run --project api/tools/TaladPOS.TestData -- [options]

        Wipes products, promotions, members and sales in the dev database and
        rebuilds the QA dataset. Staff accounts are kept.

        Options:
          --connection <string>     PostgreSQL connection string
                                    (default: env ConnectionStrings__TaladPOSDb, then the dev database)
          --products-json <path>    Product catalog (default: docs/image/product/products.json)
          --image-base-url <url>    Origin the web app serves /images/products from
                                    (default: http://localhost:3000)
          --repo-root <path>        Repository root (default: auto-detected)
          --allow-any-database      Allow resetting a database not named 'taladpos'
          --help                    Show this help
        """;

    public required string ConnectionString { get; init; }

    public required string RepoRoot { get; init; }

    public required string ProductsJsonPath { get; init; }

    public required string ImageBaseUrl { get; init; }

    public bool AllowAnyDatabase { get; init; }

    public bool ShowHelp { get; init; }

    /// <summary>Next.js serves web/public at the site root, so files here are reachable at /images/products/.</summary>
    public string ImageOutputDir => Path.Combine(RepoRoot, "web", "public", "images", "products");

    public static ToolOptions Parse(string[] args)
    {
        string? connection = null;
        string? productsJson = null;
        string? imageBaseUrl = null;
        string? repoRoot = null;
        var allowAnyDatabase = false;
        var showHelp = false;

        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--connection":
                    connection = ValueAfter(args, ref i);
                    break;
                case "--products-json":
                    productsJson = ValueAfter(args, ref i);
                    break;
                case "--image-base-url":
                    imageBaseUrl = ValueAfter(args, ref i);
                    break;
                case "--repo-root":
                    repoRoot = ValueAfter(args, ref i);
                    break;
                case "--allow-any-database":
                    allowAnyDatabase = true;
                    break;
                case "--help" or "-h":
                    showHelp = true;
                    break;
                default:
                    throw new ArgumentException($"Unknown option '{args[i]}'.");
            }
        }

        var root = Path.GetFullPath(repoRoot ?? FindRepoRoot());

        return new ToolOptions
        {
            ConnectionString = connection
                ?? Environment.GetEnvironmentVariable("ConnectionStrings__TaladPOSDb")
                ?? DefaultConnectionString,
            RepoRoot = root,
            ProductsJsonPath = Path.GetFullPath(
                productsJson ?? Path.Combine(root, "docs", "image", "product", "products.json")),
            ImageBaseUrl = (imageBaseUrl ?? "http://localhost:3000").TrimEnd('/'),
            AllowAnyDatabase = allowAnyDatabase,
            ShowHelp = showHelp,
        };
    }

    private static string ValueAfter(string[] args, ref int i)
    {
        if (i + 1 >= args.Length)
        {
            throw new ArgumentException($"Option '{args[i]}' needs a value.");
        }

        return args[++i];
    }

    // `dotnet run` keeps the caller's working directory while the binary sits
    // under bin/, so both starting points are tried.
    private static string FindRepoRoot()
    {
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                if (File.Exists(Path.Combine(dir.FullName, "api", "TaladPOS.sln")))
                {
                    return dir.FullName;
                }
            }
        }

        throw new ArgumentException(
            "Could not find the repository root (a folder containing api/TaladPOS.sln). Pass --repo-root.");
    }
}
