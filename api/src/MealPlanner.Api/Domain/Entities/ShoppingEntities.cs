namespace MealPlanner.Api.Domain.Entities;

public sealed class ShoppingList : IHasUpdatedAt
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public Guid? PlanId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public WeeklyPlan? Plan { get; set; }
    public ICollection<ShoppingItem> Items { get; set; } = [];
    public UserProfile User { get; set; } = null!;
}

public sealed class ShoppingItem
{
    public Guid Id { get; set; }
    public Guid ListId { get; set; }
    public Guid? IngredientId { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    public string? Unit { get; set; }
    public string? Category { get; set; }
    public bool IsChecked { get; set; }
    public bool IsManual { get; set; }
    public int SortOrder { get; set; }

    public Ingredient? Ingredient { get; set; }
    public ShoppingList List { get; set; } = null!;
}
