using System.Net;
using System.Text.Json;
using MealPlanner.Api.Infrastructure.Health;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MealPlanner.Api.Tests;

public sealed class HealthEndpointTests
{
    [Fact]
    public async Task GetHealth_Returns200WhenDatabaseCheckIsHealthy()
    {
        await using var app = await CreateApplicationAsync(HealthCheckResult.Healthy("PostgreSQL is reachable."));
        var client = app.GetTestClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json; charset=utf-8", response.Content.Headers.ContentType?.ToString());

        var payload = await ReadJsonAsync(response);
        Assert.Equal("Healthy", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "Healthy",
            payload.RootElement.GetProperty("checks").GetProperty("postgres").GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetHealth_Returns503WhenDatabaseCheckIsUnhealthy()
    {
        await using var app = await CreateApplicationAsync(
            HealthCheckResult.Unhealthy("PostgreSQL connectivity check failed."));
        var client = app.GetTestClient();

        var response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);

        var payload = await ReadJsonAsync(response);
        Assert.Equal("Unhealthy", payload.RootElement.GetProperty("status").GetString());
        Assert.Equal(
            "Unhealthy",
            payload.RootElement.GetProperty("checks").GetProperty("postgres").GetProperty("status").GetString());
    }

    private static async Task<WebApplication> CreateApplicationAsync(HealthCheckResult result)
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddHealthChecks()
            .AddCheck("postgres", () => result);

        var app = builder.Build();
        app.MapMealPlannerHealthEndpoint();
        await app.StartAsync();

        return app;
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        await using var stream = await response.Content.ReadAsStreamAsync();
        return await JsonDocument.ParseAsync(stream);
    }
}
