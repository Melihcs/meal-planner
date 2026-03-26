namespace MealPlanner.Api.Domain.Entities;

public interface IHasUpdatedAt
{
    DateTime UpdatedAt { get; set; }
}

public sealed class UserProfile : IHasUpdatedAt
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public ICollection<Recipe> Recipes { get; set; } = [];
    public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
    public ICollection<WeeklyPlan> WeeklyPlans { get; set; } = [];
}

public sealed class Ingredient
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? EdamamFoodId { get; set; }
    public string? Category { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<RecipeIngredient> RecipeIngredients { get; set; } = [];
    public ICollection<ShoppingItem> ShoppingItems { get; set; } = [];
}
