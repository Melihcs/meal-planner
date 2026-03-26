using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Api.Migrations;

[DbContext(typeof(MealPlannerDbContext))]
[Migration("20260326213300_AddShoppingSchema")]
public partial class AddShoppingSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "shopping_list",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                plan_id = table.Column<Guid>(type: "uuid", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shopping_list", x => x.id);
                table.ForeignKey(
                    name: "FK_shopping_list_user_profile_user_id",
                    column: x => x.user_id,
                    principalTable: "user_profile",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_shopping_list_weekly_plan_plan_id",
                    column: x => x.plan_id,
                    principalTable: "weekly_plan",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateTable(
            name: "shopping_item",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                list_id = table.Column<Guid>(type: "uuid", nullable: false),
                ingredient_id = table.Column<Guid>(type: "uuid", nullable: true),
                name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: true),
                unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                category = table.Column<string>(type: "text", nullable: true),
                is_checked = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                is_manual = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_shopping_item", x => x.id);
                table.ForeignKey(
                    name: "FK_shopping_item_ingredient_ingredient_id",
                    column: x => x.ingredient_id,
                    principalTable: "ingredient",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
                table.ForeignKey(
                    name: "FK_shopping_item_shopping_list_list_id",
                    column: x => x.list_id,
                    principalTable: "shopping_list",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_shopping_item_ingredient_id",
            table: "shopping_item",
            column: "ingredient_id");

        migrationBuilder.CreateIndex(
            name: "IX_shopping_item_list_id",
            table: "shopping_item",
            column: "list_id");

        migrationBuilder.CreateIndex(
            name: "IX_shopping_list_plan_id",
            table: "shopping_list",
            column: "plan_id");

        migrationBuilder.CreateIndex(
            name: "IX_shopping_list_user_id",
            table: "shopping_list",
            column: "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "shopping_item");

        migrationBuilder.DropTable(
            name: "shopping_list");
    }
}
