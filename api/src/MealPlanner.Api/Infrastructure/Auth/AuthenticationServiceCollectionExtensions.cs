namespace MealPlanner.Api.Infrastructure.Auth;

using System.Security.Claims;
using System.Text;
using MealPlanner.Api.Infrastructure.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

public static class AuthenticationServiceCollectionExtensions
{
    public static IServiceCollection AddMealPlannerAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtOptions = JwtIssuanceOptions.FromConfiguration(configuration);
        var googleClientId = configuration["GOOGLE_CLIENT_ID"];
        var googleClientSecret = configuration["GOOGLE_CLIENT_SECRET"];
        var secret = jwtOptions.Secret.Length >= 32
            ? jwtOptions.Secret
            : jwtOptions.Secret.PadRight(32, '_');

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<DevelopmentAuthLoginService>();
        services.AddScoped<GoogleOAuthTicketHandler>();
        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<RedirectUriValidator>();
        var authenticationBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddJwtBearer(JwtBearerDefaults.AuthenticationScheme, options =>
            {
                options.MapInboundClaims = false;
                options.RequireHttpsMetadata = false;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = !string.IsNullOrWhiteSpace(jwtOptions.Issuer),
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = !string.IsNullOrWhiteSpace(jwtOptions.Audience),
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret)),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromMinutes(1),
                    NameClaimType = "name",
                    RoleClaimType = ClaimTypes.Role,
                };
            })
            ;

        if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
        {
            authenticationBuilder.AddGoogle(GoogleDefaults.AuthenticationScheme, options =>
            {
                options.ClientId = googleClientId;
                options.ClientSecret = googleClientSecret;
                options.CallbackPath = "/auth/callback";
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.Scope.Clear();
                options.Scope.Add("openid");
                options.Scope.Add("email");
                options.Scope.Add("profile");
                options.Events.OnTicketReceived = async context =>
                {
                    var handler = context.HttpContext.RequestServices.GetRequiredService<GoogleOAuthTicketHandler>();
                    await handler.HandleAsync(context);
                };
            });
        }
        services.AddAuthorization();

        return services;
    }
}
