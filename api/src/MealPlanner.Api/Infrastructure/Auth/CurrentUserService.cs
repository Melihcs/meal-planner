namespace MealPlanner.Api.Infrastructure.Auth;

using System.Security.Claims;

public interface ICurrentUserService
{
    string UserId { get; }
    string Email { get; }
}

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public string UserId => GetRequiredClaim("user_id");

    public string Email => GetRequiredClaim("email");

    private string GetRequiredClaim(string claimType)
    {
        var httpContext = httpContextAccessor.HttpContext
            ?? throw new UnauthorizedAccessException("The current request does not have an HTTP context.");

        var principal = httpContext.User;
        if (principal.Identity?.IsAuthenticated is not true)
        {
            throw new UnauthorizedAccessException("The current request is not authenticated.");
        }

        return principal.FindFirstValue(claimType)
            ?? throw new UnauthorizedAccessException($"The current user is missing the '{claimType}' claim.");
    }
}
