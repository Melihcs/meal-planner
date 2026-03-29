namespace MealPlanner.Api.Infrastructure.Auth;

using MealPlanner.Api.Domain.Entities;
using MealPlanner.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class DevelopmentAuthLoginService(
    MealPlannerDbContext dbContext,
    JwtTokenService jwtTokenService)
{
    private const string DevelopmentUserId = "dev-easy-login";
    private const string DevelopmentUserEmail = "dev@mealplanner.local";
    private const string DevelopmentUserDisplayName = "Easy Login";

    public async Task<string> SignInAsync(CancellationToken cancellationToken)
    {
        var userProfile = await dbContext.UserProfiles.SingleOrDefaultAsync(
            user => user.Id == DevelopmentUserId,
            cancellationToken);

        if (userProfile is null)
        {
            userProfile = new UserProfile
            {
                Id = DevelopmentUserId,
                DisplayName = DevelopmentUserDisplayName,
                AvatarUrl = null,
            };

            dbContext.UserProfiles.Add(userProfile);
        }
        else
        {
            userProfile.DisplayName = DevelopmentUserDisplayName;
            userProfile.AvatarUrl = null;
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        return jwtTokenService.CreateToken(
            userProfile.Id,
            DevelopmentUserEmail,
            userProfile.DisplayName,
            userProfile.AvatarUrl);
    }
}
