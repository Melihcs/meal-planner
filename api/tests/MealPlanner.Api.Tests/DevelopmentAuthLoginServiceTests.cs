using System.IdentityModel.Tokens.Jwt;
using MealPlanner.Api.Infrastructure.Auth;
using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace MealPlanner.Api.Tests;

public sealed class DevelopmentAuthLoginServiceTests
{
    [Fact]
    public async Task SignInAsync_CreatesDevelopmentUserAndReturnsJwt()
    {
        await using var dbContext = CreateDbContext();
        var service = new DevelopmentAuthLoginService(dbContext, new JwtTokenService(BuildConfiguration()));

        var token = await service.SignInAsync(CancellationToken.None);

        var userProfile = await dbContext.UserProfiles.SingleAsync();
        Assert.Equal("dev-easy-login", userProfile.Id);
        Assert.Equal("Easy Login", userProfile.DisplayName);

        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        Assert.Equal("dev-easy-login", jwt.Claims.Single(claim => claim.Type == "user_id").Value);
        Assert.Contains(jwt.Claims, claim => claim.Type == "email" && claim.Value == "dev@mealplanner.local");
    }

    [Fact]
    public async Task SignInAsync_ReusesExistingDevelopmentUser()
    {
        await using var dbContext = CreateDbContext();
        dbContext.UserProfiles.Add(new Domain.Entities.UserProfile
        {
            Id = "dev-easy-login",
            DisplayName = "Old Name",
        });
        await dbContext.SaveChangesAsync();

        var service = new DevelopmentAuthLoginService(dbContext, new JwtTokenService(BuildConfiguration()));

        _ = await service.SignInAsync(CancellationToken.None);

        Assert.Equal(1, await dbContext.UserProfiles.CountAsync());
        var userProfile = await dbContext.UserProfiles.SingleAsync();
        Assert.Equal("Easy Login", userProfile.DisplayName);
    }

    private static MealPlannerDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<MealPlannerDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new MealPlannerDbContext(options);
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
}
