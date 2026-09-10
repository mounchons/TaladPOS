using System.Reflection;
using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
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

// OpenAPI/Swagger (tasks.md T077). Every endpoint sits behind the
// RequireAuthenticatedUser fallback policy above, so the security definition
// is not decoration: without it Swagger UI has no Authorize button and every
// "Try it out" returns 401, which makes the document useless for exploring
// the API described in contracts/*.md.
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "TaladPOS API",
        Version = "v1",
        Description =
            "REST API สำหรับระบบ POS ร้านค้าเดี่ยว (specs/001-single-store-pos). "
            + "`web/` เรียกใช้ผ่าน endpoint เหล่านี้เท่านั้น ไม่เชื่อมต่อฐานข้อมูลโดยตรง "
            + "(constitution Principle I). ทุก endpoint ต้องล็อกอินก่อน (FR-029) — "
            + "เรียก POST /api/auth/login เพื่อรับ JWT แล้วกด Authorize ด้านบน "
            + "ส่วน endpoint ที่สงวนไว้สำหรับผู้จัดการจะตอบ 403 เมื่อเรียกด้วยบัญชี Cashier",
    });

    // Swashbuckle's default schemaId is the bare type name, so the two
    // controller-nested `StaffSummaryDto` records (AuthController and
    // SalesController) collided and made /swagger/v1/swagger.json fail
    // outright with a 500. Qualifying nested types with their declaring
    // controller keeps ids unique and still readable (AuthStaffSummaryDto,
    // SalesStaffSummaryDto), and stops the next same-named DTO from
    // re-breaking the document.
    options.CustomSchemaIds(type => type.DeclaringType is null
        ? type.Name
        : $"{type.DeclaringType.Name.Replace("Controller", string.Empty)}{type.Name}");

    const string bearerScheme = "Bearer";
    options.AddSecurityDefinition(bearerScheme, new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "วาง JWT ที่ได้จาก POST /api/auth/login (ไม่ต้องพิมพ์คำว่า \"Bearer\" นำหน้า)",
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        [new OpenApiSecurityScheme
        {
            Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = bearerScheme },
        }] = Array.Empty<string>(),
    });

    // Surfaces the `<summary>` on each controller action - those already
    // cite the contracts/*.md section and FR ids they implement, so the
    // generated document stays traceable back to the spec.
    var xmlPath = Path.Combine(AppContext.BaseDirectory, $"{Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

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
