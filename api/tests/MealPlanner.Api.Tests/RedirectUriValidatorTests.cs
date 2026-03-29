using MealPlanner.Api.Domain.Exceptions;
using MealPlanner.Api.Infrastructure.Auth;

namespace MealPlanner.Api.Tests;

public sealed class RedirectUriValidatorTests
{
    private readonly RedirectUriValidator validator = new();

    [Theory]
    [InlineData("http://localhost:4200/auth/callback")]
    [InlineData("https://mealplanner.example.com/auth/callback")]
    [InlineData("com.mealplanner://auth/callback")]
    public void Validate_AllowsExpectedRedirectUriSchemes(string redirectUri)
    {
        var validatedUri = validator.Validate(redirectUri);

        Assert.Equal(redirectUri, validatedUri.ToString());
    }

    [Theory]
    [InlineData("")]
    [InlineData("ftp://mealplanner.example.com/auth/callback")]
    [InlineData("/auth/callback")]
    [InlineData("https://mealplanner.example.com/auth/callback#fragment")]
    public void Validate_RejectsInvalidRedirectUris(string redirectUri)
    {
        var exception = Assert.Throws<RequestValidationException>(() => validator.Validate(redirectUri));

        Assert.Contains("redirect_uri", exception.Errors.Keys);
    }
}
