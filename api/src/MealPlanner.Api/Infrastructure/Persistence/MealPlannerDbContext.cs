namespace MealPlanner.Api.Infrastructure.Persistence;

using MealPlanner.Api.Domain.Entities;
using Microsoft.EntityFrameworkCore;

public sealed class MealPlannerDbContext(DbContextOptions<MealPlannerDbContext> options) : DbContext(options)
{
    public DbSet<Ingredient> Ingredients => Set<Ingredient>();
    public DbSet<NutrientOrganMapping> NutrientOrganMappings => Set<NutrientOrganMapping>();
    public DbSet<PlanSlot> PlanSlots => Set<PlanSlot>();
    public DbSet<Recipe> Recipes => Set<Recipe>();
    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();
    public DbSet<RecipeStep> RecipeSteps => Set<RecipeStep>();
    public DbSet<ShoppingItem> ShoppingItems => Set<ShoppingItem>();
    public DbSet<ShoppingList> ShoppingLists => Set<ShoppingList>();
    public DbSet<UserProfile> UserProfiles => Set<UserProfile>();
    public DbSet<WeeklyPlan> WeeklyPlans => Set<WeeklyPlan>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pgcrypto");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealPlannerDbContext).Assembly);
    }
}
