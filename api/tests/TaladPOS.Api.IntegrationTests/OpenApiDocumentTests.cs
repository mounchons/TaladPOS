using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.Swagger;
using TaladPOS.Api.IntegrationTests.Infrastructure;

namespace TaladPOS.Api.IntegrationTests;

/// <summary>
/// tasks.md T077 - guards the generated OpenAPI document.
///
/// This test exists because the document was broken and nothing noticed:
/// AuthController and SalesController each declared a nested
/// <c>StaffSummaryDto</c>, both of which Swashbuckle mapped to the schemaId
/// "StaffSummaryDto", so GET /swagger/v1/swagger.json returned 500 rather
/// than a document. Generation is resolved through <see cref="ISwaggerProvider"/>
/// rather than over HTTP because Swagger UI is only mapped in Development
/// (Program.cs) - the provider is registered in every environment, and
/// generation is the part that actually breaks.
/// </summary>
public sealed class OpenApiDocumentTests : ApiTestBase
{
    public OpenApiDocumentTests(TaladPOSApiFactory factory) : base(factory)
    {
    }

    [Fact]
    public void SwaggerDocument_GeneratesWithoutSchemaIdCollisions()
    {
        var document = GenerateDocument();

        document.Info.Title.Should().Be("TaladPOS API");
        document.Info.Version.Should().Be("v1");

        // Every controller-nested DTO must have earned a distinct schemaId -
        // one entry per type, no silent overwrites.
        document.Components.Schemas.Should().ContainKeys("AuthStaffSummaryDto", "SalesStaffSummaryDto");
    }

    /// <summary>
    /// The paged envelopes are the second way this document has broken: both
    /// closed generics arrive as the bare name "PagedResult`1", so without the
    /// generic branch in CustomSchemaIds (Program.cs) one silently overwrites
    /// the other and generation throws. Asserting both keys pins the fix.
    /// </summary>
    [Fact]
    public void SwaggerDocument_GivesEachPagedEnvelopeItsOwnSchema()
    {
        var document = GenerateDocument();

        document.Components.Schemas.Should().ContainKeys(
            "PagedResultOfProductsProductDto",
            "PagedResultOfSalesSaleDto");

        // The envelope's own fields have to be in the document too - a client
        // generated from it needs TotalCount to build a pager at all.
        var envelope = document.Components.Schemas["PagedResultOfProductsProductDto"];
        envelope.Properties.Keys.Should().Contain(
            ["items", "page", "pageSize", "totalCount", "totalPages"]);
    }

    /// <summary>
    /// Every endpoint sits behind the RequireAuthenticatedUser fallback
    /// policy (FR-029), so the document must advertise the bearer scheme -
    /// otherwise Swagger UI renders no Authorize button and every request
    /// tried from it comes back 401.
    /// </summary>
    [Fact]
    public void SwaggerDocument_DeclaresJwtBearerSecurity()
    {
        var document = GenerateDocument();

        document.Components.SecuritySchemes.Should().ContainKey("Bearer");
        var scheme = document.Components.SecuritySchemes["Bearer"];
        scheme.Type.Should().Be(SecuritySchemeType.Http);
        scheme.Scheme.Should().Be("bearer");
        scheme.BearerFormat.Should().Be("JWT");

        document.SecurityRequirements.Should().NotBeEmpty();
    }

    /// <summary>
    /// The document is the machine-readable face of contracts/*.md - if a
    /// contract's endpoint is missing here, `web/` and any other REST client
    /// have no way to discover it.
    /// </summary>
    [Fact]
    public void SwaggerDocument_CoversTheContractEndpoints()
    {
        var document = GenerateDocument();

        document.Paths.Should().ContainKeys(
            "/api/v1/auth/login",
            "/api/v1/products",
            "/api/v1/sales",
            "/api/v1/members",
            "/api/v1/promotions",
            "/api/v1/reports/sales");
    }

    private OpenApiDocument GenerateDocument()
    {
        using var scope = Factory.Services.CreateScope();
        var provider = scope.ServiceProvider.GetRequiredService<ISwaggerProvider>();
        return provider.GetSwagger("v1");
    }
}
