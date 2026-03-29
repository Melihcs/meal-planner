using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using MealPlanner.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;

namespace MealPlanner.Api.Tests;

public sealed class JwtAuthenticationIntegrationTests
{
    [Fact]
    public async Task ProtectedEndpoint_Returns401WithoutBearerToken()
    {
        await using var app = await CreateApplicationAsync();
        var client = app.GetTestClient();

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task ProtectedEndpoint_ReturnsCurrentUserFromBearerToken()
    {
        var configuration = BuildConfiguration();
        await using var app = await CreateApplicationAsync(configuration);
        var client = app.GetTestClient();

        var token = new JwtTokenService(configuration).CreateToken(
            userId: "google-sub-123",
            email: "cook@example.com",
            displayName: "Casey Cook",
            pictureUrl: null);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<CurrentUserResponse>();
        Assert.NotNull(payload);
        Assert.Equal("google-sub-123", payload.UserId);
        Assert.Equal("cook@example.com", payload.Email);
    }

    private static async Task<WebApplication> CreateApplicationAsync(IConfiguration? configuration = null)
    {
        configuration ??= BuildConfiguration();

        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Configuration.AddConfiguration(configuration);
        builder.Services.AddMealPlannerAuthentication(builder.Configuration);

        var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapGet("/auth/me", (ICurrentUserService currentUser) => Results.Ok(new
        {
            userId = currentUser.UserId,
            email = currentUser.Email,
        }))
            .RequireAuthorization();

        await app.StartAsync();
        return app;
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
                ["GOOGLE_CLIENT_ID"] = "test-client-id",
                ["GOOGLE_CLIENT_SECRET"] = "test-client-secret",
            })
            .Build();
    }

    private sealed record CurrentUserResponse(string UserId, string Email);
}
