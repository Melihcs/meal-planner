using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Api.Migrations;

[DbContext(typeof(MealPlannerDbContext))]
[Migration("20260326213400_AddNutrientOrganMappingSchema")]
public partial class AddNutrientOrganMappingSchema : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "nutrient_organ_mapping",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false, defaultValueSql: "gen_random_uuid()"),
                nutrient_name = table.Column<string>(type: "text", nullable: false),
                organ_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                body_system = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                impact_description = table.Column<string>(type: "text", nullable: true),
                source_citation = table.Column<string>(type: "text", nullable: true),
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_nutrient_organ_mapping", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_nutrient_organ_mapping_nutrient_name",
            table: "nutrient_organ_mapping",
            column: "nutrient_name",
            unique: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "nutrient_organ_mapping");
    }
}
