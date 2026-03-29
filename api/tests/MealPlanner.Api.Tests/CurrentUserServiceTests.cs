using System.Security.Claims;
using MealPlanner.Api.Infrastructure.Auth;
using Microsoft.AspNetCore.Http;

namespace MealPlanner.Api.Tests;

public sealed class CurrentUserServiceTests
{
    [Fact]
    public void UserIdAndEmail_ReadClaimsFromHttpContextUser()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(
                    new ClaimsIdentity(
                        [
                            new Claim("user_id", "google-sub-123"),
                            new Claim("email", "cook@example.com"),
                        ],
                        authenticationType: "Bearer")),
            },
        };

        var currentUser = new CurrentUserService(httpContextAccessor);

        Assert.Equal("google-sub-123", currentUser.UserId);
        Assert.Equal("cook@example.com", currentUser.Email);
    }

    [Fact]
    public void UserId_ThrowsWhenRequestIsUnauthenticated()
    {
        var httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = new DefaultHttpContext(),
        };

        var currentUser = new CurrentUserService(httpContextAccessor);

        Assert.Throws<UnauthorizedAccessException>(() => _ = currentUser.UserId);
    }
}
