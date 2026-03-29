namespace MealPlanner.Api.Features.Recipes;

using MealPlanner.Api.Domain.Entities;
using MealPlanner.Api.Domain.Exceptions;
using MealPlanner.Api.Infrastructure.Auth;
using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;

public static class RecipeEndpointRouteBuilderExtensions
{
    public static RouteGroupBuilder MapRecipeEndpoints(this RouteGroupBuilder routeGroupBuilder)
    {
        routeGroupBuilder.MapGet("/recipes", GetRecipes)
            .WithName("GetRecipes");

        routeGroupBuilder.MapGet("/recipes/{recipeId:guid}", GetRecipeById)
            .WithName("GetRecipeById");

        routeGroupBuilder.MapPost("/recipes", CreateRecipe)
            .WithName("CreateRecipe");

        routeGroupBuilder.MapPut("/recipes/{recipeId:guid}", UpdateRecipe)
            .WithName("UpdateRecipe");

        routeGroupBuilder.MapDelete("/recipes/{recipeId:guid}", DeleteRecipe)
            .WithName("DeleteRecipe");

        routeGroupBuilder.MapPost("/recipes/{recipeId:guid}/ingredients", AddRecipeIngredient)
            .WithName("AddRecipeIngredient");

        routeGroupBuilder.MapPut("/recipes/{recipeId:guid}/ingredients/{recipeIngredientId:guid}", UpdateRecipeIngredient)
            .WithName("UpdateRecipeIngredient");

        routeGroupBuilder.MapDelete("/recipes/{recipeId:guid}/ingredients/{recipeIngredientId:guid}", DeleteRecipeIngredient)
            .WithName("DeleteRecipeIngredient");

        routeGroupBuilder.MapPost("/recipes/{recipeId:guid}/steps", AddRecipeStep)
            .WithName("AddRecipeStep");

        routeGroupBuilder.MapPut("/recipes/{recipeId:guid}/steps/{recipeStepId:guid}", UpdateRecipeStep)
            .WithName("UpdateRecipeStep");

        routeGroupBuilder.MapPut("/recipes/{recipeId:guid}/steps/reorder", ReorderRecipeSteps)
            .WithName("ReorderRecipeSteps");

        routeGroupBuilder.MapDelete("/recipes/{recipeId:guid}/steps/{recipeStepId:guid}", DeleteRecipeStep)
            .WithName("DeleteRecipeStep");

        return routeGroupBuilder;
    }

    private static async Task<Ok<IReadOnlyList<RecipeSummaryResponse>>> GetRecipes(
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipes = await dbContext.Recipes
            .AsNoTracking()
            .Where(recipe => recipe.UserId == currentUser.UserId)
            .OrderByDescending(recipe => recipe.UpdatedAt)
            .Select(recipe => new RecipeSummaryResponse(
                recipe.Id,
                recipe.Title,
                recipe.Description,
                recipe.Servings,
                recipe.Cuisine,
                recipe.CoverColour,
                recipe.Visibility,
                recipe.PrepTimeMinutes,
                recipe.CookTimeMinutes,
                recipe.Ingredients.Count,
                recipe.Steps.Count,
                recipe.CreatedAt,
                recipe.UpdatedAt))
            .ToListAsync(cancellationToken);

        return TypedResults.Ok<IReadOnlyList<RecipeSummaryResponse>>(recipes);
    }

    private static async Task<Ok<RecipeDetailResponse>> GetRecipeById(
        Guid recipeId,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        return TypedResults.Ok(MapRecipeDetail(recipe));
    }

