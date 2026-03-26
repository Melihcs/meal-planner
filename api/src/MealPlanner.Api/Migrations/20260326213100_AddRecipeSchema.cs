using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Api.Migrations;

[DbContext(typeof(MealPlannerDbContext))]
[Migration("20260326213100_AddRecipeSchema")]
public partial class AddRecipeSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "recipe",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                title = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                description = table.Column<string>(type: "text", nullable: true),
                servings = table.Column<int>(type: "integer", nullable: false, defaultValue: 1),
                cuisine = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                cover_colour = table.Column<string>(type: "text", nullable: false, defaultValue: "#E8F5E9"),
                visibility = table.Column<string>(type: "text", nullable: false, defaultValue: "private"),
                prep_time_minutes = table.Column<int>(type: "integer", nullable: true),
                cook_time_minutes = table.Column<int>(type: "integer", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe", x => x.id);
                table.CheckConstraint("ck_recipe_servings_positive", "servings > 0");
                table.CheckConstraint("ck_recipe_visibility", "visibility IN ('private', 'public')");
                table.ForeignKey(
                    name: "FK_recipe_user_profile_user_id",
                    column: x => x.user_id,
                    principalTable: "user_profile",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "recipe_ingredient",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                ingredient_id = table.Column<Guid>(type: "uuid", nullable: false),
                quantity = table.Column<decimal>(type: "numeric(10,3)", precision: 10, scale: 3, nullable: false),
                unit = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                sort_order = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                calories_per_100g = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                protein_g = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                carbs_g = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                fat_g = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                fibre_g = table.Column<decimal>(type: "numeric(8,2)", precision: 8, scale: 2, nullable: true),
                edamam_measure_uri = table.Column<string>(type: "text", nullable: true),
                edamam_food_id = table.Column<string>(type: "text", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_ingredient", x => x.id);
                table.CheckConstraint("ck_recipe_ingredient_quantity_positive", "quantity > 0");
                table.ForeignKey(
                    name: "FK_recipe_ingredient_ingredient_ingredient_id",
                    column: x => x.ingredient_id,
                    principalTable: "ingredient",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Restrict);
                table.ForeignKey(
                    name: "FK_recipe_ingredient_recipe_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipe",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "recipe_step",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                recipe_id = table.Column<Guid>(type: "uuid", nullable: false),
                step_number = table.Column<int>(type: "integer", nullable: false),
                instruction = table.Column<string>(type: "text", nullable: false),
                step_type = table.Column<string>(type: "text", nullable: false),
                timer_seconds = table.Column<int>(type: "integer", nullable: true),
                background_step_id = table.Column<Guid>(type: "uuid", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_recipe_step", x => x.id);
                table.CheckConstraint("ck_recipe_step_step_number_positive", "step_number > 0");
                table.CheckConstraint("ck_recipe_step_timer_positive", "timer_seconds IS NULL OR timer_seconds > 0");
                table.CheckConstraint("ck_recipe_step_timer_required", "step_type <> 'timed' OR timer_seconds IS NOT NULL");
                table.CheckConstraint("ck_recipe_step_type", "step_type IN ('sequential', 'background', 'timed')");
                table.ForeignKey(
                    name: "FK_recipe_step_recipe_recipe_id",
                    column: x => x.recipe_id,
                    principalTable: "recipe",
                    principalColumn: "id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_recipe_step_recipe_step_background_step_id",
                    column: x => x.background_step_id,
                    principalTable: "recipe_step",
                    principalColumn: "id",
                    onDelete: ReferentialAction.SetNull);
            });

        migrationBuilder.CreateIndex(
            name: "IX_recipe_user_id_created_at",
            table: "recipe",
            columns: new[] { "user_id", "created_at" },
            descending: new[] { false, true });

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredient_ingredient_id",
            table: "recipe_ingredient",
            column: "ingredient_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_ingredient_recipe_id",
            table: "recipe_ingredient",
            column: "recipe_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_step_background_step_id",
            table: "recipe_step",
            column: "background_step_id");

        migrationBuilder.CreateIndex(
            name: "IX_recipe_step_recipe_id_step_number",
            table: "recipe_step",
            columns: new[] { "recipe_id", "step_number" },
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "recipe_ingredient");

        migrationBuilder.DropTable(
            name: "recipe_step");

        migrationBuilder.DropTable(
            name: "recipe");
    }
}
