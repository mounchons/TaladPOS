using FluentAssertions;
using TaladPOS.Application.Common;
using Xunit;

namespace TaladPOS.Application.Tests.Common;

/// <summary>
/// tasks.md T091 - the paging contract from contracts/products.md and
/// contracts/sales.md, tested at the Application layer.
///
/// These run without PostgreSQL by construction (constitution Principle III):
/// the whole point of putting validation and the page arithmetic in
/// <see cref="PageRequest"/> / <see cref="PagedResult{T}"/> rather than in the
/// controllers is that the rules are decidable from the inputs alone. The
/// database only ever supplies a count and a slice.
/// </summary>
public class PaginationTests
{
    // ---------------------------------------------------------------- defaults

    /// <summary>
    /// contracts/*.md: omitting both parameters means page 1, 20 per page.
    /// A client that sends nothing must get a well-defined first page rather
    /// than the whole table.
    /// </summary>
    [Fact]
    public void TryCreate_WithNoValues_UsesTheDocumentedDefaults()
    {
        PageRequest.TryCreate(null, null, out var request).Should().BeTrue();

        request.Page.Should().Be(1);
        request.PageSize.Should().Be(20);
        request.Skip.Should().Be(0);
    }

    // -------------------------------------------------------------- rejections

    /// <summary>
    /// contracts/*.md: out-of-range values are rejected with 400
    /// invalid_pagination, never clamped. Silently clamping would let a client
    /// that asked for 500 rows believe the 100 it got were all of them.
    /// </summary>
    [Theory]
    [InlineData(0, 20)]     // page below the first page
    [InlineData(-1, 20)]
    [InlineData(1, 0)]      // a page of nothing is not a page
    [InlineData(1, -5)]
    [InlineData(1, 101)]    // one past the documented ceiling
    [InlineData(1, 1000)]
    public void TryCreate_WithValuesOutsideTheDocumentedRange_IsRejected(int page, int pageSize)
    {
        PageRequest.TryCreate(page, pageSize, out _).Should().BeFalse();
    }

    /// <summary>The ceiling itself is valid - the boundary belongs inside.</summary>
    [Fact]
    public void TryCreate_AtTheMaximumPageSize_IsAccepted()
    {
        PageRequest.TryCreate(1, PageRequest.MaxPageSize, out var request).Should().BeTrue();
        request.PageSize.Should().Be(100);
    }

    [Fact]
    public void Skip_OnALaterPage_SkipsEveryEarlierPage()
    {
        PageRequest.TryCreate(4, 25, out var request).Should().BeTrue();
        request.Skip.Should().Be(75);
    }

    // ------------------------------------------------------------- TotalPages

    [Theory]
    [InlineData(0, 20, 0)]    // nothing to show is zero pages, not one empty one
    [InlineData(1, 20, 1)]
    [InlineData(20, 20, 1)]   // exactly full - no phantom second page
    [InlineData(21, 20, 2)]
    [InlineData(137, 20, 7)]  // the worked example in contracts/products.md
    [InlineData(412, 20, 21)] // the worked example in contracts/sales.md
    public void TotalPages_RoundsUpToCoverThePartialLastPage(int totalCount, int pageSize, int expected)
    {
        var result = new PagedResult<string>([], 1, pageSize, totalCount);

        result.TotalPages.Should().Be(expected);
    }

    // ------------------------------------------------- past the last page

    /// <summary>
    /// quickstart.md 7.2 #7: asking for a page beyond the end is an empty page,
    /// not a 404 - and TotalCount must still report the true size so the client
    /// can navigate back. A 404 here would make a stale bookmark look like a
    /// deleted resource.
    /// </summary>
    [Fact]
    public void PagedResult_PastTheLastPage_IsEmptyButStillReportsTheTrueTotal()
    {
        var result = new PagedResult<string>([], Page: 99, PageSize: 20, TotalCount: 137);

        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(137);
        result.TotalPages.Should().Be(7);
        result.Page.Should().Be(99);
    }

    /// <summary>
    /// contracts/*.md: TotalCount is counted after filtering. If it reported the
    /// unfiltered table size the pager would offer pages that render empty.
    /// </summary>
    [Fact]
    public void PagedResult_CarriesTheFilteredCount_NotThePageLength()
    {
        var pageOfTwo = new PagedResult<string>(["มะม่วง", "มังคุด"], Page: 1, PageSize: 2, TotalCount: 5);

        pageOfTwo.Items.Should().HaveCount(2);
        pageOfTwo.TotalCount.Should().Be(5, "the filter matched five rows even though this page shows two");
        pageOfTwo.TotalPages.Should().Be(3);
    }

    /// <summary>
    /// The envelope must survive a round trip through the projection every
    /// controller does (domain entity -> DTO) without losing its metadata.
    /// </summary>
    [Fact]
    public void Map_ProjectsItemsAndKeepsThePagingMetadata()
    {
        var source = new PagedResult<int>([1, 2, 3], Page: 2, PageSize: 3, TotalCount: 10);

        var mapped = source.Map(n => n.ToString());

        mapped.Items.Should().Equal("1", "2", "3");
        mapped.Page.Should().Be(2);
        mapped.PageSize.Should().Be(3);
        mapped.TotalCount.Should().Be(10);
        mapped.TotalPages.Should().Be(4);
    }
}
