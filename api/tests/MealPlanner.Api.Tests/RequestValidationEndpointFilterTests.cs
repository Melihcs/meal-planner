using System.ComponentModel.DataAnnotations;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using MealPlanner.Api.Infrastructure.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;

namespace MealPlanner.Api.Tests;

public sealed class RequestValidationEndpointFilterTests
{
    [Fact]
    public async Task InvalidRequest_Returns400WithFieldLevelErrors()
    {
        await using var app = await CreateApplicationAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/recipes",
            new CreateRecipeRequest(string.Empty, 0));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("Validation failed", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal(
            "Title is required.",
            payload.RootElement.GetProperty("errors").GetProperty("title")[0].GetString());
        Assert.Equal(
            "Servings must be between 1 and 12.",
            payload.RootElement.GetProperty("errors").GetProperty("servings")[0].GetString());
    }

    [Fact]
    public async Task ValidRequest_ReachesEndpoint()
    {
        await using var app = await CreateApplicationAsync();
        var client = app.GetTestClient();

        var response = await client.PostAsJsonAsync(
            "/recipes",
            new CreateRecipeRequest("Weeknight pasta", 4));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("Weeknight pasta", payload.RootElement.GetProperty("title").GetString());
        Assert.Equal(4, payload.RootElement.GetProperty("servings").GetInt32());
    }

    private static async Task<WebApplication> CreateApplicationAsync()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();

        var app = builder.Build();
        app.UseMiddleware<GlobalExceptionHandlingMiddleware>(
            NullLogger<GlobalExceptionHandlingMiddleware>.Instance,
            new TestHostEnvironment());

        var api = app.MapGroup(string.Empty)
            .AddEndpointFilterFactory(RequestValidationEndpointFilterFactory.Create);

        api.MapPost("/recipes", (CreateRecipeRequest request) =>
            Results.Ok(new
            {
                request.Title,
                request.Servings,
            }));

        await app.StartAsync();
        return app;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }

    private sealed record CreateRecipeRequest(
        [property: Required(ErrorMessage = "Title is required.")]
        [property: StringLength(120, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 120 characters.")]
        string Title,
        [property: Range(1, 12, ErrorMessage = "Servings must be between 1 and 12.")]
        int Servings);

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Development;
        public string ApplicationName { get; set; } = "MealPlanner.Api.Tests";
        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
