namespace MealPlanner.Api.Domain.Entities;

public sealed class Recipe : IHasUpdatedAt
{
    public Guid Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Servings { get; set; }
    public string? Cuisine { get; set; }
    public string CoverColour { get; set; } = string.Empty;
    public string Visibility { get; set; } = string.Empty;
    public int? PrepTimeMinutes { get; set; }
    public int? CookTimeMinutes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public UserProfile User { get; set; } = null!;
    public ICollection<PlanSlot> PlanSlots { get; set; } = [];
    public ICollection<RecipeIngredient> Ingredients { get; set; } = [];
    public ICollection<RecipeStep> Steps { get; set; } = [];
}

public sealed class RecipeIngredient
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public Guid IngredientId { get; set; }
    public decimal Quantity { get; set; }
    public string Unit { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public decimal? CaloriesPer100G { get; set; }
    public decimal? ProteinG { get; set; }
    public decimal? CarbsG { get; set; }
    public decimal? FatG { get; set; }
    public decimal? FibreG { get; set; }
    public string? EdamamMeasureUri { get; set; }
    public string? EdamamFoodId { get; set; }

    public Ingredient Ingredient { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}

public sealed class RecipeStep
{
    public Guid Id { get; set; }
    public Guid RecipeId { get; set; }
    public int StepNumber { get; set; }
    public string Instruction { get; set; } = string.Empty;
    public string StepType { get; set; } = string.Empty;
    public int? TimerSeconds { get; set; }
    public Guid? BackgroundStepId { get; set; }

    public Recipe Recipe { get; set; } = null!;
    public RecipeStep? BackgroundStep { get; set; }
    public ICollection<RecipeStep> DependentSteps { get; set; } = [];
}
