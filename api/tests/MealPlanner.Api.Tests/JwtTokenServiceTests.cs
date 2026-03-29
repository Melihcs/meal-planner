using System.IdentityModel.Tokens.Jwt;
using MealPlanner.Api.Infrastructure.Auth;
using Microsoft.Extensions.Configuration;

namespace MealPlanner.Api.Tests;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_IssuesExpectedClaims()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_ISSUER"] = "https://api.mealplanner.test",
                ["JWT_AUDIENCE"] = "meal-planner-tests",
                ["JWT_SECRET"] = "12345678901234567890123456789012",
                ["JWT_EXPIRY_DAYS"] = "7",
            })
            .Build();

        var tokenService = new JwtTokenService(configuration);

        var token = tokenService.CreateToken(
            userId: "google-sub-123",
            email: "cook@example.com",
            displayName: "Casey Cook",
            pictureUrl: "https://images.example.com/avatar.png");

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal("https://api.mealplanner.test", jwt.Issuer);
        Assert.Equal("meal-planner-tests", jwt.Audiences.Single());
        Assert.Equal("google-sub-123", jwt.Subject);
        Assert.Equal("google-sub-123", jwt.Claims.Single(claim => claim.Type == "user_id").Value);
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == "cook@example.com");
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == JwtRegisteredClaimNames.Name && claim.Value == "Casey Cook");
        Assert.Equal("https://images.example.com/avatar.png", jwt.Claims.Single(claim => claim.Type == "picture").Value);
    }
}