    private static async Task<Created<RecipeDetailResponse>> CreateRecipe(
        CreateRecipeRequest request,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        ValidateRecipeVisibility(request.Visibility);
        await EnsureCurrentUserProfileExistsAsync(dbContext, currentUser, cancellationToken);

        var recipe = new Recipe
        {
            UserId = currentUser.UserId,
            Title = request.Title.Trim(),
            Description = NullIfWhiteSpace(request.Description),
            Servings = request.Servings,
            Cuisine = NullIfWhiteSpace(request.Cuisine),
            CoverColour = request.CoverColour?.Trim() ?? "#E8F5E9",
            Visibility = NormalizeVisibility(request.Visibility),
            PrepTimeMinutes = request.PrepTimeMinutes,
            CookTimeMinutes = request.CookTimeMinutes,
        };

        dbContext.Recipes.Add(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);

        var createdRecipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipe.Id, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        return TypedResults.Created($"/recipes/{recipe.Id}", MapRecipeDetail(createdRecipe));
    }

    private static async Task<Ok<RecipeDetailResponse>> UpdateRecipe(
        Guid recipeId,
        UpdateRecipeRequest request,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        ValidateRecipeVisibility(request.Visibility);

        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        recipe.Title = request.Title.Trim();
        recipe.Description = NullIfWhiteSpace(request.Description);
        recipe.Servings = request.Servings;
        recipe.Cuisine = NullIfWhiteSpace(request.Cuisine);
        recipe.CoverColour = request.CoverColour?.Trim() ?? "#E8F5E9";
        recipe.Visibility = NormalizeVisibility(request.Visibility);
        recipe.PrepTimeMinutes = request.PrepTimeMinutes;
        recipe.CookTimeMinutes = request.CookTimeMinutes;

        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(MapRecipeDetail(recipe));
    }

