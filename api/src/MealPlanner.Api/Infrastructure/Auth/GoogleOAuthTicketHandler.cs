namespace MealPlanner.Api.Infrastructure.Auth;

using System.Security.Claims;
using MealPlanner.Api.Domain.Entities;
using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth;
using Microsoft.EntityFrameworkCore;

public sealed class GoogleOAuthTicketHandler(
    MealPlannerDbContext dbContext,
    JwtTokenService jwtTokenService,
    RedirectUriValidator redirectUriValidator,
    ILogger<GoogleOAuthTicketHandler> logger)
{
    public async Task HandleAsync(TicketReceivedContext context)
    {
        if (context.Principal is null)
        {
            throw new InvalidOperationException("Google OAuth callback did not include a user principal.");
        }

        if (context.Properties is null)
        {
            throw new InvalidOperationException("Google OAuth callback did not preserve authentication properties.");
        }

        var googleUser = GoogleUserInfo.FromPrincipal(context.Principal);
        var redirectUri = redirectUriValidator.Validate(context.Properties);
        var userProfile = await UpsertUserProfileAsync(googleUser, context.HttpContext.RequestAborted);
        var token = jwtTokenService.CreateToken(
            userProfile.Id,
            googleUser.Email,
            userProfile.DisplayName,
            userProfile.AvatarUrl);

        var callbackUri = redirectUriValidator.AppendToken(redirectUri, token);

        logger.LogInformation("Completed Google OAuth callback for user {UserId}", userProfile.Id);

        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        context.Response.Redirect(callbackUri);
        context.HandleResponse();
    }

    private async Task<UserProfile> UpsertUserProfileAsync(
        GoogleUserInfo googleUser,
        CancellationToken cancellationToken)
    {
        var userProfile = await dbContext.UserProfiles.SingleOrDefaultAsync(
            user => user.Id == googleUser.Subject,
            cancellationToken);

        if (userProfile is null)
        {
            userProfile = new UserProfile
            {
                Id = googleUser.Subject,
                DisplayName = googleUser.DisplayName,
                AvatarUrl = googleUser.PictureUrl,
            };

            dbContext.UserProfiles.Add(userProfile);
        }
        else
        {
            userProfile.DisplayName = googleUser.DisplayName;
            userProfile.AvatarUrl = googleUser.PictureUrl;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return userProfile;
    }

    private sealed record GoogleUserInfo(
        string Subject,
        string Email,
        string DisplayName,
        string? PictureUrl)
    {
        public static GoogleUserInfo FromPrincipal(ClaimsPrincipal principal)
        {
            var subject = principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? principal.FindFirstValue("sub")
                ?? throw new InvalidOperationException("Google OAuth callback did not include a subject claim.");

            var email = principal.FindFirstValue(ClaimTypes.Email)
                ?? principal.FindFirstValue("email")
                ?? throw new InvalidOperationException("Google OAuth callback did not include an email claim.");

            var displayName = principal.FindFirstValue(ClaimTypes.Name)
                ?? principal.FindFirstValue("name")
                ?? email;

            var pictureUrl = principal.FindFirstValue("picture");

            return new GoogleUserInfo(subject, email, displayName, pictureUrl);
        }
    }
}
