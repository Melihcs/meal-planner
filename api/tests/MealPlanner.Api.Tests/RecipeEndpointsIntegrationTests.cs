using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MealPlanner.Api.Features.Recipes;
using MealPlanner.Api.Infrastructure.Auth;
using MealPlanner.Api.Infrastructure.Http;
using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MealPlanner.Api.Tests;

public sealed class RecipeEndpointsIntegrationTests
{
    [Fact]
    public async Task RecipeCrudFlow_WorksEndToEnd()
    {
        await using var app = await CreateApplicationAsync();
        var client = CreateAuthenticatedClient(app);

        var createResponse = await client.PostAsJsonAsync("/recipes", new CreateRecipeRequest(
            Title: "Weeknight Pasta",
            Description: "Fast tomato pasta.",
            Servings: 4,
            Cuisine: "Italian",
            CoverColour: "#E8F5E9",
            Visibility: "private",
            PrepTimeMinutes: 10,
            CookTimeMinutes: 20));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var createdRecipe = await createResponse.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(createdRecipe);
        Assert.Equal("Weeknight Pasta", createdRecipe.Title);
        Assert.Empty(createdRecipe.Ingredients);
        Assert.Empty(createdRecipe.Steps);

        var listResponse = await client.GetAsync("/recipes");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var recipes = await listResponse.Content.ReadFromJsonAsync<List<RecipeSummaryDto>>();
        Assert.NotNull(recipes);
        Assert.Single(recipes);
        Assert.Equal(createdRecipe.Id, recipes[0].Id);

        var updateResponse = await client.PutAsJsonAsync($"/recipes/{createdRecipe.Id}", new UpdateRecipeRequest(
            Title: "Weekend Pasta",
            Description: "Slow roasted tomato pasta.",
            Servings: 6,
            Cuisine: "Italian",
            CoverColour: "#CDE7BE",
            Visibility: "public",
            PrepTimeMinutes: 25,
            CookTimeMinutes: 35));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedRecipe = await updateResponse.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(updatedRecipe);
        Assert.Equal("Weekend Pasta", updatedRecipe.Title);
        Assert.Equal("public", updatedRecipe.Visibility);

        var getResponse = await client.GetAsync($"/recipes/{createdRecipe.Id}");

        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        var fetchedRecipe = await getResponse.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(fetchedRecipe);
        Assert.Equal("Weekend Pasta", fetchedRecipe.Title);
        Assert.Equal(6, fetchedRecipe.Servings);

        var deleteResponse = await client.DeleteAsync($"/recipes/{createdRecipe.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var emptyListResponse = await client.GetAsync("/recipes");
        var emptyRecipes = await emptyListResponse.Content.ReadFromJsonAsync<List<RecipeSummaryDto>>();
        Assert.NotNull(emptyRecipes);
        Assert.Empty(emptyRecipes);
    }

    [Fact]
    public async Task RecipeIngredientFlow_AddUpdateDelete_WorksEndToEnd()
    {
        await using var app = await CreateApplicationAsync();
        var client = CreateAuthenticatedClient(app);
        var recipe = await CreateRecipeAsync(client, "Ingredient Test Recipe");

        var addResponse = await client.PostAsJsonAsync($"/recipes/{recipe.Id}/ingredients", new UpsertRecipeIngredientRequest(
            Name: "Tomato",
            Quantity: 2.5m,
            Unit: "pcs",
            SortOrder: 1,
            EdamamFoodId: "food_tomato",
            EdamamMeasureUri: "measure://piece",
            CaloriesPer100G: 18,
            ProteinG: 0.9m,
            CarbsG: 3.9m,
            FatG: 0.2m,
            FibreG: 1.2m,
            Category: "Vegetable"));

        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);
        var createdIngredient = await addResponse.Content.ReadFromJsonAsync<RecipeIngredientDto>();
        Assert.NotNull(createdIngredient);
        Assert.Equal("Tomato", createdIngredient.Name);
        Assert.Equal(1, createdIngredient.SortOrder);

        var updateResponse = await client.PutAsJsonAsync(
            $"/recipes/{recipe.Id}/ingredients/{createdIngredient.Id}",
            new UpsertRecipeIngredientRequest(
                Name: "Cherry Tomato",
                Quantity: 300m,
                Unit: "g",
                SortOrder: 1,
                EdamamFoodId: "food_tomato",
                EdamamMeasureUri: "measure://gram",
                CaloriesPer100G: 20,
                ProteinG: 1.1m,
                CarbsG: 4.1m,
                FatG: 0.3m,
                FibreG: 1.4m,
                Category: "Vegetable"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedIngredient = await updateResponse.Content.ReadFromJsonAsync<RecipeIngredientDto>();
        Assert.NotNull(updatedIngredient);
        Assert.Equal("Cherry Tomato", updatedIngredient.Name);
        Assert.Equal(300m, updatedIngredient.Quantity);
        Assert.Equal("g", updatedIngredient.Unit);

        var detailResponse = await client.GetAsync($"/recipes/{recipe.Id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(detail);
        Assert.Single(detail.Ingredients);
        Assert.Equal("Cherry Tomato", detail.Ingredients[0].Name);

        var deleteResponse = await client.DeleteAsync($"/recipes/{recipe.Id}/ingredients/{createdIngredient.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var emptyDetailResponse = await client.GetAsync($"/recipes/{recipe.Id}");
        var emptyDetail = await emptyDetailResponse.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(emptyDetail);
        Assert.Empty(emptyDetail.Ingredients);
    }

    [Fact]
    public async Task RecipeStepFlow_AddReorderUpdateDelete_WorksEndToEnd()
    {
        await using var app = await CreateApplicationAsync();
        var client = CreateAuthenticatedClient(app);
        var recipe = await CreateRecipeAsync(client, "Step Test Recipe");

        var backgroundStepResponse = await client.PostAsJsonAsync($"/recipes/{recipe.Id}/steps", new CreateRecipeStepRequest(
            Instruction: "Start the sauce",
            StepType: "background",
            TimerSeconds: null,
            BackgroundStepId: null));

        Assert.Equal(HttpStatusCode.Created, backgroundStepResponse.StatusCode);
        var backgroundStep = await backgroundStepResponse.Content.ReadFromJsonAsync<RecipeStepDto>();
        Assert.NotNull(backgroundStep);
        Assert.Equal(1, backgroundStep.StepNumber);

        var timedStepResponse = await client.PostAsJsonAsync($"/recipes/{recipe.Id}/steps", new CreateRecipeStepRequest(
            Instruction: "Boil the pasta",
            StepType: "timed",
            TimerSeconds: 600,
            BackgroundStepId: backgroundStep.Id));

        Assert.Equal(HttpStatusCode.Created, timedStepResponse.StatusCode);
        var timedStep = await timedStepResponse.Content.ReadFromJsonAsync<RecipeStepDto>();
        Assert.NotNull(timedStep);
        Assert.Equal(2, timedStep.StepNumber);
        Assert.Equal(backgroundStep.Id, timedStep.BackgroundStepId);

        var updateResponse = await client.PutAsJsonAsync(
            $"/recipes/{recipe.Id}/steps/{timedStep.Id}",
            new UpdateRecipeStepRequest(
                Instruction: "Boil the pasta until al dente",
                StepType: "timed",
                TimerSeconds: 660,
                BackgroundStepId: backgroundStep.Id));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        var updatedStep = await updateResponse.Content.ReadFromJsonAsync<RecipeStepDto>();
        Assert.NotNull(updatedStep);
        Assert.Equal("Boil the pasta until al dente", updatedStep.Instruction);
        Assert.Equal(660, updatedStep.TimerSeconds);

        var reorderResponse = await client.PutAsJsonAsync(
            $"/recipes/{recipe.Id}/steps/reorder",
            new ReorderRecipeStepsRequest([timedStep.Id, backgroundStep.Id]));

        Assert.Equal(HttpStatusCode.OK, reorderResponse.StatusCode);
        var reorderedSteps = await reorderResponse.Content.ReadFromJsonAsync<List<RecipeStepDto>>();
        Assert.NotNull(reorderedSteps);
        Assert.Equal(timedStep.Id, reorderedSteps[0].Id);
        Assert.Equal(1, reorderedSteps[0].StepNumber);
        Assert.Equal(backgroundStep.Id, reorderedSteps[1].Id);
        Assert.Equal(2, reorderedSteps[1].StepNumber);

        var deleteResponse = await client.DeleteAsync($"/recipes/{recipe.Id}/steps/{backgroundStep.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var detailResponse = await client.GetAsync($"/recipes/{recipe.Id}");
        var detail = await detailResponse.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(detail);
        Assert.Single(detail.Steps);
        Assert.Equal(timedStep.Id, detail.Steps[0].Id);
        Assert.Equal(1, detail.Steps[0].StepNumber);
        Assert.Null(detail.Steps[0].BackgroundStepId);
    }

    private static async Task<WebApplication> CreateApplicationAsync()
    {
        var configuration = BuildConfiguration();
        var databaseName = Guid.NewGuid().ToString("N");
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddProblemDetails();
        builder.Services.AddSingleton<UpdatedAtSaveChangesInterceptor>();
        builder.Services.AddDbContext<MealPlannerDbContext>(options =>
            options.UseInMemoryDatabase(databaseName));
        builder.Services.AddMealPlannerAuthentication(builder.Configuration);

        var app = builder.Build();
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>();
        app.UseAuthentication();
        app.UseAuthorization();

        var api = app.MapGroup(string.Empty)
            .AddEndpointFilterFactory(RequestValidationEndpointFilterFactory.Create)
            .RequireAuthorization();

        api.MapRecipeEndpoints();

        await app.StartAsync();
        return app;
    }

    private static HttpClient CreateAuthenticatedClient(WebApplication app)
    {
        var configuration = BuildConfiguration();
        var token = new JwtTokenService(configuration).CreateToken(
            userId: "user-123",
            email: "cook@example.com",
            displayName: "Casey Cook",
            pictureUrl: null);

        var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<RecipeDetailDto> CreateRecipeAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/recipes", new CreateRecipeRequest(
            Title: title,
            Description: null,
            Servings: 2,
            Cuisine: null,
            CoverColour: "#E8F5E9",
            Visibility: "private",
            PrepTimeMinutes: 5,
            CookTimeMinutes: 15));

        response.EnsureSuccessStatusCode();
        var recipe = await response.Content.ReadFromJsonAsync<RecipeDetailDto>();
        Assert.NotNull(recipe);
        return recipe;
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_ISSUER"] = "https://api.mealplanner.test",
                ["JWT_AUDIENCE"] = "meal-planner-tests",
                ["JWT_SECRET"] = "12345678901234567890123456789012",
                ["JWT_EXPIRY_DAYS"] = "7",
            })
            .Build();
    }

    private sealed record RecipeSummaryDto(Guid Id, string Title, int IngredientCount, int StepCount);

    private sealed record RecipeDetailDto(
        Guid Id,
        string Title,
        int Servings,
        string Visibility,
        IReadOnlyList<RecipeIngredientDto> Ingredients,
        IReadOnlyList<RecipeStepDto> Steps);

    private sealed record RecipeIngredientDto(
        Guid Id,
        Guid IngredientId,
        string Name,
        decimal Quantity,
        string Unit,
        int SortOrder);

    private sealed record RecipeStepDto(
        Guid Id,
        int StepNumber,
        string Instruction,
        string StepType,
        int? TimerSeconds,
        Guid? BackgroundStepId);
}
