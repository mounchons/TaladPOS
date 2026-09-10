using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using TaladPOS.Api.Middleware;
using TaladPOS.Application.Auth;
using TaladPOS.Application.Common;
using TaladPOS.Application.Members;
using TaladPOS.Application.Products;
using TaladPOS.Application.Promotions;
using TaladPOS.Application.Reports;
using TaladPOS.Application.Sales;
using TaladPOS.Domain.Staff;
using TaladPOS.Infrastructure.Auth;
using TaladPOS.Infrastructure.Persistence;
using TaladPOS.Infrastructure.Repositories;
using TaladPOS.Infrastructure.Seed;

var builder = WebApplication.CreateBuilder(args);

// --- Persistence (constitution Principle II: EF Core + PostgreSQL) ---
builder.Services.AddDbContext<TaladPOSDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("TaladPOSDb")));

// --- Auth (research.md #1: JWT bearer, standalone PasswordHasher) ---
builder.Services.AddScoped<IStaffRepository, StaffRepository>();
builder.Services.AddScoped<StaffAuthenticator>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// --- Sales (US1: FR-001-FR-006, FR-016, FR-023) ---
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ISaleRepository, SaleRepository>();
builder.Services.AddScoped<IUnitOfWork, EfUnitOfWork>();
builder.Services.AddScoped<CompleteSaleUseCase>();

// --- Stock management (US3: FR-015, FR-017, FR-018) ---
builder.Services.AddScoped<CreateProductUseCase>();
builder.Services.AddScoped<UpdateProductUseCase>();
builder.Services.AddScoped<DeleteProductUseCase>();

// --- Membership (US4: FR-010-FR-014) ---
builder.Services.AddScoped<IMemberRepository, MemberRepository>();
builder.Services.AddScoped<RegisterMemberUseCase>();

// --- Promotions (US5: FR-019-FR-022) ---
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<CreatePromotionUseCase>();
builder.Services.AddScoped<UpdatePromotionUseCase>();
builder.Services.AddScoped<DeletePromotionUseCase>();

// --- Sales history & reports (US6: FR-024-FR-028) ---
builder.Services.AddScoped<GetSalesHistoryQuery>();
builder.Services.AddScoped<GetDailyOrMonthlySalesReportQuery>();
builder.Services.AddScoped<GetBestSellingProductsReportQuery>();
builder.Services.AddScoped<GetSalesByStaffReportQuery>();
builder.Services.AddScoped<GetStockReportQuery>();

var jwtSection = builder.Configuration.GetSection("Jwt");
var signingKey = jwtSection["SigningKey"]
    ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtSection["Issuer"],
            ValidateAudience = true,
            ValidAudience = jwtSection["Audience"],
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
        };
    });

// FR-029: every endpoint requires login by default; Manager-only endpoints
// opt in explicitly with [Authorize(Roles = nameof(StaffRole.Manager))].
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();
});

// `web/` (Next.js) calls `api/` from the browser as a separate origin
// (constitution Principle I: REST-only, no shared process/origin) - CORS
// must explicitly allow it. AllowCredentials is NOT needed: the JWT travels
// as an Authorization header set by our own fetch wrapper, not a cookie.
const string WebAppCorsPolicy = "WebApp";
var webAppOrigin = builder.Configuration["Cors:WebAppOrigin"] ?? "http://localhost:3000";
builder.Services.AddCors(options =>
{
    options.AddPolicy(WebAppCorsPolicy, policy =>
        policy.WithOrigins(webAppOrigin).AllowAnyHeader().AllowAnyMethod());
});

// Promotion.Scope (Item/Bill) travels over the wire as a string per
// contracts/promotions.md, not System.Text.Json's numeric enum default.
builder.Services.AddControllers()
    .AddJsonOptions(options => options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseMiddleware<ErrorHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<TaladPOSDbContext>();
    var authenticator = scope.ServiceProvider.GetRequiredService<StaffAuthenticator>();
    await dbContext.Database.MigrateAsync();
    await DevelopmentSeeder.SeedAsync(dbContext, authenticator);
}

app.UseHttpsRedirection();

app.UseCors(WebAppCorsPolicy);

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();

// Exposed for WebApplicationFactory<Program> in integration tests (contracts/*.md).
public partial class Program
{
}
