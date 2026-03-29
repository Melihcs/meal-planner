namespace MealPlanner.Api.Features.Recipes;

using System.ComponentModel.DataAnnotations;

public sealed record CreateRecipeRequest(
    [property: Required(ErrorMessage = "Title is required.")]
    [property: StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters.")]
    string Title,
    [property: StringLength(4000, ErrorMessage = "Description must be 4000 characters or fewer.")]
    string? Description,
    [property: Range(1, 100, ErrorMessage = "Servings must be between 1 and 100.")]
    int Servings,
    [property: StringLength(100, ErrorMessage = "Cuisine must be 100 characters or fewer.")]
    string? Cuisine,
    [property: RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Cover colour must be a hex colour like #E8F5E9.")]
    string? CoverColour,
    [property: StringLength(20, ErrorMessage = "Visibility must be 20 characters or fewer.")]
    string? Visibility,
    [property: Range(0, 1440, ErrorMessage = "Prep time must be between 0 and 1440 minutes.")]
    int? PrepTimeMinutes,
    [property: Range(0, 1440, ErrorMessage = "Cook time must be between 0 and 1440 minutes.")]
    int? CookTimeMinutes);

public sealed record UpdateRecipeRequest(
    [property: Required(ErrorMessage = "Title is required.")]
    [property: StringLength(200, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 200 characters.")]
    string Title,
    [property: StringLength(4000, ErrorMessage = "Description must be 4000 characters or fewer.")]
    string? Description,
    [property: Range(1, 100, ErrorMessage = "Servings must be between 1 and 100.")]
    int Servings,
    [property: StringLength(100, ErrorMessage = "Cuisine must be 100 characters or fewer.")]
    string? Cuisine,
    [property: RegularExpression("^#[0-9A-Fa-f]{6}$", ErrorMessage = "Cover colour must be a hex colour like #E8F5E9.")]
    string? CoverColour,
    [property: StringLength(20, ErrorMessage = "Visibility must be 20 characters or fewer.")]
    string? Visibility,
    [property: Range(0, 1440, ErrorMessage = "Prep time must be between 0 and 1440 minutes.")]
    int? PrepTimeMinutes,
    [property: Range(0, 1440, ErrorMessage = "Cook time must be between 0 and 1440 minutes.")]
    int? CookTimeMinutes);

public sealed record UpsertRecipeIngredientRequest(
    [property: Required(ErrorMessage = "Ingredient name is required.")]
    [property: StringLength(200, MinimumLength = 1, ErrorMessage = "Ingredient name must be between 1 and 200 characters.")]
    string Name,
    [property: Range(typeof(decimal), "0.001", "9999999", ErrorMessage = "Quantity must be greater than zero.")]
    decimal Quantity,
    [property: Required(ErrorMessage = "Unit is required.")]
    [property: StringLength(50, MinimumLength = 1, ErrorMessage = "Unit must be between 1 and 50 characters.")]
    string Unit,
    [property: Range(1, 1000, ErrorMessage = "Sort order must be between 1 and 1000.")]
    int? SortOrder,
    [property: StringLength(255, ErrorMessage = "Edamam food id must be 255 characters or fewer.")]
    string? EdamamFoodId,
    [property: StringLength(500, ErrorMessage = "Edamam measure URI must be 500 characters or fewer.")]
    string? EdamamMeasureUri,
    [property: Range(typeof(decimal), "0", "9999999", ErrorMessage = "Calories must be zero or greater.")]
    decimal? CaloriesPer100G,
    [property: Range(typeof(decimal), "0", "9999999", ErrorMessage = "Protein must be zero or greater.")]
    decimal? ProteinG,
    [property: Range(typeof(decimal), "0", "9999999", ErrorMessage = "Carbs must be zero or greater.")]
    decimal? CarbsG,
    [property: Range(typeof(decimal), "0", "9999999", ErrorMessage = "Fat must be zero or greater.")]
    decimal? FatG,
    [property: Range(typeof(decimal), "0", "9999999", ErrorMessage = "Fibre must be zero or greater.")]
    decimal? FibreG,
    [property: StringLength(100, ErrorMessage = "Category must be 100 characters or fewer.")]
    string? Category);

public sealed record CreateRecipeStepRequest(
    [property: Required(ErrorMessage = "Instruction is required.")]
    [property: StringLength(4000, MinimumLength = 1, ErrorMessage = "Instruction must be between 1 and 4000 characters.")]
    string Instruction,
    [property: Required(ErrorMessage = "Step type is required.")]
    [property: RegularExpression("^(sequential|background|timed)$", ErrorMessage = "Step type must be sequential, background, or timed.")]
    string StepType,
    [property: Range(1, 86400, ErrorMessage = "Timer seconds must be between 1 and 86400.")]
    int? TimerSeconds,
    Guid? BackgroundStepId);

public sealed record UpdateRecipeStepRequest(
    [property: Required(ErrorMessage = "Instruction is required.")]
    [property: StringLength(4000, MinimumLength = 1, ErrorMessage = "Instruction must be between 1 and 4000 characters.")]
    string Instruction,
    [property: Required(ErrorMessage = "Step type is required.")]
    [property: RegularExpression("^(sequential|background|timed)$", ErrorMessage = "Step type must be sequential, background, or timed.")]
    string StepType,
    [property: Range(1, 86400, ErrorMessage = "Timer seconds must be between 1 and 86400.")]
    int? TimerSeconds,
    Guid? BackgroundStepId);

public sealed record ReorderRecipeStepsRequest(
    [property: Required(ErrorMessage = "Step ids are required.")]
    [property: MinLength(1, ErrorMessage = "Step ids must contain at least one step.")]
    IReadOnlyList<Guid> StepIds);

public sealed record RecipeSummaryResponse(
    Guid Id,
    string Title,
    string? Description,
    int Servings,
    string? Cuisine,
    string CoverColour,
    string Visibility,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    int IngredientCount,
    int StepCount,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record RecipeDetailResponse(
    Guid Id,
    string Title,
    string? Description,
    int Servings,
    string? Cuisine,
    string CoverColour,
    string Visibility,
    int? PrepTimeMinutes,
    int? CookTimeMinutes,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<RecipeIngredientResponse> Ingredients,
    IReadOnlyList<RecipeStepResponse> Steps);

public sealed record RecipeIngredientResponse(
    Guid Id,
    Guid IngredientId,
    string Name,
    decimal Quantity,
    string Unit,
    int SortOrder,
    decimal? CaloriesPer100G,
    decimal? ProteinG,
    decimal? CarbsG,
    decimal? FatG,
    decimal? FibreG,
    string? EdamamFoodId,
    string? EdamamMeasureUri,
    string? Category);

public sealed record RecipeStepResponse(
    Guid Id,
    int StepNumber,
    string Instruction,
    string StepType,
    int? TimerSeconds,
    Guid? BackgroundStepId);
