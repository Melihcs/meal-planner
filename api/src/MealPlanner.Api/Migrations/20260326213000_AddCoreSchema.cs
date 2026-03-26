using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Api.Migrations;

[DbContext(typeof(MealPlannerDbContext))]
[Migration("20260326213000_AddCoreSchema")]
public partial class AddCoreSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AlterDatabase()
            .Annotation("Npgsql:PostgresExtension:pgcrypto", ",,");

        migrationBuilder.CreateTable(
            name: "ingredient",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                name = table.Column<string>(type: "text", nullable: false),
                edamam_food_id = table.Column<string>(type: "text", nullable: true),
                category = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_ingredient", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "user_profile",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                display_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                avatar_url = table.Column<string>(type: "text", nullable: true),
                created_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
                updated_at = table.Column<DateTime>(type: "timestamp with time zone", nullable: false, defaultValueSql: "now()"),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_user_profile", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_ingredient_edamam_food_id",
            table: "ingredient",
            column: "edamam_food_id",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "ingredient");

        migrationBuilder.DropTable(
            name: "user_profile");
    }
}
