using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TaladPOS.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddConditionalPromotions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsGift",
                table: "sale_line_items",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "conditional_promotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Reward_Kind = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    Reward_GiftProductId = table.Column<Guid>(type: "uuid", nullable: true),
                    Reward_GiftQuantity = table.Column<int>(type: "integer", nullable: true),
                    Reward_DiscountPercentage = table.Column<decimal>(type: "numeric(5,2)", nullable: true),
                    AppliesToMembersOnly = table.Column<bool>(type: "boolean", nullable: false),
                    StartDate = table.Column<DateOnly>(type: "date", nullable: false),
                    EndDate = table.Column<DateOnly>(type: "date", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conditional_promotions", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "sale_applied_promotions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    SaleId = table.Column<Guid>(type: "uuid", nullable: false),
                    PromotionId = table.Column<Guid>(type: "uuid", nullable: false),
                    DescriptionSnapshot = table.Column<string>(type: "character varying(400)", maxLength: 400, nullable: false),
                    SetCount = table.Column<int>(type: "integer", nullable: false),
                    DiscountAmount = table.Column<decimal>(type: "numeric(12,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sale_applied_promotions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sale_applied_promotions_sales_SaleId",
                        column: x => x.SaleId,
                        principalTable: "sales",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "conditional_promotion_lines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ProductId = table.Column<Guid>(type: "uuid", nullable: false),
                    MinimumQuantity = table.Column<int>(type: "integer", nullable: false),
                    SortOrder = table.Column<int>(type: "integer", nullable: false),
                    ConditionalPromotionId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_conditional_promotion_lines", x => x.Id);
                    table.ForeignKey(
                        name: "FK_conditional_promotion_lines_conditional_promotions_Conditio~",
                        column: x => x.ConditionalPromotionId,
                        principalTable: "conditional_promotions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_conditional_promotion_lines_ConditionalPromotionId_ProductId",
                table: "conditional_promotion_lines",
                columns: new[] { "ConditionalPromotionId", "ProductId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sale_applied_promotions_SaleId",
                table: "sale_applied_promotions",
                column: "SaleId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "conditional_promotion_lines");

            migrationBuilder.DropTable(
                name: "sale_applied_promotions");

            migrationBuilder.DropTable(
                name: "conditional_promotions");

            migrationBuilder.DropColumn(
                name: "IsGift",
                table: "sale_line_items");
        }
    }
}
