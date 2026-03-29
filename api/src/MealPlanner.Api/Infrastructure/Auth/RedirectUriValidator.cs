namespace MealPlanner.Api.Infrastructure.Auth;

using MealPlanner.Api.Domain.Exceptions;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.WebUtilities;

public sealed class RedirectUriValidator
{
    private static readonly HashSet<string> AllowedSchemes = new(StringComparer.OrdinalIgnoreCase)
    {
        Uri.UriSchemeHttp,
        Uri.UriSchemeHttps,
        "com.mealplanner",
    };

    public Uri Validate(string? redirectUri)
    {
        if (string.IsNullOrWhiteSpace(redirectUri))
        {
            throw CreateValidationException("redirect_uri", "redirect_uri is required.");
        }

        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var parsedUri))
        {
            throw CreateValidationException("redirect_uri", "redirect_uri must be an absolute URI.");
        }

        if (!AllowedSchemes.Contains(parsedUri.Scheme))
        {
            throw CreateValidationException(
                "redirect_uri",
                "redirect_uri must use http, https, or com.mealplanner.");
        }

        if (!string.IsNullOrEmpty(parsedUri.Fragment))
        {
            throw CreateValidationException("redirect_uri", "redirect_uri must not contain a URI fragment.");
        }

        return parsedUri;
    }

    public Uri Validate(AuthenticationProperties properties)
    {
        properties.Items.TryGetValue("redirect_uri", out var redirectUri);
        return Validate(redirectUri);
    }

    public string AppendToken(Uri redirectUri, string token)
    {
        return QueryHelpers.AddQueryString(redirectUri.ToString(), "token", token);
    }

    private static RequestValidationException CreateValidationException(string key, string message)
    {
        return new RequestValidationException(new Dictionary<string, string[]>
        {
            [key] = [message],
        });
    }
}
