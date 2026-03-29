using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using MealPlanner.Api.Infrastructure.Auth;
using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace MealPlanner.Api.Tests;

public sealed class GoogleOAuthTicketHandlerTests
{
    [Fact]
    public async Task HandleAsync_UpsertsUserAndRedirectsWithJwt()
    {
        var databaseName = Guid.NewGuid().ToString("N");
        var dbContext = new MealPlannerDbContext(
            new DbContextOptionsBuilder<MealPlannerDbContext>()
                .UseInMemoryDatabase(databaseName)
                .Options);

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JWT_ISSUER"] = "https://api.mealplanner.test",
                ["JWT_AUDIENCE"] = "meal-planner-tests",
                ["JWT_SECRET"] = "12345678901234567890123456789012",
                ["JWT_EXPIRY_DAYS"] = "7",
            })
            .Build();

        var ticketHandler = new GoogleOAuthTicketHandler(
            dbContext,
            new JwtTokenService(configuration),
            new RedirectUriValidator(),
            NullLogger<GoogleOAuthTicketHandler>.Instance);

        var services = new ServiceCollection()
            .AddSingleton<IAuthenticationService, NoOpAuthenticationService>()
            .BuildServiceProvider();

        var httpContext = new DefaultHttpContext
        {
            RequestServices = services,
        };

        var principal = new ClaimsPrincipal(
            new ClaimsIdentity(
                [
                    new Claim(ClaimTypes.NameIdentifier, "google-sub-123"),
                    new Claim(ClaimTypes.Email, "cook@example.com"),
                    new Claim(ClaimTypes.Name, "Casey Cook"),
                    new Claim("picture", "https://images.example.com/avatar.png"),
                ],
                authenticationType: "Google"));

        var properties = new AuthenticationProperties(new Dictionary<string, string?>
        {
            ["redirect_uri"] = "http://localhost:4200/auth/callback",
        });

        var ticket = new AuthenticationTicket(principal, properties, "Google");
        var context = new TicketReceivedContext(
            httpContext,
            new AuthenticationScheme("Google", "Google", typeof(NoOpAuthenticationHandler)),
            new GoogleOptions(),
            ticket);

        await ticketHandler.HandleAsync(context);

        var user = await dbContext.UserProfiles.SingleAsync();
        Assert.Equal("google-sub-123", user.Id);
        Assert.Equal("Casey Cook", user.DisplayName);
        Assert.Equal("https://images.example.com/avatar.png", user.AvatarUrl);

        Assert.Equal(StatusCodes.Status302Found, httpContext.Response.StatusCode);
        var location = httpContext.Response.Headers.Location.ToString();
        Assert.StartsWith("http://localhost:4200/auth/callback?token=", location, StringComparison.Ordinal);

        var redirectedToken = location.Split("token=", 2)[1];
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(redirectedToken);
        Assert.Equal("google-sub-123", jwt.Subject);
        Assert.Contains(
            jwt.Claims,
            claim => claim.Type == "email" && claim.Value == "cook@example.com");
    }

    private sealed class NoOpAuthenticationHandler : IAuthenticationHandler
    {
        public Task InitializeAsync(AuthenticationScheme scheme, HttpContext context) => Task.CompletedTask;
        public Task<AuthenticateResult> AuthenticateAsync() => Task.FromResult(AuthenticateResult.NoResult());
        public Task ChallengeAsync(AuthenticationProperties? properties) => Task.CompletedTask;
        public Task ForbidAsync(AuthenticationProperties? properties) => Task.CompletedTask;
    }

    private sealed class NoOpAuthenticationService : IAuthenticationService
    {
        public Task<AuthenticateResult> AuthenticateAsync(HttpContext context, string? scheme)
            => Task.FromResult(AuthenticateResult.NoResult());

        public Task ChallengeAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task ForbidAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignInAsync(HttpContext context, string? scheme, ClaimsPrincipal principal, AuthenticationProperties? properties)
            => Task.CompletedTask;

        public Task SignOutAsync(HttpContext context, string? scheme, AuthenticationProperties? properties)
            => Task.CompletedTask;
    }
}
