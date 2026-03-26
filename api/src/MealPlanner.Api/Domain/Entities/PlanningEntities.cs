namespace MealPlanner.Api.Domain.Entities;

public sealed class WeeklyPlan : IHasUpdatedAt
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public DateOnly WeekStart { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public DateTime? GeneratedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserProfile User { get; set; } = null!;
    public ICollection<PlanSlot> Slots { get; set; } = [];
    public ICollection<ShoppingList> ShoppingLists { get; set; } = [];
}

public sealed class PlanSlot
{
    public Guid Id { get; set; }
    public Guid PlanId { get; set; }
    public Guid RecipeId { get; set; }
    public DateOnly SlotDate { get; set; }
    public string MealType { get; set; } = string.Empty;
    public int Servings { get; set; }
    public string? Rationale { get; set; }
    public int SortOrder { get; set; }

    public WeeklyPlan Plan { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}
