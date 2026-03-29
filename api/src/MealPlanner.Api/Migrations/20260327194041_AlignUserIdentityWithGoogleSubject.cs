using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPlanner.Api.Migrations
{
    /// <inheritdoc />
    public partial class AlignUserIdentityWithGoogleSubject : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_weekly_plan_user_profile_user_id",
                table: "weekly_plan");

            migrationBuilder.DropForeignKey(
                name: "FK_shopping_list_user_profile_user_id",
                table: "shopping_list");

            migrationBuilder.DropForeignKey(
                name: "FK_recipe_user_profile_user_id",
                table: "recipe");

            migrationBuilder.Sql("""
                ALTER TABLE user_profile
                ALTER COLUMN id TYPE character varying(255)
                USING id::text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE weekly_plan
                ALTER COLUMN user_id TYPE character varying(255)
                USING user_id::text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE shopping_list
                ALTER COLUMN user_id TYPE character varying(255)
                USING user_id::text;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE recipe
                ALTER COLUMN user_id TYPE character varying(255)
                USING user_id::text;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "id",
                table: "user_profile",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "weekly_plan",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "shopping_list",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);

            migrationBuilder.AlterColumn<string>(
                name: "user_id",
                table: "recipe",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uuid",
                oldNullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_weekly_plan_user_profile_user_id",
                table: "weekly_plan",
                column: "user_id",
                principalTable: "user_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_shopping_list_user_profile_user_id",
                table: "shopping_list",
                column: "user_id",
                principalTable: "user_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_recipe_user_profile_user_id",
                table: "recipe",
                column: "user_id",
                principalTable: "user_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_weekly_plan_user_profile_user_id",
                table: "weekly_plan");

            migrationBuilder.DropForeignKey(
                name: "FK_shopping_list_user_profile_user_id",
                table: "shopping_list");

            migrationBuilder.DropForeignKey(
                name: "FK_recipe_user_profile_user_id",
                table: "recipe");

            migrationBuilder.Sql("""
                ALTER TABLE recipe
                ALTER COLUMN user_id TYPE uuid
                USING user_id::uuid;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE shopping_list
                ALTER COLUMN user_id TYPE uuid
                USING user_id::uuid;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE weekly_plan
                ALTER COLUMN user_id TYPE uuid
                USING user_id::uuid;
                """);

            migrationBuilder.Sql("""
                ALTER TABLE user_profile
                ALTER COLUMN id TYPE uuid
                USING id::uuid;
                """);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "weekly_plan",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "id",
                table: "user_profile",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "shopping_list",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: false);

            migrationBuilder.AlterColumn<Guid>(
                name: "user_id",
                table: "recipe",
                type: "uuid",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255,
                oldNullable: false);

            migrationBuilder.AddForeignKey(
                name: "FK_weekly_plan_user_profile_user_id",
                table: "weekly_plan",
                column: "user_id",
                principalTable: "user_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_shopping_list_user_profile_user_id",
                table: "shopping_list",
                column: "user_id",
                principalTable: "user_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_recipe_user_profile_user_id",
                table: "recipe",
                column: "user_id",
                principalTable: "user_profile",
                principalColumn: "id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
