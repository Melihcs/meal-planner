namespace MealPlanner.Api.Infrastructure.Persistence.Configurations;

using MealPlanner.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipe");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.UserId)
            .HasColumnName("user_id");

        builder.Property(entity => entity.Title)
            .HasColumnName("title")
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(entity => entity.Description)
            .HasColumnName("description");

        builder.Property(entity => entity.Servings)
            .HasColumnName("servings")
            .HasDefaultValue(1);

        builder.Property(entity => entity.Cuisine)
            .HasColumnName("cuisine")
            .HasMaxLength(100);

        builder.Property(entity => entity.CoverColour)
            .HasColumnName("cover_colour")
            .HasDefaultValue("#E8F5E9")
            .IsRequired();

        builder.Property(entity => entity.Visibility)
            .HasColumnName("visibility")
            .HasDefaultValue("private")
            .IsRequired();

        builder.Property(entity => entity.PrepTimeMinutes)
            .HasColumnName("prep_time_minutes");

        builder.Property(entity => entity.CookTimeMinutes)
            .HasColumnName("cook_time_minutes");

        builder.Property(entity => entity.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.Property(entity => entity.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        builder.HasIndex(entity => new { entity.UserId, entity.CreatedAt })
            .IsDescending(false, true);

        builder.HasOne(entity => entity.User)
            .WithMany(user => user.Recipes)
            .HasForeignKey(entity => entity.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_recipe_servings_positive", "servings > 0");
            tableBuilder.HasCheckConstraint(
                "ck_recipe_visibility",
                "visibility IN ('private', 'public')");
        });
    }
}

internal sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("recipe_ingredient");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.RecipeId)
            .HasColumnName("recipe_id");

        builder.Property(entity => entity.IngredientId)
            .HasColumnName("ingredient_id");

        builder.Property(entity => entity.Quantity)
            .HasColumnName("quantity")
            .HasPrecision(10, 3);

        builder.Property(entity => entity.Unit)
            .HasColumnName("unit")
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(entity => entity.SortOrder)
            .HasColumnName("sort_order")
            .HasDefaultValue(0);

        builder.Property(entity => entity.CaloriesPer100G)
            .HasColumnName("calories_per_100g")
            .HasPrecision(8, 2);

        builder.Property(entity => entity.ProteinG)
            .HasColumnName("protein_g")
            .HasPrecision(8, 2);

        builder.Property(entity => entity.CarbsG)
            .HasColumnName("carbs_g")
            .HasPrecision(8, 2);

        builder.Property(entity => entity.FatG)
            .HasColumnName("fat_g")
            .HasPrecision(8, 2);

        builder.Property(entity => entity.FibreG)
            .HasColumnName("fibre_g")
            .HasPrecision(8, 2);

        builder.Property(entity => entity.EdamamMeasureUri)
            .HasColumnName("edamam_measure_uri");

        builder.Property(entity => entity.EdamamFoodId)
            .HasColumnName("edamam_food_id");

        builder.HasOne(entity => entity.Recipe)
            .WithMany(recipe => recipe.Ingredients)
            .HasForeignKey(entity => entity.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.Ingredient)
            .WithMany(ingredient => ingredient.RecipeIngredients)
            .HasForeignKey(entity => entity.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_recipe_ingredient_quantity_positive", "quantity > 0");
        });
    }
}

internal sealed class RecipeStepConfiguration : IEntityTypeConfiguration<RecipeStep>
{
    public void Configure(EntityTypeBuilder<RecipeStep> builder)
    {
        builder.ToTable("recipe_step");

        builder.HasKey(entity => entity.Id);

        builder.Property(entity => entity.Id)
            .HasColumnName("id")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(entity => entity.RecipeId)
            .HasColumnName("recipe_id");

        builder.Property(entity => entity.StepNumber)
            .HasColumnName("step_number");

        builder.Property(entity => entity.Instruction)
            .HasColumnName("instruction")
            .IsRequired();

        builder.Property(entity => entity.StepType)
            .HasColumnName("step_type")
            .IsRequired();

        builder.Property(entity => entity.TimerSeconds)
            .HasColumnName("timer_seconds");

        builder.Property(entity => entity.BackgroundStepId)
            .HasColumnName("background_step_id");

        builder.HasIndex(entity => new { entity.RecipeId, entity.StepNumber })
            .IsUnique();

        builder.HasOne(entity => entity.Recipe)
            .WithMany(recipe => recipe.Steps)
            .HasForeignKey(entity => entity.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(entity => entity.BackgroundStep)
            .WithMany(step => step.DependentSteps)
            .HasForeignKey(entity => entity.BackgroundStepId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.ToTable(tableBuilder =>
        {
            tableBuilder.HasCheckConstraint("ck_recipe_step_step_number_positive", "step_number > 0");
            tableBuilder.HasCheckConstraint(
                "ck_recipe_step_type",
                "step_type IN ('sequential', 'background', 'timed')");
            tableBuilder.HasCheckConstraint(
                "ck_recipe_step_timer_required",
                "step_type <> 'timed' OR timer_seconds IS NOT NULL");
            tableBuilder.HasCheckConstraint(
                "ck_recipe_step_timer_positive",
                "timer_seconds IS NULL OR timer_seconds > 0");
        });
    }
}
