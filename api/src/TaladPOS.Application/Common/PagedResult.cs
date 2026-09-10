namespace TaladPOS.Application.Common;

/// <summary>
/// A validated page request. Lives at the Application layer rather than in the
/// controllers so the rules in contracts/products.md and contracts/sales.md are
/// decidable from the inputs alone and testable without a database
/// (constitution Principle III).
/// </summary>
public sealed class PageRequest
{
    public const int DefaultPage = 1;
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    private PageRequest(int page, int pageSize)
    {
        Page = page;
        PageSize = pageSize;
    }

    public int Page { get; }

    public int PageSize { get; }

    /// <summary>Rows to skip to reach this page. Never negative - Page is at least 1.</summary>
    public int Skip => (Page - 1) * PageSize;

    public int Take => PageSize;

    /// <summary>
    /// Builds a request from the raw query values, or fails.
    ///
    /// Out-of-range values are rejected rather than clamped: a client that
    /// asked for 500 rows and silently received 100 would treat them as the
    /// complete set. The caller turns a false here into
    /// <c>400 {"error":"invalid_pagination"}</c>.
    /// </summary>
    public static bool TryCreate(int? page, int? pageSize, out PageRequest request)
    {
        var p = page ?? DefaultPage;
        var size = pageSize ?? DefaultPageSize;

        if (p < 1 || size < 1 || size > MaxPageSize)
        {
            // Never handed back on the false path; callers must check the bool.
            request = new PageRequest(DefaultPage, DefaultPageSize);
            return false;
        }

        request = new PageRequest(p, size);
        return true;
    }
}

/// <summary>
/// The response envelope shared by every paged endpoint (research.md #11).
///
/// <paramref name="TotalCount"/> is the number of rows matching the filter, not
/// the number in <paramref name="Items"/> and not the size of the table - the
/// pager derives its page count from it, so an unfiltered count would offer
/// pages that render empty.
/// </summary>
public sealed record PagedResult<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount)
{
    /// <summary>
    /// Rounded up so a partial last page still counts. Zero rows is zero pages
    /// rather than one empty one, which keeps "page 1 of 0" out of the UI.
    /// </summary>
    public int TotalPages => TotalCount <= 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    /// <summary>
    /// Projects the items (domain entity to DTO, in practice) while carrying the
    /// paging metadata across unchanged - the controllers all need this and
    /// rebuilding the envelope by hand is where a wrong TotalCount creeps in.
    /// </summary>
    public PagedResult<TOut> Map<TOut>(Func<T, TOut> selector) =>
        new([.. Items.Select(selector)], Page, PageSize, TotalCount);

    public static PagedResult<T> From(IReadOnlyList<T> items, PageRequest request, int totalCount) =>
        new(items, request.Page, request.PageSize, totalCount);
}