    private static async Task<NoContent> DeleteRecipe(
        Guid recipeId,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await dbContext.Recipes
            .FirstOrDefaultAsync(
                candidate => candidate.Id == recipeId && candidate.UserId == currentUser.UserId,
                cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        dbContext.Recipes.Remove(recipe);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Created<RecipeIngredientResponse>> AddRecipeIngredient(
        Guid recipeId,
        UpsertRecipeIngredientRequest request,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        var ingredient = await ResolveIngredientAsync(dbContext, request, cancellationToken);
        var recipeIngredient = new RecipeIngredient
        {
            RecipeId = recipe.Id,
            Ingredient = ingredient,
            Quantity = request.Quantity,
            Unit = request.Unit.Trim(),
            CaloriesPer100G = request.CaloriesPer100G,
            ProteinG = request.ProteinG,
            CarbsG = request.CarbsG,
            FatG = request.FatG,
            FibreG = request.FibreG,
            EdamamFoodId = NullIfWhiteSpace(request.EdamamFoodId),
            EdamamMeasureUri = NullIfWhiteSpace(request.EdamamMeasureUri),
        };

        recipe.Ingredients.Add(recipeIngredient);
        ReindexIngredientSortOrder(recipe.Ingredients, recipeIngredient, request.SortOrder);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/recipes/{recipe.Id}/ingredients/{recipeIngredient.Id}",
            MapRecipeIngredient(recipeIngredient));
    }

    private static async Task<Ok<RecipeIngredientResponse>> UpdateRecipeIngredient(
        Guid recipeId,
        Guid recipeIngredientId,
        UpsertRecipeIngredientRequest request,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        var recipeIngredient = recipe.Ingredients
            .FirstOrDefault(candidate => candidate.Id == recipeIngredientId)
            ?? throw new NotFoundException("Recipe ingredient not found.");

        recipeIngredient.Ingredient = await ResolveIngredientAsync(dbContext, request, cancellationToken);
        recipeIngredient.Quantity = request.Quantity;
        recipeIngredient.Unit = request.Unit.Trim();
        recipeIngredient.CaloriesPer100G = request.CaloriesPer100G;
        recipeIngredient.ProteinG = request.ProteinG;
        recipeIngredient.CarbsG = request.CarbsG;
        recipeIngredient.FatG = request.FatG;
        recipeIngredient.FibreG = request.FibreG;
        recipeIngredient.EdamamFoodId = NullIfWhiteSpace(request.EdamamFoodId);
        recipeIngredient.EdamamMeasureUri = NullIfWhiteSpace(request.EdamamMeasureUri);

        ReindexIngredientSortOrder(recipe.Ingredients, recipeIngredient, request.SortOrder);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Ok(MapRecipeIngredient(recipeIngredient));
    }

    private static async Task<NoContent> DeleteRecipeIngredient(
        Guid recipeId,
        Guid recipeIngredientId,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        var recipeIngredient = recipe.Ingredients
            .FirstOrDefault(candidate => candidate.Id == recipeIngredientId)
            ?? throw new NotFoundException("Recipe ingredient not found.");

        recipe.Ingredients.Remove(recipeIngredient);
        dbContext.RecipeIngredients.Remove(recipeIngredient);
        ReindexIngredientSortOrder(recipe.Ingredients, targetIngredient: null, requestedSortOrder: null);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.NoContent();
    }

    private static async Task<Created<RecipeStepResponse>> AddRecipeStep(
        Guid recipeId,
        CreateRecipeStepRequest request,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        var recipeStep = new RecipeStep
        {
            RecipeId = recipe.Id,
            Instruction = request.Instruction.Trim(),
            StepType = request.StepType.Trim().ToLowerInvariant(),
            TimerSeconds = request.TimerSeconds,
            BackgroundStepId = request.BackgroundStepId,
            StepNumber = recipe.Steps.Count + 1,
        };

        ValidateRecipeStep(recipe, recipeStep, recipeStep.StepType, recipeStep.TimerSeconds, recipeStep.BackgroundStepId);

        recipe.Steps.Add(recipeStep);
        await dbContext.SaveChangesAsync(cancellationToken);

        return TypedResults.Created(
            $"/recipes/{recipe.Id}/steps/{recipeStep.Id}",
            MapRecipeStep(recipeStep));
    }

    private static async Task<Ok<RecipeStepResponse>> UpdateRecipeStep(
        Guid recipeId,
        Guid recipeStepId,
        UpdateRecipeStepRequest request,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        var recipeStep = recipe.Steps
            .FirstOrDefault(candidate => candidate.Id == recipeStepId)
            ?? throw new NotFoundException("Recipe step not found.");

        var normalizedStepType = request.StepType.Trim().ToLowerInvariant();
        ValidateRecipeStep(recipe, recipeStep, normalizedStepType, request.TimerSeconds, request.BackgroundStepId);

        recipeStep.Instruction = request.Instruction.Trim();
        recipeStep.StepType = normalizedStepType;
        recipeStep.TimerSeconds = request.TimerSeconds;
        recipeStep.BackgroundStepId = request.BackgroundStepId;

        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok(MapRecipeStep(recipeStep));
    }

    private static async Task<Ok<IReadOnlyList<RecipeStepResponse>>> ReorderRecipeSteps(
        Guid recipeId,
        ReorderRecipeStepsRequest request,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        ValidateRecipeStepOrder(recipe, request.StepIds);

        var orderedSteps = request.StepIds
            .Select(stepId => recipe.Steps.First(step => step.Id == stepId))
            .ToList();

        for (var index = 0; index < orderedSteps.Count; index++)
        {
            orderedSteps[index].StepNumber = index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.Ok<IReadOnlyList<RecipeStepResponse>>(orderedSteps.Select(MapRecipeStep).ToList());
    }

    private static async Task<NoContent> DeleteRecipeStep(
        Guid recipeId,
        Guid recipeStepId,
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var recipe = await GetOwnedRecipeAsync(dbContext, currentUser.UserId, recipeId, cancellationToken)
            ?? throw new NotFoundException("Recipe not found.");

        var recipeStep = recipe.Steps
            .FirstOrDefault(candidate => candidate.Id == recipeStepId)
            ?? throw new NotFoundException("Recipe step not found.");

        foreach (var dependentStep in recipe.Steps.Where(step => step.BackgroundStepId == recipeStep.Id))
        {
            dependentStep.BackgroundStepId = null;
        }

        recipe.Steps.Remove(recipeStep);
        dbContext.RecipeSteps.Remove(recipeStep);

        foreach (var step in recipe.Steps.OrderBy(step => step.StepNumber).ThenBy(step => step.Id).Select((step, index) => new { step, index }))
        {
            step.step.StepNumber = step.index + 1;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return TypedResults.NoContent();
    }

    private static async Task<Recipe?> GetOwnedRecipeAsync(
        MealPlannerDbContext dbContext,
        string userId,
        Guid recipeId,
        CancellationToken cancellationToken)
    {
        return await dbContext.Recipes
            .Include(recipe => recipe.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.Ingredient)
            .Include(recipe => recipe.Steps)
            .FirstOrDefaultAsync(
                recipe => recipe.Id == recipeId && recipe.UserId == userId,
                cancellationToken);
    }

    private static async Task EnsureCurrentUserProfileExistsAsync(
        MealPlannerDbContext dbContext,
        ICurrentUserService currentUser,
        CancellationToken cancellationToken)
    {
        var exists = await dbContext.UserProfiles
            .AnyAsync(user => user.Id == currentUser.UserId, cancellationToken);

        if (exists)
        {
            return;
        }

        dbContext.UserProfiles.Add(new UserProfile
        {
            Id = currentUser.UserId,
            DisplayName = currentUser.Email,
        });

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task<Ingredient> ResolveIngredientAsync(
        MealPlannerDbContext dbContext,
        UpsertRecipeIngredientRequest request,
        CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();
        var normalizedEdamamFoodId = NullIfWhiteSpace(request.EdamamFoodId);
        var normalizedCategory = NullIfWhiteSpace(request.Category);

        Ingredient? ingredient = null;
        if (normalizedEdamamFoodId is not null)
        {
            ingredient = await dbContext.Ingredients
                .FirstOrDefaultAsync(candidate => candidate.EdamamFoodId == normalizedEdamamFoodId, cancellationToken);
        }

        ingredient ??= await dbContext.Ingredients
            .FirstOrDefaultAsync(
                candidate => candidate.Name.ToLower() == normalizedName.ToLower(),
                cancellationToken);

        if (ingredient is null)
        {
            ingredient = new Ingredient
            {
                Name = normalizedName,
                EdamamFoodId = normalizedEdamamFoodId,
                Category = normalizedCategory,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.Ingredients.Add(ingredient);
            return ingredient;
        }

        ingredient.Name = normalizedName;
        ingredient.EdamamFoodId = normalizedEdamamFoodId ?? ingredient.EdamamFoodId;
        ingredient.Category = normalizedCategory ?? ingredient.Category;
        return ingredient;
    }

    private static void ReindexIngredientSortOrder(
        ICollection<RecipeIngredient> recipeIngredients,
        RecipeIngredient? targetIngredient,
        int? requestedSortOrder)
    {
        var orderedIngredients = recipeIngredients
            .Where(recipeIngredient => targetIngredient is null || recipeIngredient.Id != targetIngredient.Id)
            .OrderBy(recipeIngredient => recipeIngredient.SortOrder)
            .ThenBy(recipeIngredient => recipeIngredient.Id)
            .ToList();

        if (targetIngredient is not null)
        {
            var insertionIndex = requestedSortOrder is null
                ? orderedIngredients.Count
                : Math.Clamp(requestedSortOrder.Value - 1, 0, orderedIngredients.Count);

            orderedIngredients.Insert(insertionIndex, targetIngredient);
        }

        for (var index = 0; index < orderedIngredients.Count; index++)
        {
            orderedIngredients[index].SortOrder = index + 1;
        }
    }

    private static void ValidateRecipeVisibility(string? visibility)
    {
        var normalizedVisibility = NormalizeVisibility(visibility);
        if (normalizedVisibility is "private" or "public")
        {
            return;
        }

        throw new RequestValidationException(new Dictionary<string, string[]>
        {
            ["visibility"] = ["Visibility must be either private or public."],
        });
    }

    private static string NormalizeVisibility(string? visibility)
    {
        return string.IsNullOrWhiteSpace(visibility)
            ? "private"
            : visibility.Trim().ToLowerInvariant();
    }

    private static void ValidateRecipeStep(
        Recipe recipe,
        RecipeStep candidateStep,
        string stepType,
        int? timerSeconds,
        Guid? backgroundStepId)
    {
        var errors = new Dictionary<string, string[]>();

        if (stepType == "timed" && timerSeconds is null)
        {
            errors["timerSeconds"] = ["Timed steps require timerSeconds."];
        }

        if (stepType != "timed" && timerSeconds is not null)
        {
            errors["timerSeconds"] = ["Only timed steps may define timerSeconds."];
        }

        if (backgroundStepId is not null)
        {
            if (backgroundStepId == candidateStep.Id)
            {
                errors["backgroundStepId"] = ["A step cannot depend on itself."];
            }
            else
            {
                var backgroundStep = recipe.Steps.FirstOrDefault(step => step.Id == backgroundStepId.Value);

                if (backgroundStep is null)
                {
                    errors["backgroundStepId"] = ["Background step must belong to the same recipe."];
                }
                else if (backgroundStep.StepType != "background")
                {
                    errors["backgroundStepId"] = ["Background step dependencies must reference a background step."];
                }
            }
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }

    private static void ValidateRecipeStepOrder(Recipe recipe, IReadOnlyList<Guid> stepIds)
    {
        if (stepIds.Distinct().Count() != stepIds.Count)
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["stepIds"] = ["Step ids must be unique."],
            });
        }

        var existingStepIds = recipe.Steps.Select(step => step.Id).Order().ToArray();
        var requestedStepIds = stepIds.Order().ToArray();
        if (!existingStepIds.SequenceEqual(requestedStepIds))
        {
            throw new RequestValidationException(new Dictionary<string, string[]>
            {
                ["stepIds"] = ["Step ids must match the recipe's current steps exactly."],
            });
        }
    }

    private static RecipeDetailResponse MapRecipeDetail(Recipe recipe)
    {
        return new RecipeDetailResponse(
            recipe.Id,
            recipe.Title,
            recipe.Description,
            recipe.Servings,
            recipe.Cuisine,
            recipe.CoverColour,
            recipe.Visibility,
            recipe.PrepTimeMinutes,
            recipe.CookTimeMinutes,
            recipe.CreatedAt,
            recipe.UpdatedAt,
            recipe.Ingredients
                .OrderBy(recipeIngredient => recipeIngredient.SortOrder)
                .ThenBy(recipeIngredient => recipeIngredient.Id)
                .Select(MapRecipeIngredient)
                .ToList(),
            recipe.Steps
                .OrderBy(recipeStep => recipeStep.StepNumber)
                .ThenBy(recipeStep => recipeStep.Id)
                .Select(MapRecipeStep)
                .ToList());
    }

    private static RecipeIngredientResponse MapRecipeIngredient(RecipeIngredient recipeIngredient)
    {
        return new RecipeIngredientResponse(
            recipeIngredient.Id,
            recipeIngredient.IngredientId,
            recipeIngredient.Ingredient.Name,
            recipeIngredient.Quantity,
            recipeIngredient.Unit,
            recipeIngredient.SortOrder,
            recipeIngredient.CaloriesPer100G,
            recipeIngredient.ProteinG,
            recipeIngredient.CarbsG,
            recipeIngredient.FatG,
            recipeIngredient.FibreG,
            recipeIngredient.EdamamFoodId,
            recipeIngredient.EdamamMeasureUri,
            recipeIngredient.Ingredient.Category);
    }

    private static RecipeStepResponse MapRecipeStep(RecipeStep recipeStep)
    {
        return new RecipeStepResponse(
            recipeStep.Id,
            recipeStep.StepNumber,
            recipeStep.Instruction,
            recipeStep.StepType,
            recipeStep.TimerSeconds,
            recipeStep.BackgroundStepId);
    }

    private static string? NullIfWhiteSpace(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
