using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Api.Migrations;

[DbContext(typeof(MealPlannerDbContext))]
[Migration("20260326213200_AddPlanningSchema")]
public partial class AddPlanningSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "weekly_plan",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                week_start = table.Column<DateOnly>(type: "date", nullable: false),
                status = table.Column<string>(type: "text", nullable: false, defaultValue: "draft"),
                notes = table.Column<string>(type: "text", nullable: true),
                generated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_weekly_plan", x => x.id);
                table.CheckConstraint("ck_weekly_plan_status", "status IN ('draft', 'confirmed')");
                table.ForeignKey(
                    name: "FK_weekly_plan_user_profile_user_id",
                    column: x => x.user_id,
                    principalTable: "user_profile",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "plan_slot",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                plan_id = table.Column<Guid>(type: "uuid", nullable: false),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                slot_date = table.Column<DateOnly>(type: "date", nullable: false),
                meal_type = table.Column<string>(type: "text", nullable: false),
                servings = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                rationale = table.Column<string>(type: "text", nullable: true),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_plan_slot", x => x.id);
                table.CheckConstraint("ck_plan_slot_meal_type", "meal_type IN ('breakfast', 'lunch', 'dinner', 'snack')");
                table.CheckConstraint("ck_plan_slot_servings_positive", "servings > 0");
                table.ForeignKey(
                    name: "FK_plan_slot_recipe_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipe",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_plan_slot_weekly_plan_plan_id",
                    column: x => x.plan_id,
                    principalTable: "weekly_plan",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_plan_slot_plan_id",
            table: "plan_slot",
            column: "plan_id");

        migrationBuilder.CreateIndex(
            name: "IX_plan_slot_recipe_id",
            table: "plan_slot",
            column: "recipe_id");

        migrationBuilder.CreateIndex(
            name: "IX_weekly_plan_user_id_week_start",
            table: "weekly_plan",
            columns: new[] { "user_id", "week_start" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "plan_slot");

        migrationBuilder.DropTable(
            name: "weekly_plan");
    }
}
